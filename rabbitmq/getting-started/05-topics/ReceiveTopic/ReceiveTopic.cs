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
    exchange: "topic_logs",
    type: ExchangeType.Topic
);

string queueName = channel.QueueDeclare().QueueName;

if (args.Length < 1)
{
    Console.Error.WriteLine(
        "Usage: {0} [binding_key...]",
        Environment.GetCommandLineArgs()[0]
    );

    Console.WriteLine("Press [enter] to exit.");
    Console.ReadLine();

    Environment.ExitCode = 1;
    return;
}

foreach (var bindingKey in args)
{
    channel.QueueBind(
        queue: queueName,
        exchange: "topic_logs",
        routingKey: bindingKey
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