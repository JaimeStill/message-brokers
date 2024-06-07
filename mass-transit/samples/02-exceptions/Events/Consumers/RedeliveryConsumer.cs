using MassTransit;
using MtExceptions.Events.Messages;

namespace MtExceptions.Events.Consumers;
public class RedeliveryConsumer(ILogger<RedeliveryConsumer> logger) : IConsumer<RedeliveryMessage>
{
    readonly ILogger<RedeliveryConsumer> logger = logger;

    public Task Consume(ConsumeContext<RedeliveryMessage> context)
    {
        logger.LogInformation(
            "Attempting to process message. Retries: {Retries}.",
            context.Message.Retries
        );

        if (context.Message.Retries > 3)
        {
            logger.LogInformation(
                "Message Received: {Message}.",
                context.Message.Value
            );

            return Task.CompletedTask;
        }
        else
        {
            context.Message.Retries += 1;

            throw new Exception(context.Message.Alert);
        }
    }
}