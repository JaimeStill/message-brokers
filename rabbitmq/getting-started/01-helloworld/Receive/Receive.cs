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
    queue: "hello",
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

Console.WriteLine("[*] Waiting for messages.");

EventingBasicConsumer consumer = new(channel);

consumer.Received += (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    string message = Encoding.UTF8.GetString(body);
    Console.WriteLine($"[x] Received {message}");
};

channel.BasicConsume(
    queue: "hello",
    autoAck: true,
    consumer: consumer
);

Console.WriteLine("Press [enter] to exit.");
Console.ReadLine();