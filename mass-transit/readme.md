# MassTransit

MassTransit is an open-source distributed application framework for .NET that provides a consistent abstraction on top of the supported message transports. The interfaces provides by MassTransit reduce message-based application complexity and allow developers to focus their effort on adding business value.

* [Getting Started](./getting-started/)
* [Documentation](./docs/)
* [Experiments](./experiments/)

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