# [Exceptions](https://masstransit.io/documentation/concepts/exceptions)

MassTransit provides a number of features to help your application recover from and deal with exceptions.

Consider a consumer that simply throws an exception:

```cs
public class SubmitOrderConsumer : IConsumer<SubmitOrder>
{
    public Task Consume(ConsumeContext<SubmitOrder> context)
    {
        throw new Excpetion("Very bad things happened");
    }
}
```

When a message is delivered to the consumer, the consumer throws an exception. Wiht a default bus configuration, the exception is caught by middleware in the transport (the `ErrorTransportFilter` to be exact), and the message is moved to an *_error* queue (prefixed by the receive endpoint queue name). The exception details are stored as headers with the message for analysis and to assit in troubleshooting the exception.

> In addition to moving the mssage to an error queue, MassTransit also produces a [`Fault<T>`](#faults) event.

## [Retry](https://masstransit.io/documentation/concepts/exceptions#retry)

Some exceptions may be caused by a transient condition, such as a database deadlock, a busy web service, or some similar type of situation which usually clears up on a second attempt. With these exception types, it is often desirable to retry the message delivery to the consumer, allowing the consumer to try the operation again.

Shown below is a retry policy which attempts to deliver the message to a consumer five times before throwing the exception back up the pipeline:

```cs
services.AddMassTransit(mt =>
{
    mt.AddConsumer<SubmitOrderConsumer>();

    mt.UsingRabbitMq((context, cfg) =>
    {
        cfg.UseMessageRetry(r => r.Immediate(5));

        cfg.ConfigureEndpoints(context);
    });
});
```

The `UseMessageRetry` method is an extension method that configures a middleware filter, in this case the `RetryFilter`.

To configure retry on a manually configured receive endpoint:

```cs
services.AddMassTransit(mt =>
{
    mt.AddConsumer<SubmitOrderConsumer>();

    mt.UsingRabbitMq((context, cfg) =>
    {
        cfg.ReceiveEndopint("submit-order", e =>
        {
            e.UseMessageRetry(r => r.Immediate(5));

            e.ConfigureConsumer<SubmitOrderConsumer>(context);
        });
    });
});
```

MassTransit retry filters execute in memory and maintain a *lock* on the message. As such, they should only be used to handle short, transient error conditions.

## [Retry Configuration](https://masstransit.io/documentation/concepts/exceptions#retry-configuration)

When configuring message retry, there are several retry policies available:

Policy | Description
-------|------------
**None** | No retry
**Immediate** | Retry immediately, up to the retry limit
**Interval** | Retry after a fixed delay, up to the retry limit
**Intervals** | Retry after a delay, for each interval specified
**Exponential** | Retry after an exponentially increasing delay, up to the retry limit
**Incremental** | Retry after a steadily increasing delay, up to the retry limit

### [Exception Filters](https://masstransit.io/documentation/concepts/exceptions#exception-filters)

Sometimes you do not want to always retry, but instead only retry when some specific exception is thrown and fault for all other exceptions. To implement this, you can use an exception filter. Specify exception types using either the `Handle` or `Ignore` method. A filter can have either *Handle* or *Ignore* statements, combining them has unpredictable effects.

Both methods have two signatures:

1. Generic version `Handle<T>` and `Ignore<T>`, where `T` must be derivate of `System.Exception`. With no argument, all exceptions of specified type will either be handled or ignored. You can also specify a function argument that will filter exceptions further based on other parameters.

2. Non-generic version that needs one or more exception types as parameters. No further filtering is possible if this version is used.

You can use multiple calls to these methods to specify filters for multiple exception types:

```cs
e.UseMessageRetry(r =>
{
    r.Handle<ArgumentNullException>();
    r.Ignore(typeof(InvalidOperationException), typeof(InvalidCastException));
    r.Ignore<ArgumentException>(t => t.ParamName == "orderTotal");
});
```

You can also specify multiple retry policies for a single endpoint:

```cs
services.AddMassTransit(mt =>
{
    mt.AddConsumer<SubmitOrderConsumer>();

    mt.UsingRabbitMq((context, cfg) =>
    {
        cfg.ReceiveEndpoint("submit-order", e =>
        {
            e.UseMessageRetry(r =>
            {
                r.Immediate(5);
                r.Handle<DataException>(x => x.Message.Contains("SQL"));
            });

            e.ConfigureConsumer<SubmitOrderConsumer>(context, c => c.UseMessageRetry(r =>
            {
                r.Interval(10, TimeSpan.FromMilliseconds(200));
                r.Ignore<ArgumentNullException>();
                r.Ignore<DataException>(x => x.Message.Contains("SQL"));
            }));
        });
    });
});
```

In the above example, if the consumer throws an `ArgumentNullException` it won't be retried. If a `DataException` is thrown matching the filter expression, it wouldn't be handled by the second retry filter, but would be handled by the first retry filter.

## [Redelivery](https://masstransit.io/documentation/concepts/exceptions#redelivery)

Some errors take a while to resolve, say a remote service is down or a SQL server has crashed. Redelivery is a form of retry (some refer to it as a *second-level retry*) where the message is removed from the queue and then redelivered to the queue at a future time.

To use delayed redelivery, ensure the transport is properly configured. RabbitMQ requires a [delayed-exchange plug-in](https://github.com/rabbitmq/rabbitmq-delayed-message-exchange?tab=readme-ov-file#rabbitmq-delayed-message-plugin), and ActiveMQ (non-Artemis) requires the scheduler to be enabled via the XML configuration.

```cs
services.AddMassTransit(mt =>
{
    mt.AddConsumer<SubmitOrderConsumer>();

    mt.UsingRabbitMq((context, cfg) =>
    {
        cfg.UseDelayedRedelivery(r => r.Intervals(
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(30)
        ));

        cfg.UseMessageRetry(r => r.Immediate(5));
        cfg.ConfigureEndpoints(context);
    });
});
```

Now, if the initial 5 immediate retries fail (the database is really, really down), the message will retry an additional three times after 5, 15, and 30 minutes. This could mean a total of 15 retry attempts (on top of the initial 4 attempts prior to the retry / redelivery filters taking control).

## [Outbox](https://masstransit.io/documentation/concepts/exceptions#outbox)

If the consumer publishes events or sends messages (using `ConsumeContext`, which is provided via the `Consume` method on the consumer) and subsequently throws an exception, it isn't likely that those messages should still be published or sent. MassTransit provides an outbox to buffer those messages until the consumer completes successfully. If an exception is thrown, the buffered messages are discarded.

To configure the outbox with redelivery and retry:

```cs
services.AddMassTransit(mt =>
{
    mt.AddConsumer<SubmitOrderConsumer>();

    mt.UsingRabbitMq((context, cfg) =>
    {
        cfg.UseDelayedRedelivery(r => r.Intervals(
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(30)
        ));

        cfg.UseMessageRetry(r => r.Immediate(5));
        cfg.UseInMemoryOutbox();
        cfg.ConfigureEndpoints(context);
    });
});
```

### [Configuring for a Consumer or Saga](https://masstransit.io/documentation/concepts/exceptions#configuring-for-a-consumer-or-saga)

If there are multiple consumers (or saga) on the same endpoint, and the retry / redelivery behavior should only apply to a specific consumer or saga, the same configuration can be applied specifically to the consumer or saga:

```cs
services.AddMassTransit(mt =>
{
    mt.AddConsumer<SubmitOrderConsumer>();

    mt.UsingRabbitMq((context, cfg) =>
    {
        cfg.ReceiveEndpoint("submit-order", e =>
        {
            e.ConfigureConsumer<SubmitOrderConsumer>(context, c =>
            {
                c.UseDelayedRedelivery(r => r.Intervals(
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromMinutes(15),
                    TimeSpan.FromMinutes(30)
                ));

                c.UseMessageRetry(r => r.Immediate(5));
                c.UseInMemoryOutbox();
            });
        });
    });
});
```

Sagas are configured in the same way, using the saga configurator.

## [Faults](https://masstransit.io/documentation/concepts/exceptions#faults)

As shown above, MassTransit delivers messages to consumers by calling the `Consume` method. When a message consumer throws an exception instead of returning normally, a `Fault<T>` is produced, which may be published or sent depending upon the context.

A `Fault<T>` is a generic message contract including the original message that caused the consumer to fail, as well as the `ExceptionInfo`, `HostInfo`, and the time of the exception.

```cs
public interface Fault<T> where T : class
{
    Guid FaultId { get; }
    Guid? FaultedMessageId { get; }
    DateTime Timestamp { get; }
    ExceptionInfo[] Exceptions { get; }
    HostInfo Host { get; }
    T Message { get; }
}
```

If the message headers specify a `FaultAddress`, the fault is sent directly to that address. If the `FaultAddress` is not present, but a `ResponseAddress` is specified, the fault is sent to the response address. Otherwise, the fault is published, allowing any subscribed consumers to receive it.

### [Consuming Faults](https://masstransit.io/documentation/concepts/exceptions#consuming-faults)

Developers may want to do something with faults, such as updating an operational dashboard. To observe faults separate of the consumer that caused the fault to be produced, a consumer can consume fault messages the same as any other message:

```cs
public class DashboardFaultConsumer : IConsumer<Fault<SubmitOrder>>
{
    public async Task Consume(ConsumeContext<Fault<SubmitOrder>> context)
    {
        // update the dashboard
    }
}
```

Faults can also be observed by state machines when specified as an event:

```cs
Event(
    () => SubmitOrderFaulted,
    x => x.CorrelatedById(m => m.Message.Message.OrderId) // Fault<T> includes the original message
          .SelectId(m => m.Message.Message.OrderId)
);

public Event<Fault<SubmitOrder>> SubmitOrderFaulted { get; private set; }
```

## [Error Pipe](https://masstransit.io/documentation/concepts/exceptions#error-pipe)

By default, MassTransit will move faulted messages to the `_error` queue. This behavior can be customized for each receive endpoint.

To discard faulted messages so that they are *not* moved to the `_error` queue:

```cs
cfg.ReceiveEndpoint("input_queue", e =>
{
    e.DiscardFaultedMessages();
});
```

Beyond that built-in customization, the individual filters can be added / configured as well. Shown below are the default filters:

> This is be default, do NOT configure this unless you have a reason to change the behavior.

```cs
cfg.ReceiveEndpoint("input-queue", e =>
{
    e.ConfigureError(x =>
    {
        x.UseFilter(new GenerateFaultFilter());
        x.UseFilter(new ErrorTransportFilter());
    });
});
```

## [Dead-Letter Pipe](https://masstransit.io/documentation/concepts/exceptions#dead-letter-pipe)

By default, MassTransit will move skipped messages to the `_skipped` queue. This behavior can be customized for each receive endpoint.

To discard skipped messages so they are *not* moved to the `_skipped` queue:

```cs
cfg.ReceiveEndpoint("input-queue", e =>
{
    e.DiscardSkippedMessages();
});
```

Beyond that built-in customization, the individual filters can be added / configured as well. Shown below are the default filters:

> This is by default, do NOT configure this unless you have a reason to change the behavior.

```cs
cfg.ReceiveEndpoint("input-queue", e =>
{
    e.ConfigureDeadLetter(x =>
    {
        x.UseFilter(new DeadLetterTransportFilter());
    });
});
```