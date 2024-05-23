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
    queue: "task_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

channel.BasicQos(
    prefetchSize: 0,
    prefetchCount: 1,
    global: false
);

Console.WriteLine("[*] Waiting for messages.");

EventingBasicConsumer consumer = new(channel);

consumer.Received += (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    string message = Encoding.UTF8.GetString(body);
    Console.WriteLine($"[x] Received {message}");

    int dots = message.Split('.').Length - 1;
    Thread.Sleep(dots * 1000);

    Console.WriteLine("[x] Done");

    channel.BasicAck(
        deliveryTag: ea.DeliveryTag,
        multiple: false
    );
};

channel.BasicConsume(
    queue: "task_queue",
    autoAck: false,
    consumer: consumer
);

Console.WriteLine("Press [enter] to exit.");
Console.ReadLine();