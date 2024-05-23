using System.Text;
using RabbitMQ.Client;

ConnectionFactory factory = new()
{
    HostName = "localhost"
};

using IConnection connection = factory.CreateConnection();
using IModel channel = connection.CreateModel();

channel.ExchangeDeclare(
    exchange: "logs",
    type: ExchangeType.Fanout
);

string message = GetMessage(args);
byte[] body = Encoding.UTF8.GetBytes(message);

channel.BasicPublish(
    exchange: "logs",
    routingKey: string.Empty,
    basicProperties: null,
    body: body
);

Console.WriteLine($"[x] Sent {message}");

static string GetMessage(string[] args) =>
    args.Length > 0
        ? string.Join(" ", args)
        : "info: Hello World!";