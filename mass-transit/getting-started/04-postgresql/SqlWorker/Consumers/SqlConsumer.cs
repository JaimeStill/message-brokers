using MassTransit;
using SqlWorker.Contracts;

namespace SqlWorker.Consumers;

public class SqlConsumer(ILogger<SqlConsumer> logger) : IConsumer<SqlContract>
{
    readonly ILogger<SqlConsumer> logger = logger;

    public Task Consume(ConsumeContext<SqlContract> context)
    {
        logger.LogInformation($"Received Text: {context.Message.Value}");
        return Task.CompletedTask;
    }
}