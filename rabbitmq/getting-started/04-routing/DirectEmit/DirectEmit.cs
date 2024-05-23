using System.Text;
using RabbitMQ.Client;

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

string severity = args.Length > 0
    ? args[0]
    : "info";

string message = args.Length > 1
    ? string.Join(" ", args.Skip(1).ToArray())
    : "Hello World!";

byte[] body = Encoding.UTF8.GetBytes(message);

channel.BasicPublish(
    exchange: "direct_logs",
    routingKey: severity,
    basicProperties: null,
    body: body
);

Console.WriteLine($"[x] Sent '{severity}':'{message}'");