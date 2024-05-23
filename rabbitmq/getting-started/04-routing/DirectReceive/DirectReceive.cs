using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

ConnectionFactory factory = new()
{
    HostName = "localhost"
};

using IConnection connection = factory.CreateConnection();
using IModel channel = connection.CreateModel();

channel.ExchangeDeclare(
    exchange: "direct_logs",
    type: ExchangeType.Direct
);

string queueName = channel.QueueDeclare().QueueName;

if (args.Length < 1)
{
    Console.Error.WriteLine(
        "Usage: {0} [info] [warning] [error]",
        Environment.GetCommandLineArgs()[0]
    );

    Console.WriteLine("Press [enter] to exit.");
    Console.ReadLine();
    Environment.ExitCode = 1;
    return;
}

foreach (string severity in args)
{
    channel.QueueBind(
        queue: queueName,
        exchange: "direct_logs",
        routingKey: severity
    );
}

Console.WriteLine("[*] Waiting for messages.");

EventingBasicConsumer consumer = new(channel);

consumer.Received += (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    string message = Encoding.UTF8.GetString(body);
    string routingKey = ea.RoutingKey;
    
    Console.WriteLine($"[x] Received '{routingKey}':'{message}'");
};

channel.BasicConsume(
    queue: queueName,
    autoAck: true,
    consumer: consumer
);

Console.WriteLine("Press [enter] to exit.");
Console.ReadLine();