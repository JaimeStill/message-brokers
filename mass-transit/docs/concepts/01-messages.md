# [Messages](https://masstransit.io/documentation/concepts/messages)

A message contract is defined *code first* by creating a .NET type. A message can be defined using a record, class, or interface. Messages should only consist of properties; methods and other behavior should not be included.

## [Message Types](https://masstransit.io/documentation/concepts/messages#message-types)

Messages must be reference types, and can be defined using records, interfaces, or classes.

### [Records](https://masstransit.io/documentation/concepts/messages#records)

```cs
namespace Company.Application.Contracts;
public record UpdateCustomerAddress
{
    public Guid CommandId { get; init; }
    public DateTime Timestamp { get; init; }
    public string CustomerId { get; init; }
    public string HouseNumber { get; init; }
    public string Street { get; init; }
    public string City { get; init; }
    public string State { get; init; }
    public string PostalCode { get; init; }
}
```

### [Interfaces](https://masstransit.io/documentation/concepts/messages#interfaces)

```cs
namespace Company.Application.Contracts;
public interface UpdateCustomerAddress
{
    Guid CommandId { get; }
    DateTime Timestamp { get; }
    string CustomerId { get; }
    string HouseNumber { get; }
    string Street { get; }
    string City { get; }
    string State { get; }
    string PostalCode { get; }
}
```

### [Classes](https://masstransit.io/documentation/concepts/messages#classes)

```cs
namespace Company.Application.Contracts;
public class UpdateCustomerAddress
{
    public Guid CommandId { get; set; }
    public DateTime Timestamp { get; set; }
    public string CustomerId { get; set; }
    public string HouseNumber { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string PostalCode { get; set; }
}
```

## [Message Attributes](https://masstransit.io/documentation/concepts/messages#message-attributes)

