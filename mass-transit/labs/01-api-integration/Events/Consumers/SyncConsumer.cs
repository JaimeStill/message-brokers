using MassTransit;
using MassTransitApi.Events.Messages;

namespace MassTransitApi.Events.Consumers;
public class SyncConsumer(ILogger<SyncConsumer> logger) : IConsumer<SyncMessage>
{
    readonly ILogger<SyncConsumer> logger = logger;

    public Task Consume(ConsumeContext<SyncMessage> context)
    {
        logger.LogInformation(
            "Sync Received: {Type}: {Message}. State: {State}",
            context.Message.Type,
            context.Message.Value,
            context.Message.State
        );

        return Task.CompletedTask;
    }        
}