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

This would configure teh queue as non-durable, where messages would only be stored in memory and therefore would not survive a broker restart.

### [Address Conventions](https://masstransit.io/documentation/concepts/producers#address-conventions)

## [Publish](https://masstransit.io/documentation/concepts/producers#publish)

## [Message Initialization](https://masstransit.io/documentation/concepts/producers#message-initialization)

### [Object Properties](https://masstransit.io/documentation/concepts/producers#object-properties)

### [Interface Messages](https://masstransit.io/documentation/concepts/producers#interface-messages)

### [Headers](https://masstransit.io/documentation/concepts/producers#headers)

### [Variables](https://masstransit.io/documentation/concepts/producers#variables)

### [Async Properties](https://masstransit.io/documentation/concepts/producers#async-properties)

## [Send Headers](https://masstransit.io/documentation/concepts/producers#send-headers)