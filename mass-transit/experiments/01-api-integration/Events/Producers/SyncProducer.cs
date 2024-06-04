using MassTransit;
using MassTransitApi.Events.Messages;

namespace MassTransitApi.Events.Producers;

public class SyncProducer(IPublishEndpoint publisher)
{
    readonly IPublishEndpoint publisher = publisher;

    public async Task Sync(SyncMessage message) =>
        await publisher.Publish(message);
}