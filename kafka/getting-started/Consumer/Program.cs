using Confluent.Kafka;

ConsumerConfig config = new()
{
    BootstrapServers = "[::1]:9092",
    GroupId = "kafka-dotnet-getting-started",
    AutoOffsetReset = AutoOffsetReset.Earliest
};

const string topic = "purchases";

CancellationTokenSource cts = new();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

using IConsumer<string, string> consumer = new ConsumerBuilder<string, string>(config).Build();

consumer.Subscribe(topic);

try
{
    while (true)
    {
        ConsumeResult<string, string> result = consumer.Consume(cts.Token);
        Console.WriteLine($"Consumed event from topic {topic}: key = {result.Message.Key,-10} value = {result.Message.Value}");
    }
}
catch (OperationCanceledException) { }
finally {
    consumer.Close();
}