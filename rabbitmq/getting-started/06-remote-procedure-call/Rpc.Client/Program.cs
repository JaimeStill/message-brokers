using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class RpcClient : IDisposable
{
    private const string QUEUE_NAME = "rpc_queue";

    private readonly IConnection connection;
    private readonly IModel channel;
    private readonly string replyQueueName;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> callbackMapper = new();

    public RpcClient()
    {
        ConnectionFactory factory = new()
        {
            HostName = "localhost"
        };

        connection = factory.CreateConnection();
        channel = connection.CreateModel();

        replyQueueName = channel.QueueDeclare().QueueName;
        EventingBasicConsumer consumer = new(channel);

        consumer.Received += (model, ea) =>
        {
            callbackMapper.TryRemove(
                ea.BasicProperties.CorrelationId,
                out TaskCompletionSource<string>? tcs
            );

            if (tcs is null)
                return;

            byte[] body = ea.Body.ToArray();
            string response = Encoding.UTF8.GetString(body);
            tcs.TrySetResult(response);
        };

        channel.BasicConsume(
            consumer: consumer,
            queue: replyQueueName,
            autoAck: true
        );
    }

    public Task<string> CallAsync(string message, CancellationToken cancellationToken = default)
    {
        IBasicProperties props = channel.CreateBasicProperties();
        string correlationId = Guid.NewGuid().ToString();
        props.CorrelationId = correlationId;
        props.ReplyTo = replyQueueName;
        byte[] body = Encoding.UTF8.GetBytes(message);
        TaskCompletionSource<string> tcs = new();
        callbackMapper.TryAdd(correlationId, tcs);

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: QUEUE_NAME,
            basicProperties: props,
            body: body
        );

        cancellationToken.Register(() => callbackMapper.TryRemove(correlationId, out _));
        return tcs.Task;
    }

    public void Dispose()
    {
        channel.Close();
        connection.Close();

        GC.SuppressFinalize(this);
    }
}

public class Rpc
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("RPC Client");

        string n = args.Length > 0
            ? args[0]
            : "30";

        await InvokeAsync(n);
    }

    private static async Task InvokeAsync(string n)
    {
        using RpcClient client = new();

        Console.WriteLine($"[x] Requesting Fib({n})");
        string response = await client.CallAsync(n);
        Console.WriteLine($"[.] Got '{response}'");
    }
}