# [Producers](https://masstransit.io/documentation/concepts/producers)

An application or service can produce messages using two different methods. A message can be sent or a message can be published. The behavior of each method is very different, but it's easy to understand by looking at the type of messages involved with each particular method.

When a messages is sent, it is delivered to a specific endpoint using a *DestinationAddress*. When a message is published, it is not sent to a specific endpoint, but is instead broadcast to any consumers which have *subscribed* to the message type. For these two separate behaviors, we describe messages sent as commands, and messages published as events.

## [Send](https://masstransit.io/documentation/concepts/producers#send)

To send a message, the `DestinationAddress` is used to deliver the message to an endpoint - such as a queue. One of the `Send` method overloads on the `ISendEndpoint` interface is called, which will then send the message to the transport. An `ISendEndpoint` is obtained from one of the following objects:

1. The `ConsumeContext` of the message being consumed
    * This ensures that the correlation headers, message headers, and trace information is propogated to the sent message.
2. An `ISendEndpointProvider` instance
    * This may be passed as an argument, but is typically specified on the constructor of an object that is resolved using a dependency injection container.
3. The `IBus`
    * The last resort, and should only be used for messages that are being sent by an *initiator* - a process that is initiating a business process.

Once the `Send` method has been called (only once or repeatedly to send a series of messages), the `ISendEndpoint` referene should fall out of scope.

For instance, an `IBus` instance is a send endpoint provider, but it should *never* be used by a consumer to obtain an `ISendEndpoint`. `ConsumeContext` can also provide send endpoints, and should be used since it is *closer* to the consumer.

> This cannot be stressed enough -- always obtain an `ISendEndpoint` from the closest scope. There is extensive logic to tie message flows together using conversation, correlation, and initiator identifiers. By skipping a level and going outside the closest scope, that critical information will be lost which prevents the useful trace identifiers from being propogated.

### [Send Endpoint](https://masstransit.io/documentation/concepts/producers#send-endpoint)

To obtain a send endpoint from a send endpoint provider, call the `GetSendEndpoint` method:

```cs
public record Submitorder
{
    public string OrderId { get; init; }
}

public async Task SendOrder(ISendEndpointProvider sendEndpointProvider)
{
    var endpoint = await sendEndpointProvider.GetSendEndpoint(_serviceAddress);

    await endpoint.Send(new SubmitOrder { OrderId = "123" });
}
```

#### Send with Timeout

If there is a connectivity issue between the application and the broker, the `Send` method will internally retry until the connection is restored, blocking the returned `Task` until the send operation completes. The `Send` method supports passing a `CancellationToken` that can be used to cancel the operation:

```cs
var timeout = TimeSpan.FromSeconds(30);
using var source = new CancellationTokenSource(timeout);

await endpoint.Send(
    new SubmitOrder { OrderId = "123" },
    source.Token
);
```

Typically, the `Send` call completes quickly, only taking a few milliseconds. If the token is canceled the send oepration will throw an `OperationCanceledException`.

### [Endpoint Address](https://masstransit.io/documentation/concepts/producers#endpoint-address)

An endpoint address is a fully-qualified URI which may include transport-specific details. For example, an endpoint on a local RabbitMQ server would be:

```
rabbitmq://localhost/input-queue
```

Transport-specific details may include query parameters, such as:

```
rabbitmq://localhost/input-queue?durable=false
```

This would configure the queue as non-durable, where messages would only be stored in memory and therefore would not survive a broker restart.

Starting with MassTransit v6, short addresses are supported. For instance, to obtain a send endponit for a queue on RabbitMQ, the caller would only have to specify:

```cs
GetSendEndpoint(new Uri("queue:input-queue"));
```

Each transport has a specific set of supported short addresses:

Short Address | RabbitMQ | Azure Service Bus | ActiveMQ | Amazon SQS
--------------|----------|-------------------|----------|-----------
`queue:name` | * | * | * | *
`topic:name` | | * | * | *
`exchange:name` | * | | | 

## [Publish](https://masstransit.io/documentation/concepts/producers#publish)

Messages are published similarly to how messages are sent, but in this case, a single `IPublishEndpoint` is used. The same rules for endpoints apply, the closest instance of the publish endpoint should be used. So the `ConsumeContext` for consumers, and `IBus` for applications that are published outside of a consumer context.

