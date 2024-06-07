using MassTransit;
using MtExceptions.Events.Messages;

namespace MtExceptions.Events.Consumers;
public class RetryConsumer(ILogger<RetryConsumer> logger) : IConsumer<RetryMessage>
{
    readonly ILogger<RetryConsumer> logger = logger;

    public Task Consume(ConsumeContext<RetryMessage> context)
    {
        logger.LogInformation(
            "Attempting {Attempts} to process message.",
            context.Message.Attempts
        );

        if (context.Message.Attempts > context.Message.Iterations)
        {
            logger.LogInformation(
                "Message Received: {Message}. Attempts: {Attempts}",
                context.Message.Value,
                context.Message.Attempts
            );

            return Task.CompletedTask;
        }
        else
        {
            context.Message.Attempts += 1;
            throw new InvalidOperationException(context.Message.Value);
        }
    }
}