using System.Text;
using RabbitMQ.Client;

ConnectionFactory factory = new()
{
    HostName = "localhost"
};

using IConnection connection = factory.CreateConnection();
using IModel channel = connection.CreateModel();

channel.QueueDeclare(
    queue: "task_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

string message = GetMessage(args);
byte[] body = Encoding.UTF8.GetBytes(message);

IBasicProperties properties = channel.CreateBasicProperties();
properties.Persistent = true;

channel.BasicPublish(
    exchange: string.Empty,
    routingKey: "task_queue",
    basicProperties: null,
    body: body
);

Console.WriteLine($"[x] Sent {message}");

static string GetMessage(string[] args) =>
    args.Length > 0
        ? string.Join(" ", args)
        : "Hello World!";