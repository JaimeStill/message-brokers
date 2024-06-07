using MassTransit;
using MtExceptions.Events.Messages;

namespace MtExceptions.Events.Consumers;
public class FilterConsumer(ILogger<FilterConsumer> logger) : IConsumer<FilterMessage>
{
    readonly ILogger<FilterConsumer> logger = logger;

    public Task Consume(ConsumeContext<FilterMessage> context)
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

            throw new ArgumentException(
                context.Message.Value,
                context.Message.Parameter
            );
        }
    }
}