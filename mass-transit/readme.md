# MassTransit

MassTransit is an open-source distributed application framework for .NET that provides a consistent abstraction on top of the supported message transports. The interfaces provides by MassTransit reduce message-based application complexity and allow developers to focus their effort on adding business value.

* [Getting Started](./getting-started/) - Tutorials from the MassTransit [Quick Starts](https://masstransit.io/quick-starts).
* [Samples](./samples) - Core concepts developed through studying the MassTransit [Documentation](https://masstransit.io/documentation/concepts).
* [Experiments](./experiments/) - Test ideas and develop a deeper understanding of concepts.

It provides:

* **Message Routing** - Type-based publish / subscribe and automatic broker topology configuration.
* **Exception Handling** - When an exception is thrown, messages can be retried, redelievered, or moved to an *error* queue.
* **Test Harness** - Fast, in-memory unit tests with consumed, published, and sent message observers.
* **Observability** - Native Open Telementry (OTEL) support for end-to-end activity tracing.
* **Dependency Injection** - Service collection configuration and scope service provider management.
* **Scheduling** - Schedule message delivery using transport delay, Quartz.Net, and Hangfire.
* **Sagas, State Machines** - Reliable, durable, event-driven workflow orchestration.
* **Routing Slip Activities** - Distributed, fault-tolerant transaction choreography with compensation.
* **Request, Response** - Handle requests with fast, automatic response routing.

## Core Concepts

* **Messages** - A contract defined *code first* by creating a .NET type. A message can be defined using a record, class, or interface. Messages should only consist of properties; methods and other behavior shoudl not be included.
* **Consumers** - *Consumes* one or more message types when configured on or connected to a receive endopint. MassTransit includes many consumer types: consumers, [sagas](https://masstransit.io/documentation/patterns/saga), saga state machines, [routing slip activities](https://masstransit.io/documentation/patterns/routing-slip), handlers, and [job consumers](https://masstransit.io/documentation/patterns/job-consumers).
* **Producers** - A service that can produce messages using two different methods: a message can be sent, or a message can be published. When a message is sent, it is delivered to a specific endpoint using a `DestinationAddress`. When a message is published, it is not sent to a specific endpoint, but is instead broadcast to any consumers which have *subscribed* to the message type. For these two separate bheaviors, we describe messages sent as **commands**, and messages published as **events**.
* **Faults** - A `Fault<T>` is a generic message contract including the original message that caused a consumer to fail, as well as the `ExceptionInfo`, `HostInfo`, and the time of the exception.
* **Riders** - Provide a new way to deliver messages from any source to a bus. Riders are configured along with a bus, and board the bus when it is started. Riders have access to the receive endpoints, can send and publish messages, and if supported, can *produce* messages as well.
    * This was created specifically as a solution for supporting event streaming transports such as Apache Kafka and Azure Event Hub.