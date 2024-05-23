using System.Text;
using RabbitMQ.Client;

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

string routingKey = args.Length > 0
    ? args[0]
    : "anonymous.info";

string message = args.Length > 1
    ? string.Join(" ", args.Skip(1).ToArray())
    : "Hello World!";

byte[] body = Encoding.UTF8.GetBytes(message);

channel.BasicPublish(
    exchange: "topic_logs",
    routingKey: routingKey,
    basicProperties: null,
    body: body
);

Console.WriteLine($"[x] Sent '{routingKey}':'{message}'");