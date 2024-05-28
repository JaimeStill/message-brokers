using Confluent.Kafka;

const string topic = "purchases";

string[] users = [
    "eabara",
    "jsmith",
    "sgarcia",
    "jbernard",
    "htanaka",
    "awalther"
];

string[] items = [
    "book",
    "alarm clock",
    "t-shirts",
    "gift card",
    "batteries"
];

ProducerConfig config = new()
{
    BootstrapServers = "[::1]:9092",
    Acks = Acks.All
};

using IProducer<string, string> producer = new ProducerBuilder<string, string>(config).Build();

int numProduced = 0;
Random rnd = new();

for (int i = 0; i < 10; ++i)
{
    string user = users[rnd.Next(users.Length)];
    string item = items[rnd.Next(items.Length)];

    producer.Produce(
        topic, 
        new Message<string, string> {
            Key = user,
            Value = item
        },
        (deliveryReport) =>
        {
            if (deliveryReport.Error.Code != ErrorCode.NoError)
            {
                Console.WriteLine($"Failed to deliver message: {deliveryReport.Error.Reason}");
            }
            else
            {
                Console.WriteLine($"Produced event to topic {topic}: key = {user, -10} value = {item}");
                numProduced += 1;
            }
        }
    );

    producer.Flush(TimeSpan.FromSeconds(10));
    Console.WriteLine($"{numProduced} messages were produced to topic {topic}");
}