The same guidelines apply for publishing messages, the closest object should be used:

1. The `ConsumeContext` of the message being consumed
    * This ensures that the correlation headers, and trace information is propogated to the published message.
2. An `IPublishEndpoint` instance
    * This may be passed as an argument, but is typically specified on the constructor of an object that is resolved using a dependency injection contanier.
3. The `IBus`
    * The last resort, and should only be used for messages that are being published by an *initiator* - a process that is initiating a business process.

To publish a message:

```cs
public record OrderSubmitted
{
    public string OrderId { get; init; }
    public Datetime Orderdate { get; init; }
}

public async Task NotifyOrderSubmitted(IPublishEndpoint endpoint)
{
    await endopint.Publish<OrderSubmitted>(new()
    {
        OrderId = "27",
        OrderDate = DateTime.UtcNow
    });
}
```

If you are planning to publish messages from within your consumers, this example would suit better:

```cs
public class SubmitOrderConsumer(IOrderSubmitter submitter) : IConsumer<SubmitOrder>
{
    private readonly IOrderSubmitter submitter = submitter;

    public async Task Consume(IConsumeContext<SubmitOrder> context)
    {
        await submitter.Process(context.Message);

        await context.Publish<OrderSubmitted>(new()
        {
            OrderId = context.Message.OrderId,
            OrderDate = DateTime.UtcNow
        });
    }
}
```

## [Message Initialization](https://masstransit.io/documentation/concepts/producers#message-initialization)

Messages can be initialized by MassTransit using an anonymous object passed as an *object* to the *publish* or *send* methods. While originally designed to support the initialization of interface-bsed message types, anonymous objects can also be used to initialize message types defined using classes or records.

### [Object Properties](https://masstransit.io/documentation/concepts/producers#object-properties)

`Send`, `Publish`, and most of the methods that behave in similar ways (scheduling, responding to requests, etc.) all support passing an object of *values* which is used to set the properties on the specified interface. A simple example is shown below:

```cs
public record SubmitOrder
{
    public Guid OrderId { get; init; }
    public DateTime OrderDate { get; init; }
    public string OrderNumber { get; init; }
    public decimal OrderAmount { get; init; }
}
```

To send this message to an endpoint:

```cs
await sendpoint.Send<SubmitOrder>(new // <-- notice no ()
{
    OrderId = NewId.NextGuid(),
    OrderDate = DateTime.UtcNow,
    OrderNumber = "18001",
    OrderAmount = 123.45m
});
```

The anonymous object properties are matched by name and there is an extensive set of type conversions that may be used to match the types defined by the interface. Most numeric, string, and date / time conversions are supported, as well as several advanced conversions (including variables, and asynchronous `Task<T>` results).

Collections, including arrays, lists, and dictionaries, are broadly supported, including the conversion of list elements, as well as dictionary keys and values. For instance, a dictionary of (`int`, `decimal`) could be converted on the fly to (`long`, `string`) using the default format conversions.

Nested objects are also supported, for instance, if a property was of type `Address` and another anonymous object was created (or any type whose property names match the names of the properties on the message contract), those properties would be set on the message contract.

### [Interface Messages](https://masstransit.io/documentation/concepts/producers#interface-messages)

MassTransit supports interface message types and there are convenience methods to initialize an interface without requiring the creation of a class implementing the interface.

```cs
public interface SubmitOrder
{
    public string OrderId { get; init; }
    public DateTime OrderDate { get; init; }
    public decimal OrderAmount { get; init; }
}

public async Task SendOrder(ISendEndpoint endpoint)
{
    await endpoint.Send<SubmitOrder>(new
    {
        OrderId = "27",
        OrderDate = DateTime.UtcNow,
        OrderAmount = 123.45m
    });
}
```

### [Headers](https://masstransit.io/documentation/concepts/producers#headers)

Header values can be specified in the anonymous object using a double-underscore, *dunder*, property name. For instance, to set the message time-to-live, specify a property with the duration. Remember, any value that can be converted to a `TimeSpan` works.

```cs
public record GetOrderStatus
{
    public Guid OrderId { get; init; }
}

var response = await requestClient.GetResponse<OrderStatus>(new
{
    __TimeToLive = 15000, // 15 seconds, or in this case, 15000 milliseconds
    OrderId = orderId
});
```

