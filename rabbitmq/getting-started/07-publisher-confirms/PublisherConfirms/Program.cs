using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using RabbitMQ.Client;

const int MESSAGE_COUNT = 50_000;

PublishMessagesIndividually();
PublishMessagesInBatch();
await HandlePublishConfirmsAsynchronously();

static IConnection CreateConnection()
{
    ConnectionFactory factory = new()
    {
        HostName = "localhost"
    };

    return factory.CreateConnection();
}

static void PublishMessagesIndividually()
{
    using IConnection connection = CreateConnection();
    using IModel channel = connection.CreateModel();

    string queueName = channel.QueueDeclare().QueueName;
    channel.ConfirmSelect();

    long startTime = Stopwatch.GetTimestamp();

    for (int i = 0; i < MESSAGE_COUNT; i++)
    {
        byte[] body = Encoding.UTF8.GetBytes(i.ToString());

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: queueName,
            basicProperties: null,
            body: body
        );

        channel.WaitForConfirmsOrDie(
            TimeSpan.FromSeconds(5)
        );
    }

    long endTime = Stopwatch.GetTimestamp();

    Console.WriteLine($"Published {MESSAGE_COUNT:N0} messages individually in {Stopwatch.GetElapsedTime(startTime, endTime).TotalMilliseconds:N0}ms");
}

static void PublishMessagesInBatch()
{
    using IConnection connection = CreateConnection();
    using IModel channel = connection.CreateModel();

    string queueName = channel.QueueDeclare().QueueName;
    channel.ConfirmSelect();

    int batchSize = 100;
    int outstandingMessageCount = 0;

    long startTime = Stopwatch.GetTimestamp();

    for (int i = 0; i < MESSAGE_COUNT; i++)
    {
        byte[] body = Encoding.UTF8.GetBytes(i.ToString());

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: queueName,
            basicProperties: null,
            body: body
        );

        outstandingMessageCount++;

        if (outstandingMessageCount == batchSize)
        {
            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
            outstandingMessageCount = 0;
        }
    }

    if (outstandingMessageCount > 0)
        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

    long endTime = Stopwatch.GetTimestamp();

    Console.WriteLine($"Published {MESSAGE_COUNT:N0} messages in batch in {Stopwatch.GetElapsedTime(startTime, endTime).TotalMilliseconds:N0}ms");
}

static async Task HandlePublishConfirmsAsynchronously()
{
    using IConnection connection = CreateConnection();
    using IModel channel = connection.CreateModel();

    string queueName = channel.QueueDeclare().QueueName;
    channel.ConfirmSelect();

    ConcurrentDictionary<ulong, string> confirms = new();

    void CleanConfirms(ulong sequenceNumber, bool multiple)
    {
        IEnumerable<ulong> confirmed = multiple
            ? confirms
                .Select(x => x.Key)
                .Where(k => k <= sequenceNumber)
            : [sequenceNumber];

        foreach (ulong key in confirmed)
            confirms.TryRemove(key, out _);
    }

    channel.BasicAcks += (sender, ea) =>
        CleanConfirms(ea.DeliveryTag, ea.Multiple);

    channel.BasicNacks += (sender, ea) =>
    {
        confirms.TryGetValue(ea.DeliveryTag, out string? body);
        Console.WriteLine($"Message with body {body} has been nack-ed. Sequence number: {ea.DeliveryTag}, multiple: {ea.Multiple}");
        CleanConfirms(ea.DeliveryTag, ea.Multiple);
    };

    long startTime = Stopwatch.GetTimestamp();

    for (int i = 0; i < MESSAGE_COUNT; i++)
    {
        string message = i.ToString();
        byte[] body = Encoding.UTF8.GetBytes(message);
        confirms.TryAdd(channel.NextPublishSeqNo, message);

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: queueName,
            basicProperties: null,
            body: body
        );
    }

    if (!await WaitUntil(60, () => confirms.IsEmpty))
        throw new Exception("All messages could not be confirmed in 60 seconds");

    long endTime = Stopwatch.GetTimestamp();
    Console.WriteLine($"Published {MESSAGE_COUNT:N0} messages and handled confirms asynchronously in {Stopwatch.GetElapsedTime(startTime, endTime).TotalMilliseconds:N0}ms");
}

static async ValueTask<bool> WaitUntil(int numberOfSeconds, Func<bool> condition)
{
    int waited = 0;

    while (!condition() && waited < numberOfSeconds * 1000)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        waited += 100;
    }

    return condition();
}