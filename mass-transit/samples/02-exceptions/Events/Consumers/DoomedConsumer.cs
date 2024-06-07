using MassTransit;
using MtExceptions.Events.Messages;

namespace MtExceptions.Events.Consumers;
public class DoomedConsumer : IConsumer<DoomedMessage>
{
    public Task Consume(ConsumeContext<DoomedMessage> context)
    {
        throw new Exception(context.Message.Value);
    }
}