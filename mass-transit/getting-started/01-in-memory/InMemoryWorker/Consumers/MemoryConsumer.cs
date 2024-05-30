using InMemoryWorker.Contracts;
using MassTransit;

namespace InMemoryWorker.Consumers;

public class MemoryConsumer(ILogger<MemoryConsumer> logger) : IConsumer<MemoryContract>
{
    readonly ILogger<MemoryConsumer> logger = logger;

    public Task Consume(ConsumeContext<MemoryContract> context)
    {
        logger.LogInformation($"Received Text: {context.Message.Value}");
        return Task.CompletedTask;
    }
}