To add a custom header value, a special property name format is used. In the name, underscorse are converted to dashes, and double underscores are converted to underscores:

```cs
var response = await requestClient.GetResponse<OrderStatus>(new
{
    __Header_X_B3_TraceId = zipkinTraceId,
    __Header_X_B3_SpanId = zipkinSpanId,
    OrderId = orderId
});
```

This would set the headers used by open tracing (or Zipkin, as shown above) as part of the request message so the service coudl share in the span / trace. In this case, `X-B3-TraceId` and `X-B3-SpanId` would be added to the message envelope, and depending upon the transport, copied to the transport headers as well.

### [Variables](https://masstransit.io/documentation/concepts/producers#variables)

MassTransit also supports variables, which are special types added to the anonymous object. Following the example above, the initialization could be changed to use variables for the `OrderId` and `OrderDate`. Variables are consistent throughout the message creation, using the same variable multiple times returns the value. For instance, the Id created to set the *OrderId* would be teh same used to set the *OrderId* in each item.

```cs
public record OrderItem
{
    public Guid OrderId { get; init; }
    public string ItemNumber { get; init; }
}

public record SubmitOrder
{
    public Guid OrderId { get; init; }
    public DateTime OrderDate { get; init; }
    public string OrderNumber { get; init; }
    public decimal OrderAmount { get; init; }
    public OrderItem[] OrderItems { get; init; }
}

await endpoint.Send<SubmitOrder>(new
{
    OrderId = InVar.Id,
    Orderdate = InVar.Timestamp,
    OrderAmount = "18001",
    OrderAmount = 123.45m,
    OrderItems = new[]
    {
        new { OrderId = InVar.Id, ItemNumber = "237" },
        new { OrderId = InVar.Id, ItemNumber = "762" }
    }
});
```

### [Async Properties](https://masstransit.io/documentation/concepts/producers#async-properties)

Message initializers are asynchronous which makes it possible to do some pretty cool thinsg, including waiting for *Task* input properties to complete and use the result to initialize the property:

```cs
public record OrderUpdated
{
    public Guid CorrelationId { get; init; }
    public DateTime Timestamp { get; init; }
    public Guid OrderId { get; init; }
    public Customer Customer { get; init; }
}

public async Task<CustomerInfo> LoadCustomer(Guid orderId)
{
    // work happens in here
}

await context.Publish<OrderUpdated>(new
{
    InVar.CorrelationId,
    InVar.Timestamp,
    OrderId = context.Message.OrderId,
    Customer = LoadCustomer(context.Message.OrderId)
});
```

The property initializer will wait for the task result and then use it to initialize the property (converting all the types, etc. as it would any other object).

> While it is of course possible to await the call to `LoadCusteomr`, properties are initialized in parallel, and thus, allowing the initializer to await the Task can result in better overall performance. Your mileage may vary, however.

## [Send Headers](https://masstransit.io/documentation/concepts/producers#send-headers)

There are a variety of headers available which are used for correlation and tracking of messages. It is also possible to override some default behaviors of MassTransit when a fault occurs. For instance, a fault is normally *published* when a consumer throws an exception. If instead the application wants faults delivered to a specific address, teh `FaultAddress` can be specified via a header:

```cs
public record SubmitOrder
{
    public string OrderId { get; init; }
    public DateTime OrderDate { get; init; }
    public decimal OrderAmount { get; init; }
}

public async Task SendOrder(ISendEndpoint endpoint)
{
    await endpoint.Send<SubmitOrder>(new
    {
        OrderId = "27",
        OrderDate = DateTime.UtcNow,
        OrderAmount = 123.45m
    }, context => context.FaultAddress = new Uri("rabbitmq://localhost/order_faults"));
}
```

Since a message initializer is being used, this can actually be simplified:

```cs
public async Task SendOrder(ISendEndpoint endpoint)
{
    await endpoint.Send<SubmitOrder>(new
    {
        OrderId = "27",
        OrderDate = DateTime.UtcNow,
        OrderAmount = 123.45m,

        // header names are prefixed with __, and types are converted as needed
        __FaultAddress = "rabbitmq://localhost/order_faults"
    });
}
```