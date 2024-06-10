using MassTransit;
using MtExceptions.Events.Messages;

namespace MtExceptions.Events.Consumers;
public class FaultyConsumer(ILogger<FaultyConsumer> logger) : IConsumer<FaultyMessage>
{
    readonly ILogger<FaultyConsumer> logger = logger;

    public Task Consume(ConsumeContext<FaultyMessage> context)
    {
        logger.LogInformation(
            "Message Received: {Message}",
            context.Message.Value
        );

        throw new InvalidOperationException(
            "Throwing faulty message"
        );
    }
}