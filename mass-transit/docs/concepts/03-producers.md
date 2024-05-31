# [Producers](https://masstransit.io/documentation/concepts/producers)

An application or service can produce messages using two different methods. A message can be sent or a message can be published. The behavior of each method is very different, but it's easy to understand by looking at the type of messages involved with each particular method.

When a messages is sent, it is delivered to a specific endpoint using a *DestinationAddress*. When a message is published, it is not sent to a specific endpoint, but is instead broadcast to any consumers which have *subscribed* to the message type. For these two separate behaviors, we describe messages sent as commands, and messages published as events.

## [Send](https://masstransit.io/documentation/concepts/producers#send)

### [Send Endpoint](https://masstransit.io/documentation/concepts/producers#send-endpoint)

### [Endpoint Address](https://masstransit.io/documentation/concepts/producers#endpoint-address)

### [Address Conventions](https://masstransit.io/documentation/concepts/producers#address-conventions)

## [Publish](https://masstransit.io/documentation/concepts/producers#publish)

## [Message Initialization](https://masstransit.io/documentation/concepts/producers#message-initialization)

### [Object Properties](https://masstransit.io/documentation/concepts/producers#object-properties)

### [Interface Messages](https://masstransit.io/documentation/concepts/producers#interface-messages)

### [Headers](https://masstransit.io/documentation/concepts/producers#headers)

### [Variables](https://masstransit.io/documentation/concepts/producers#variables)

### [Async Properties](https://masstransit.io/documentation/concepts/producers#async-properties)

## [Send Headers](https://masstransit.io/documentation/concepts/producers#send-headers)