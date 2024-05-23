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
    exchange: "logs",
    type: ExchangeType.Fanout
);

string queueName = channel.QueueDeclare().QueueName;

channel.QueueBind(
    queue: queueName,
    exchange: "logs",
    routingKey: string.Empty
);

Console.WriteLine("[*] Waiting for logs.");

EventingBasicConsumer consumer = new(channel);

consumer.Received += (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    string message = Encoding.UTF8.GetString(body);
    Console.WriteLine($"[x] {message}");
};

channel.BasicConsume(
    queue: queueName,
    autoAck: true,
    consumer: consumer
);

Console.WriteLine("Press [enter] to exit.");
Console.ReadLine();