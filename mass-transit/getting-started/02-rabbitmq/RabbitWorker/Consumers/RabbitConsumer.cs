using MassTransit;
using RabbitWorker.Contracts;

namespace RabbitWorker.Consumers;

public class RabbitConsumer(ILogger<RabbitConsumer> logger) : IConsumer<RabbitContract>
{
    readonly ILogger<RabbitConsumer> logger = logger;

    public Task Consume(ConsumeContext<RabbitContract> context)
    {
        logger.LogInformation("Received Text: {Value}", context.Message.Value);
        return Task.CompletedTask;
    }
}