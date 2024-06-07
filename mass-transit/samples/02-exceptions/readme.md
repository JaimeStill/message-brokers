# Exceptions

Establish a Web API that configures different exception handling scenarios. Each scenario should be testable through an API endpoint.

## Setup

Before running this sample, you must ensure that the [RabbitMQ Delayed Message Plugin](https://github.com/rabbitmq/rabbitmq-delayed-message-exchange) is installed.

To do this:

1. Download the [rabbitmq_server-<version>.ez](https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases) plugin.
    * The version should match the version of RabbitMQ that you are running. You can find this by running: `rabbitmq-diagnostics status`.
2. Move the downloaded file to `C:\Program Files\RabbitMQ Server\rabbitmq_server-<version>\plugins`.
3. Run the following to enable:
    ```cmd
    rabbitmq-plugins.bat enable rabbitmq_delayed_message_exchange
    ```

## Agenda

1. Demonstrate what happens by default when an exception is thrown from a consumer.

2. Establish an endpoint that demonstrates a [retry configuration](https://masstransit.io/documentation/concepts/exceptions#retry-configuration).

3. Demonstrate the use of [Exception Filters](https://masstransit.io/documentation/concepts/exceptions#exception-filters).

4. Demonstrate a [Redelivery](https://masstransit.io/documentation/concepts/exceptions#redelivery) strategy.

5. Demonstrate the use of [Outbox](https://masstransit.io/documentation/concepts/exceptions#outbox).

6. Demonstrate consuming [Faults](https://masstransit.io/documentation/concepts/exceptions#faults) to communicate exception information.

## Walkthrough