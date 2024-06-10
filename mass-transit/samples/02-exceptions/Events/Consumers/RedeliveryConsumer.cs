using MassTransit;
using MtExceptions.Events.Messages;

namespace MtExceptions.Events.Consumers;
public class RedeliveryConsumer(ILogger<RedeliveryConsumer> logger) : IConsumer<RedeliveryMessage>
{
    readonly ILogger<RedeliveryConsumer> logger = logger;

    public Task Consume(ConsumeContext<RedeliveryMessage> context)
    {
        if (context.Message.Retries > 0)
        {
            logger.LogInformation(
                "Attempting to process message. Retries: {Retries}",
                context.Message.Retries
            );
        }
        else
        {
            logger.LogInformation(
                "Attempting to process message."
            );
        }

        Random rng = new();
        int result = rng.Next(1, 7);

        if (result == 6)
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
            throw new InvalidOperationException(context.Message.Alert);
        }
    }
}