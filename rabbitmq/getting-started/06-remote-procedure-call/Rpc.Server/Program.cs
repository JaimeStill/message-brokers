using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

ConnectionFactory factory = new()
{
    HostName = "localhost"
};

using IConnection connection = factory.CreateConnection();
using IModel channel = connection.CreateModel();

channel.QueueDeclare(
    queue: "rpc_queue",
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

channel.BasicQos(
    prefetchSize: 0,
    prefetchCount: 1,
    global: false
);

EventingBasicConsumer consumer = new(channel);

channel.BasicConsume(
    queue: "rpc_queue",
    autoAck: false,
    consumer: consumer
);

Console.WriteLine("[x] Awaiting RPC requests");

consumer.Received += (model, ea) =>
{
    string result = string.Empty;

    byte[] body = ea.Body.ToArray();
    IBasicProperties props = ea.BasicProperties;
    IBasicProperties replyProps = channel.CreateBasicProperties();
    replyProps.CorrelationId = props.CorrelationId;

    try
    {
        string message = Encoding.UTF8.GetString(body);
        int n = int.Parse(message);
        Console.WriteLine($"[.] Fib({message})");
        result = Fib(n).ToString();
    }   
    catch (Exception e)
    {
        Console.WriteLine($"[.] {e.Message}");
        result = string.Empty;
    }
    finally
    {
        byte[] response = Encoding.UTF8.GetBytes(result);

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: props.ReplyTo,
            basicProperties: replyProps,
            body: response
        );

        channel.BasicAck(
            deliveryTag: ea.DeliveryTag,
            multiple: false
        );
    }
};

Console.WriteLine("Press [enter] to exit.");
Console.ReadLine();

static int Fib(int n)
{
    if (n is 0 or 1)
    {
        return n;
    }

    return Fib(n - 1) + Fib(n - 2);
}