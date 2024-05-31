# [Consumers](https://masstransit.io/documentation/concepts/consumers)

Consumer is a widely used noun for something that *consumes* something. In MassTransit, a consumer *consumes* one or more message types when configured on or connected to a receive endpoint. MassTransit includes many consumer types, including consumers, [sagas](https://masstransit.io/documentation/patterns/saga), saga state machines, [routing slip activities](https://masstransit.io/documentation/patterns/routing-slip), handlers, and [job consumers](https://masstransit.io/documentation/patterns/job-consumers).

## [Message Consumers](https://masstransit.io/documentation/concepts/consumers#message-consumers)

A message consumer, the most common consumer type, is a class that consumes one or more message types. For each message type, the class implements `IConsumer<TMessage>` and the `Consume` method:

```cs
public interface IConsumer<in TMessage>
: IConsumer where TMessage : class
{
    Task Consume(ConsumeContext<TMessage> context);
}
```

An example message consumer that consumes the `SubmitOrder` message type:

```cs
class SubmitOrderConsumer
: IConsumer<SubmitOrder>
{
    public async Task Consume(ConsumeContext<SubmitOrder> context)
    {
        await context.Publish<OrderSubmitted>(new
        {
            context.Message.OrderId
        });
    }
}
```

To add a consumer and automatically configure a receive endpoint for the consumer, call one of the [**`AddConsumer`**](https://masstransit.io/documentation/configuration/consumers) methods and call `ConfigureEndpoints`:

```cs
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SubmitOrderConsumer>();

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});
```

## [Batch Consumers](https://masstransit.io/documentation/concepts/consumers#batch-consumers)

In some scenarios, high message volume can lead to consumer resource bottlenecks. If a system is publishing thousands of messages per second, and has a consumer that is writing the content of those messages to some type of storage, the storage system might not be optimized for thousands of individual writes per second. It may, however, perform better if writes are performed in batches. For example, receiving one hundred messages and then writing the content of those messages using a single storage operation may be significantly more efficient (and faster).

MassTransit supports receiving multiple messages and delivering those messages to the consumer in a batch.

To create a batch consumer, consume the `Batch<T>` interface, where `T` is the message type. That consumer can then be configured using the container integration, with the batch options specified in a consumer definition. The example below consumes a batch of `OrderAudit` events, up to 100 at a time, and up to 10 concurrent batches:

```cs
class BatchMessageConsumer
: IConsumer<Batch<Message>>
{
    public async Task Consume(ConsumeContext<Batch<Message>> context)
    {
        for (int i = 0; i < context.Message.Length; i++)
            ConsumeContext<Message> message = context.Message[i];
    }
}
```

## [Definitions](https://masstransit.io/documentation/concepts/consumers#definitions)

Consumer definitinos are used to specify the behavior of consumers so that they can be automatically configured. Definitions may be explicitly added to `AddConsumer` or discovered automatically using any of the `AddConsumers` methods.

```cs
public class SubmitOrderConsumerDefinition
: ConsumerDefinition<SubmitOrderConsumer>
{
    public SubmitOrderConsumerDefinition()
    {
        // override the default endpoint name, for whatever reason
        EndpointName = "ha-submit-order";

        /*
            limit the number of messages consumed concurrently
            this applies to the consumer only, not the endpoint
        */
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<DiscoveryPingConsumer> consumerConfigurator
    )
    {
        endpointConfigurator.UseMessageRetry(r => r.Interval(5, 1000));
        endpointConfigurator.UseInMemoryOutbox();
    }
}
```

### [Skipped Messages](https://masstransit.io/documentation/concepts/consumers#skipped-messages)

When a consumer is removed (or disconnected) from a receive endpoint, a message type is removed from a consumer, or if a message is mistakenly sent to a receive endpoint, messages may be delivered to the receive endpoint that do not have a consumer.

If this occurs, the unconsumed message is moved to a `_skipped` queue (prefixed by the original queue name). The original message content is retained and additional headers are added to identify the host that skipped the message.