Attribute | Description
----------|------------
[`EntityName`](https://masstransit.io/documentation/configuration/topology/message#entityname) | The exchange or topic name.
[`ExcludeFromTopology`](https://masstransit.io/documentation/configuration/topology/message#excludefromtopology) | Don't create an exchange or topic unless it is directly consumed or published.
[`ExcludeFromImplementedTypes`](https://masstransit.io/documentation/configuration/topology/message#excludefromimplementedtypes) | Don't create a middleware filter for the message type.
[`MessageUrn`](https://masstransit.io/documentation/configuration/topology/message#messageurn) | The message urn.

## [Message Names](https://masstransit.io/documentation/concepts/messages#message-names)

There are two main message types, *events* and *commands*. When choosing a name for a message, the type of message should dictate the tense of the message.

### [Commands](https://masstransit.io/documentation/concepts/messages#commands)

A command tells a service to do something, and typically a command should only be consumed by a single consumer. If you have a command, such as `SubmitOrder`, then you should have only one consumer that implements `IConsumer<SubmitOrder>` or one saga state machine with `Event<SubmitOrder`> configured.

Commands should be expressed in a verb-noun sequence, following the *tell* style:

* `UpdateCustomerAddress`
* `UpgradeCustomerAccount`
* `SubmitOrder`

### [Events](https://masstransit.io/documentation/concepts/messages#events)

An event signifies that something has happened. Events are **published** (using `Publish`) via either `ConsumeContext` (within a message consumer), `IPublishEndpoint` (within a container scope), or `IBus` (standalone).

Events should be expressed in a noun-verb (past tense) sequence, indicating that something happened. Some example event names may include:

* `CustomerAddressUpdated`
* `CustomerAccountUpgraded`
* `OrderSubmitted`, `OrderAccepted`, `OrderRejected`, `OrderShipped`

## [Message Headers](https://masstransit.io/documentation/concepts/messages#message-headers)

MassTransit encapsulated every sent or published message in a message envelope (described by the [**Envelope Wrapper**](https://www.enterpriseintegrationpatterns.com/patterns/messaging/EnvelopeWrapper.html) pattern). The envelope adds a series of message headers, including:

Property | Type | Description
---------|------|------------
`MessageId` | Auto | Generated for each message using `NewId.NextGuid`.
`CorrelationId` | User | Assigned by the application, or automatically by convention, and should uniquely identify the operation, event, etc.
`RequestId` | Request | Assigned by the request client, and automatically copied by the *Respond* methods to correlate responses to the original request.
`InitiatorId` | Auto | Assigned when publishing or sending from a consumer, saga, or activity to the value of the `CorrelationId` on the consumed message.
`ConversationId` | Auto | Assigned when the first message is sent or published and no consumed message is available, ensuring that a set of messages within the same conversation have the same identifier.
`SourceAddress` | Auto | Where the message originated (may be a temporary address for messages published or sent from `IBus`).
`DestinationAddress` | Auto | Where the message was sent.
`ResponseAddress` | Request | Where responses to the request should be sent. If not present, responses are *published*.
`FaultAddress` | User | Where consumer faults should be sent. If not present, faults are *published*.
`ExpirationTime` | User | When the message should expire, which may be used by the transport to remove the message if it isn't consumed by the expiration time.
`SentTime` | Auto | When the message was sent, in UTC.
`MessageType` | Auto | An array of message types, in a `MessageUrn` format, which can be deserialized.
`Host` | Auto | The host information of the machinet hat sent or published the message.
`Headers` | User | Additional headers, which can be added by the user, middleware, or diagnostic trace filters.

Message headers can be read using the `consumeContext` interface and specified using the `SendContext` interface.

## [Message Correlation](https://masstransit.io/documentation/concepts/messages#message-correlation)

Messages are usually part of a conversation and identifiers are used to connect messages to the conversation. In the previous section, the headers supported by MassTransit are used to combine separate messages into a conversation. OUtbound messages that are published or sent by a consuemr will have the same `ConversationId` as the consumed message. If the consumed message has a `CorrelationId`, that value will be copied to the `InitiatorId`. These headers capture the flow of messages involved in the conversation.

`CorrelationId` may be set, when appropriate, by the developer publishing or sending a message. `CorrelationId` can be set explicitly on a `PublishContext` or `SendContext` or when using a message initializer vai the `__CorrelationId` property.

To set the `CorrelationId` using the `SendContext`:

```cs
await endpoint.Send<SubmitOrder>(
    new { OrderId = InVar.Id },
    sendContext => sendContext.CorrelationId = context.Message.OrderId
);
```

To set the `CorrelationId` using a message initializer:

```cs
await endpoint.Send<SubmitOrder>(new
{
    OrderId = context.Message.OrderId,
    __CorrelationId = context.Message.OrderId
})
```

### [Correlation Conventions](https://masstransit.io/documentation/concepts/messages#correlation-conventions)

`CorrelationId` can also be set by convention. MassTransit includes several conventions by default, wihc may be used as the source to initialize the `CorrelationId` header:

1. If the message implements the `CorrelatedBy<Guid>` interface, which has a `GuidCorrelationId` property, its value will be used.

2. If the message has a property named `Correlationid`, `CommandId`, or `EventId` that is a `Guid` or `Guid?`, its value will be used.

3. If the developer registered a `CorrelationId` provider for the message type, it will be used to get the value.

The final convention requires the develoepr to register a `CorrelationId` provider prior to bus creation:

```cs
GlobalTopology.Send.UseCorrelationId<SubmitOrder>(x => x.OrderId);
```

The convention can also be specified during bus configuration. This convention applies only to the configured bus instance:

```cs
cfg.SendTopology.UseCorrelationId<SubmitOrder>(x => x.OrderId);
```

Registering `CorrelationId` providers should be done early in the application, prior to bus configuration. An easy approach is putting the registration methods into a class method and calling it during application startup.

### [Saga Correlation](https://masstransit.io/documentation/concepts/messages#saga-correlation)

Sagas *must* have a `CorrelationId`, it is the primary key used by the sage repository and the way messages are correlated to a specific saga instance. MassTransit follows the conventions above to obtain the `CorrelationId` used to create a new or load an existing saga instance. Newly created saga instances will be assigned the `CorrelationId` from the initiating message.

### [Identifiers](https://masstransit.io/documentation/concepts/messages#identifiers)

MassTransit uses and highly encourages the use of *Guid* identifiers. Distributed systems would crumble using monotonically incrementing identifiers (such as *int* or *long*) due to the bottleneck of lockign and incrementing a shared counter. Historically, SQL DBAs have argued against using *Guid* as a key - a clustered primary key in particular. However, wtih MassTransit, we solved that problem.

MassTransit uses [**NewId**](https://masstransit.io/documentation/patterns/newid) to generate identifiers that are unique, sequential, and represented as a *Guid*. The generated identifiers are clustered-index friendly, and are ordered so that SQL Server can efficiently insert them into a database with the *uniqueidentifier* as the primary key.

To create a *Guid*, call `Newid.NextGuid()` where you would otherwise call `Guid.NewGuid()` and enjoy the benefits of fast, distributed unique identifiers.

## [Guidance](https://masstransit.io/documentation/concepts/messages#guidance)

When defining message contracts, what follows is general guidance based upon years of using MassTransit combined with continued questions raised by developers new to MassTransit.

* **Good** - Use records, define properties such as `public` and specify `{ get; init; }` accesors. Create messages using the constructor / object initializer or a [**message initializer**](https://masstransit.io/documentation/concepts/producers#message-initialization).
* **Good** - Use interfaces, specifying only `{ get; }` accesors. Create messages using initializers and use the Roslyn Analyzer to identify missing or incompatible properties.
* **Good** - Limit the use of inheritance, pay attention to polymorphic message routing. A message type containing a dozen interfaces is a bit annoying to untangle if you need to delve deep into message routing to troubleshoot an issue.
* **Good** - Class inheritance has the same guidance as interfaces, but with more caution.
* **Warning** - Message design is not object-oriented design. Messages should contain state, not behavior. Behavior should be in a separate class or service.
* **Danger** - Consuming a base class type, and expecting polymorphic method behavior almost always leads to problems.
* **Danger** - A big base class may cause pain down the road as changes are made, particularly when supporting multiple message versions.

### [Message Inheritance](https://masstransit.io/documentation/concepts/messages#message-inheritance)

**Message design is not object-oriented design.**

By design, MassTransit treats your classes, records, and interfaces as a *contract*.

An example, let's say that you have a message that is defined by the dotnet class below:

```cs
public record SubmitOrder
{
    public string Sku { get; init; }
    public int Quantity { get; init; }
}
```

You want all of your messages to have a common set of properties, so you try and do this:

```cs
public record CoreEvent
{
    public string User { get; init; }
}

public record SubmitOrder : CoreEvent
{
    public string Sku { get; init; }
    public int Quantity { get; init; }
}
```

You try and consume a `Batch<CoreEvent>` and expect to get a variety of types, one of which would be `SubmitOrder`. In OOP land, that makes all the sense in the world, but in MassTransit contract design it does not. The application has said that it cares about batches of `CoreEvent` so it will only get back the single property `User`. This is not a symptom of using **System.Text.Json**, this has been the standard behavior of MassTransit since day one, even when using **Newtonsoft.Json**. MassTransit will always respect the contract that has been designed.

If you want to have a standard set of properties available, by all means use a base class, or bundlet hem up into a single property, our preference. If you want to subscribe to all implementations of a class, then you will need to subscribe to all implementations of a class.