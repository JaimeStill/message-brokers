using AsbWorker.Contracts;
using MassTransit;

namespace AsbWorker.Consumers;

public class AsbConsumer(ILogger<AsbConsumer> logger) : IConsumer<AsbContract>
{
    readonly ILogger<AsbConsumer> logger = logger;

    public Task Consume(ConsumeContext<AsbContract> context)
    {
        logger.LogInformation($"Received Text: {context.Message.Value}");
        return Task.CompletedTask;
    }
}