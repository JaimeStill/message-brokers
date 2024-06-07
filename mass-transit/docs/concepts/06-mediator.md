# [Mediator](https://masstransit.io/documentation/concepts/mediator)

MassTransit includes a mediator implementation, with full support for consumers, handlers, and sagas (including saga state machines). MassTransit Mediator runs in-process and in-memory, no transport is required. For maximum performance, messages are passed by references rather than serialized, and control flows directly from the *Publish / Send* caller to the consumer. If a consumer throws an exception, the *Publish / Send* method throws and the exception should be handled by the caller.

## [Configuration](https://masstransit.io/documentation/concepts/mediator#configuration)

To configure Mediator, use the `AddMediator` method:

```cs
services.AddMediator(cfg =>
{
    cfg.AddConsumer<SubmitOrderConsumer>();
    cfg.AddConsumer<OrderStatusConsumer>();
});
```

Consumers and sagas (including saga repositories) can be added, routing slip activities are not supported using mediator. Consumer and saga definitions are supported as well, but certain properties like `EndpointName` are ignored. Middleware components, including `UseMessageRetry` and `UseInMemoryOutbox`, are fully supported.

Once created, Mediator doesn't need to be started or stopped and can be used immediately. `IMediator` combines several other interfaces into a single interface, including `IPublishEndpoint`, `ISendEndpoint`, and `IClientFactory`.

MassTransit dispatches the command to the consumer asynchronously. Once the `Consume` method completes, the `Send` method will complete. If the consumer throws an exception, it will be propgated back to the caller.

### [Scoped Mediator](https://masstransit.io/documentation/concepts/mediator#scoped-mediator)

The main mediator interface `IMediator` is registered as a singleton but there is another scoped version, `IScopedMediator`. This interface is registered as a part of current IoC scope (`HttpContext` or manually created) and can be used in order to share the scope for the entire pipeline. By default with `IMediator`, each consumer has its own scope. By using `IScopedMediator`, the scope is shared between several consumers.

> No additional configuration is required as long as Mediator is configured via `services.AddMediator()`.

## [Connect](https://masstransit.io/documentation/concepts/mediator#connect)

Consumers can be connected and disconnected from mediator at run-time, allowing components and services to temporarily consume messages. Use the `ConnectConsumer` method to connect a consumer. The handle can be used to disconnect the consumer.

```cs
var handle = mediator.ConnectConsumer<SubmitOrderConsumer>();
```

## [Requests](https://masstransit.io/documentation/concepts/mediator#requests)

To send a request using the mediator, a request client can be created from `IMediator`. The example below configures two consumers and then sends the `SubmitOrder` command, followed by the `GetOrderStatus` request.

```cs
Guid orderId = NewId.NextGuid();

await mediator.Send<SubmitOrder>(new { OrderId = orderId });

var client = mediator.CreateRequestClient<GetOrderStatus>();

var response = await client.GetResponse<OrderStatus>(new { OrderId = orderId });
```

The `OrderStatusConsumer`, along with the message contracts, is shown below:

```cs
public record GetOrderStatus
{
    public Guid OrderId { get; init; }
}

public record OrderStatus
{
    public Guid OrderId { get; init; }
    public string Status { get; init; }
}

class OrderStatusConsumer : IConsumer<GetOrderStatus>
{
    public async Task Consume(ConsumeContext<GetOrderStatus> context)
    {
        await context.RespondAsync<OrderStatus>(new
        {
            context.Message.OrderId,
            Status = "Pending"
        });
    }
}
```

Just like `Send`, the request is executed asynchronously. If an exception occurs, the exception will be propogated back to the caller. If the request times out, or if the request is canceled, the `GetResponse` method will throw an exception (either a `RequestTimeoutException` or an `OperationCanceledException`).

## [Middleware](https://masstransit.io/documentation/concepts/mediator#middleware)

MassTransit Mediator is built using the same components used to create a bus, which means all the same middleware components can be configured. For instance, to configure the Mediator pipeline, such as adding a scoped filter:

```cs
public class ValidateOrderStatusFilter<T>
: IFilter<SendContext<T>> where T : class
{
    public void Probe(ProbeContext context) { }

    public Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        if (context.Message is GetOrderStatus getOrderStatus && getOrderStatus.OrderId == Guid.Empty)
            throw new ArgumentException("The OrderId must not be empty");

        return next.Send(context);
    }
}
```

```cs
cfg.ConfigureMediator((context, cfg) =>
{
    cfg.UseSendFilter(typeof(ValidateOrderStatusFilter<>), context);
});
```