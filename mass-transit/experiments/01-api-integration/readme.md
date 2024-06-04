# API Integration

POST a message to an API controller, which will in turn broadcast the message using a MassTransit producer. MassTransit will be configured to use RabbitMQ.

## Experiment

> The experiment represents what you are trying to accomplish with this lab. It is written before any of the lab code.

1. A [message](https://masstransit.io/documentation/concepts/messages) should be posted to a controller endpoint.

2. The message should be published using a [producer](https://masstransit.io/documentation/concepts/producers), which is injected into the API controller that received the message.

3. A [consumer](https://masstransit.io/documentation/concepts/consumers) will receive the message and log it to the console.

## Result

> The result captures the results of the experiment and describes the infrastructure.

A `SyncMessage` is able to be posted to `http://localhost:5000/api/Sync`. The message is then broadcast through a `SyncProducer` service. The broadcast message is handled by the `SyncConsumer`.

https://github.com/JaimeStill/JaimeStill/assets/14102723/fb165426-526f-4ea5-aef9-87d3e9a38edf

* The [*Events*](./Events/) directory contains all of the MassTransit infrastructure.

* [`RabbitMqHostConfig`](./Events/Configuration/RabbitMqHostConfig.cs) is used to simplify the process of configuring the RabbitMQ transport host in [`Program.cs`](./Program.cs#L31). The configuration is retrieved from [`appsettings.json`](./appsettings.json#L10).

* [`SyncMessage`](./Events/Messages/SyncMessage.cs) contains the definition for the [message](https://masstransit.io/documentation/concepts/messages) contract.

* [`SyncConsumer`](./Events/Consumers/SyncConsumer.cs) contains the definition for the [consumer](https://masstransit.io/documentation/concepts/consumers) that handles events related to `SyncMessage`. It is registered in [`Program.cs`](./Program.cs#L26).

* [`SyncProducer`](./Events/Producers/SyncProducer.cs) contains the definition for the [producer](https://masstransit.io/documentation/concepts/producers) responsible for broadcasting received `SyncMessage` messages. It is registered as a service in [`Program.cs`](./Program.cs#L39) and injected into the [`SyncController`](./Controllers/SyncController.cs#L11).

* [`SyncController`](./Controllers/SyncController.cs) defines a single `POST` endpoint that receives a `SyncMessage`. The received `SyncMessage` is published through the injected `SyncProvider` service.