using MassTransit;
using MtExceptions.Events.Messages;

namespace MtExceptions.Events.Producers;
public class ErrorProducer(IPublishEndpoint publisher)
{
    readonly IPublishEndpoint publisher = publisher;

    public async Task Doom(DoomedMessage message) =>
        await publisher.Publish(message);

    public async Task Fault(FaultyMessage message) =>
        await publisher.Publish(message);

    public async Task Filter(FilterMessage message) =>
        await publisher.Publish(message);

    public async Task Redelivery(RedeliveryMessage message) =>
        await publisher.Publish(message);

    public async Task Retry(RetryMessage message) =>
        await publisher.Publish(message);
}