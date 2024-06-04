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

### [Scoped Mediator](https://masstransit.io/documentation/concepts/mediator#scoped-mediator)

## [Connect](https://masstransit.io/documentation/concepts/mediator#connect)

## [Requests](https://masstransit.io/documentation/concepts/mediator#requests)

## [Middleware](https://masstransit.io/documentation/concepts/mediator#middleware)