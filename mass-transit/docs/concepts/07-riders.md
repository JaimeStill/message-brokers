# [Riders](https://masstransit.io/documentation/concepts/riders)

MassTransit includes full support for several transports, most of which are traditional message brokers. RabbitMQ, ActiveMQ, and Azure Service Bus all support topics and queues, as does Amazon SQS when combined with SNS. They all support dispatch and lock messages while they are consumed, and if the client is disconnected, the message is requeued so it is handled by another consumer. They all remove messages from the queue as they are consumed.

Meanwhile, event streaming has become mainstream, and both Kafka and Event Hub are now commonly used. Coming up with a solution to support these message delivery platforms in MassTransit has been challenging, as many of the concepts and idioms do not apply. New patterns are needed when processing event streams, and it is important to differentiate dispatch style brokers from these new platforms.

**Riders**, introduced with MassTransit v7, provide a new way to deliver messages from any source to a bus. Riders are configured along with a bus, and board the bus when it is started. Riders have access to receive endopints, can senda nd publish messages, and if supported they can *produce* messages as well.

To interact with the bus, riders should use either `ConsumeContext`, `ISendEndpointProvider`, or `IPublishEndpoint` - the implementations of these interfaces will transfer contextual headers to the outgoing messages and will be injected into the consumer as its dependencies by the container.

To produce messages, the rider-specific producer interfaces should be used (if available). For example, the Kafka rider includes the `ITopicProducer` interface.

## [Kafka](https://masstransit.io/documentation/concepts/riders#kafka)

Kafka topics can be consumed using MassTransit consumers and sagas, including saga state machines, and messages can be produced to Kafka topics. For details, refer to the [**Kafka Rider documentation**](https://masstransit.io/documentation/configuration/transports/kafka).

## [Azure Event Hub](https://masstransit.io/documentation/concepts/riders#azure-event-hub)

Event hubs can be consumed using MassTransit consumers and sagas, including saga state machines, and messages can be produced to event hubs. For details, refer to the [**Event Hub Rider documentation**](https://masstransit.io/documentation/configuration/transports/azure-event-hub).