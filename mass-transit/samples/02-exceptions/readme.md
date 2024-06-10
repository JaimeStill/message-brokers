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

5. Demonstrate consuming [Faults](https://masstransit.io/documentation/concepts/exceptions#faults) to communicate exception information.

The sections that follow will demonstrate the details of each agenda item indicated above. To get started, simply run `dotnet run` from this directory and navigate to http://localhost:5000/swagger to follow along.

## Base Infrastructure

* [`MtExceptions.csproj`](./MtExceptions.csproj#L9) - Dependencies
* [`appsettings.json`](./appsettings.json#L9) - MassTransit configuration
* [`RabbitMqHostConfig`](./Events/Configuration/RabbitMqHostConfig.cs)
* [`Program.cs`](./Program.cs#L20) - MassTransit registration
* [`Program.cs`](./Program.cs#L80) - `ErrorProducer` registration

## Standard Exception

With the default bus configuration, if an exception is thrown by a consumer, the exception is caught by middleware in the transport (the `ErrorTransportFilter`) and the message is moved to an `_error` queue (prefixed by the receive endpoint queue name). The exception details are stored as headers with the message for analysis and to assist in troubleshooting the exception.

Provide a message to the `/api/Error/Doom/{message}` endpoint of the API swagger interface (http://localhost:5000/swagger), and submit the HTTP request.

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/c3573006-894b-4557-958b-6ea0310c2f33)

Upon execution, the API will return a `200` status code. However, if you inspect the terminal where the API is running, you'll see the following exception logged:

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/0198d0cf-4e3a-4877-9635-4f24353f4a82)

Navigate to the *queues* route on the RabbitMQ management interface at http://localhost:15672/#/queues. Here, you will see that a `doomed_error` queue has been generated:

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/b658b21e-7ead-4c1d-b90d-ca94540e423e)

Click into the `doomed_error` queue and you'll see that there is a message queued:

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/a6f6e847-94f3-42ed-a1d7-a8f5e32551cf).

Scroll down and expand the **Get messages** section. With the default settings:

* **Ack Mode**: Nack message requeue true
* **Encoding**: Auto string / base64
* **Messages**: 1

click the *Get Message(s)* button to retrieve the fault details:

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/96bfa87d-82f0-4f70-aeda-6f40ffc8df57)

### Standard Exception Infrastructure

1. [`DoomedMessage`](./Events/Messages/DoomedMessage.cs)
2. [`DoomedConsumer`](./Events/Consumers/DoomedConsumer.cs)
3. [`ErrorProducer.Doom`](./Events/Producers/ErrorProducer.cs#L9)
4. [`ErrorController.Doom`](./Controllers/ErrorController.cs#L13)
5. [`Program.cs`](./Program.cs#L25) - Consumer registration

## Retry

The `UseMessageRetry` method is an extension method that configures a middleware filter, in this case the `RetryFilter`. This allows operstions to be retried before moving the message to the error queue.

The `retry` endpoint is configured as follows:

```cs
cfg.ReceiveEndopint("retry", e =>
{
    /*
        Process up to 4 times before faulting.
        The initial attempt + 3 retries.
    */
    e.UseMessageRetry(r => r.Interval(3, 1000));
})
```

This means that the message will be consumed up to 4 times before faulting: the initial attempt + 3 retries.

Provide the following values to the `/api/Error/Retry/{message}/{iterations}` endpoint of the API swagger interface (http://localhost:5000/swagger), and submit the HTTP request:

* **message**: Try to connect
* **iterations**: 3

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/432514bc-37f0-42db-85e7-9aa7331c9900)

If you inspect the terminal where the API is running, you'll see the following logs:

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/6fea87ad-fed3-4170-9343-a287ed8623f5)

If you modify the **iterations** to `4` and resubmit the HTTP request, you will see the following logs:

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/f58fcf99-f59b-4ac0-81d4-093900b31d0b)

This time, the number of iterations where a failure is encountered surpasses the amount of retries. This means that the message is never successfully consumed and a fault is posted to the error queue:

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/761c24ba-4d7e-4561-86d5-307482aeca6f)

### Retry Infrastructure

1. [`RetryMessage`](./Events/Messages/RetryMessage.cs)
2. [`RetryConsumer`](./Events/Consumers/RetryConsumer.cs)
3. [`ErrorProducer.Retry`](./Events/Producers/ErrorProducer.cs#L21)
4. [`ErrorController.Retry`](./Controllers/ErrorController.cs#L61)
5. [`Program.cs`](./Program.cs#L30) - Consumer registration
6. [`Program.cs`](./Program.cs#L66) - Endpoint configuration

## Exception Filters

Sometimes you do not want to always retry, but instead only retry when some specific exception is thrown and fault for all other exceptions. To implement this, you can use an exception filter. Specify exception types using either the `Handle` or `Ignore` method. A filter can have either *Handle* or *Ignore* statements, combining them has unpredictable effects.

The exception filter is configured as follows:

```cs
cfg.ReceiveEndpoint("filter", e =>
{
    e.UseMessageRetry(r =>
    {
        /*
            Immediately retry up to 3 times for all Exceptions
            other than an ArgumentException with a ParamName
            value of volatile.
        */
        r.Immediate(3);
        r.Ignore<ArgumentException>(err =>
            err.ParamName?.ToLower() == "volatile"
        );
    });

    e.ConfigureConsumer<FilterConsumer>(context);
})
```

This configuration means that the `filter` endpoint will immediately retry 3 times for any `Exception` that is not of type `ArgumentException` with a `ParamName` equal to the value *volatile*.

To see this in action, Provide the following values to the `/api/Error/Filter/{message}/{parameter}/{iterations}` endpoint of the API swagger interface (http://localhost:5000/swagger), and submit the HTTP request:

* **message**: Try to connect to an itermittent endpoint
* **parameter**: intermittent
* **iterations**: 3

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/78616c3b-94e3-4ba8-95c1-0c130271551e)

If you inspect the terminal where the API is running, you'll see the following logs:

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/e0651c60-8d37-4f5c-a124-58d1491b8d2e)

Here, you can see that the message was retried before finally being consumed. If you modify the **parameter** to `volatile` and resubmit the HTTP request, you will see the following logs:

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/8abe3e83-b62b-4b5f-80e5-6cbc761bb788)

This time, the message is not retried and a fault is sent to the error queue:

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/05ad4dfa-f042-4ab3-9a21-3fb94114476e)

### Exception Filters Infrastructure

1. [`FilterMessage`](./Events/Messages/FilterMessage.cs)
2. [`FilterConsumer`](./Events/Consumers/FilterConsumer.cs)
3. [`ErrorProducer.Filter`](./Events/Producers/ErrorProducer.cs#L15)
4. [`ErrorController.Filter`](./Controllers/ErrorController.cs#L27)
5. [`Program.cs`](./Program.cs#L27) - Consumer registration
6. [`Program.cs`](./Program.cs#L38) - Endpoint configuration

## Redelivery

> Be sure you have followed the [setup instructions](#setup) before following along with this section.

Redelivery is a form of retry (some refer to it as *second-level retry*) where the message is removed from the queue and then redelivered to the queue at a future time.

Redelivery is configured as follows:

```cs
cfg.ReceiveEndopint("redelivery", e =>
{
    /*
        Retry 3 times at an interval of 500ms before
        delaying 5 seconds between 3 more retries.
    */ 
    e.UseDelayedRedelivery(r => r.Interval(3, 5000));
    e.UseMessageRetry(r => r.Interval(3, 500));
    e.ConfigureConsumer<RedeliveryConsumer>(context);
}
```

This means to retry 3 times at an interval of 500ms. If the message has still not been consumed, wait 5 seconds before retrying 3 times at an interval of 500ms. This full process is executed three times.

After each delayed redelievery interval, the message state is back to the way it was when the first delivery was attempted. This means that state cannot carry over between delayed redelivery intervals. To account for this, I wrote some variability into the consumer to allow scenarios where the delayed redelivery is triggered before the message is consumed:

```cs
Random rng = new();
int result = rng.Next(1, 7); // random number min: 1, max: 6
if (result == 6)
{
    logger.LogInformation(
        "Message Received: {Message}.",
        context.Message.Value
    );

    return Task.CompletedTask;
}
else
{
    /*
        this number will never exceed 3 because it is reset
        at the start of a delayed redelivery interval.
    */
    context.Message.Retries += 1;
    throw new InvalidOperationException(context.Message.Alert);
}
```

Provide the following values to the `/api/Error/Redelivery/{message}/{alert}` endpoint of the API swagger interface (http://localhost:5000/swagger), and submit the HTTP request:

* **message**: Attempting to reach an intermittent service
* **alert**: Unable to connect to intermittent service

The following logs indicate a message that hit a delayed redelivery interval:

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/af54a4b2-f8fe-4046-8e5d-8b91aee5c402)

* The consumer attempts to process the message: `Attempting to process message.`
* An exception is thrown: `warn: MassTransit.ReceiveTransport[0]...`
* Three subsequent retry intervals take place and an exception is thrown each time: `Attempting to process message: Retries: x`
* The delayed redelivery interval is hit, and the bus waits 5 seconds until retrying again: `warn: MassTransit.ReceiveTransport[0]...`
* On the first attempt after waiting the delayed redelivery interval, the message is successfully consumed: `Message Received: Attempting to reach an intermittent service.`

The following logs indicate a message that was consumed before a delayed redelivery interval:

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/d68c85bc-c0ff-480e-be33-a8274e2615dd)

### Redelivery Infrastructure

1. [`RedeliveryMessage`](./Events/Messages/RedeliveryMessage.cs)
2. [`RedeliveryConsumer`](./Events/Consumers/RedeliveryConsumer.cs)
3. [`ErrorProducer.Redelivery`](./Events/Producers/ErrorProducer.cs#L18)
4. [`ErrorController.Redelivery`](./Controllers/ErrorController.cs#L45)
5. [`Program.cs`](./Program.cs#L29) - Consumer registration
6. [`Program.cs`](./Program.cs#L56) - Endpoint configuration



## Faults

A `Fault<T>` is a generic message contract including the original message that caused the consuemr to fail, as well as the `ExceptionInfo`, `HostInfo`, and the time of the exception:

```cs
public interface Fault<T>
where T : class
{
    Guid FaultId { get; }
    Guid? FaultedMessageId { get; }
    DateTime Timestamp { get; }
    ExceptionInfo[] Exceptions { get; }
    HostInfo Host { get; }
    T Message { get; }
}
```

If the message headers specify a `FaultAddress`, the fault is sent directly to that address. If the `FaultAddress` is not present, but a `ResponseAddress` is specified, the fault is sent to the response address. Otherwise, the fault is published, allowing any subscribed consumers to receive it.

To observe faults, a consumer can consume fault messages the same as any other message. Here is the class signature of the `LogFaultConsumer`:

```cs
public class LogFaultConsumer(ILogger<LogFaultConsumer> logger)
: IConsumer<Fault<FaultyMessage>>
```

Provide a message to the `/api/Error/Fault/{message}` endpoint of the API swagger interface (http://localhost:5000/swagger), and submit the HTTP request:

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/d7cb28eb-e2f5-4cb6-ab3d-084b1f12c48e)

The resulting logs indicate that a message was received, a fault was generated, and the `LogFaultConsumer` logged the fault to `$env:TEMP\MassTransit\Faults\FaultMessage-{timestamp}.json`:

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/95eb97d0-05ae-4546-93fa-f62232c6c402)

Opening the generated JSON file shows the following values:

```json
{
    "Message": {
        "Value": "The boot disc was erased from the OS!"
    },
    "FaultId": "a8290000-84b5-b885-484d-08dc89912053",
    "FaultedMessageId": "a8290000-84b5-b885-e97c-08dc89912043",
    "Timestamp": "2024-06-10T21:05:58.535958Z",
    "Exceptions": [
        {
            "ExceptionType": "System.InvalidOperationException",
            "InnerException": null,
            "StackTrace": "   at MtExceptions.Events.Consumers.FaultyConsumer.Consume(ConsumeContext\u00601 context) in G:\\s2va\\message-brokers\\mass-transit\\samples\\02-exceptions\\Events\\Consumers\\FaultyConsumer.cs:line 16\r\n   at MassTransit.Middleware.MethodConsumerMessageFilter\u00602.MassTransit.IFilter\u003CMassTransit.ConsumerConsumeContext\u003CTConsumer,TMessage\u003E\u003E.Send(ConsumerConsumeContext\u00602 context, IPipe\u00601 next) in /_/src/MassTransit/Middleware/MethodConsumerMessageFilter.cs:line 28\r\n   at MassTransit.Configuration.PipeConfigurator\u00601.LastPipe.Send(TContext context) in /_/src/MassTransit.Abstractions/Middleware/Configuration/PipeBuilder.cs:line 123\r\n   at MassTransit.DependencyInjection.ScopeConsumerFactory\u00601.Send[TMessage](ConsumeContext\u00601 context, IPipe\u00601 next)\r\n   at MassTransit.DependencyInjection.ScopeConsumerFactory\u00601.Send[TMessage](ConsumeContext\u00601 context, IPipe\u00601 next) in /_/src/MassTransit/DependencyInjection/DependencyInjection/ScopeConsumerFactory.cs:line 22\r\n   at MassTransit.Middleware.ConsumerMessageFilter\u00602.MassTransit.IFilter\u003CMassTransit.ConsumeContext\u003CTMessage\u003E\u003E.Send(ConsumeContext\u00601 context, IPipe\u00601 next) in /_/src/MassTransit/Middleware/ConsumerMessageFilter.cs:line 48",
            "Message": "Throwing faulty message",
            "Source": "MtExceptions",
            "Data": null
        }
    ],
    "Host": {
        "MachineName": "{machine-name}",
        "ProcessName": "MtExceptions",
        "ProcessId": 10664,
        "Assembly": "MtExceptions",
        "AssemblyVersion": "1.0.0.0",
        "FrameworkVersion": "8.0.6",
        "MassTransitVersion": "8.2.2.0",
        "OperatingSystemVersion": "Microsoft Windows NT 10.0.22635.0"
    },
    "FaultMessageTypes": [
        "urn:message:MtExceptions.Events.Messages:FaultyMessage"
    ]
}
```

### Faults Infrastructure

1. [`FaultyMessage`](./Events/Messages/FaultyMessage.cs)
2. [`FaultyConsumer`](./Events/Consumers/FaultyConsumer.cs)
3. [`LogFaultConsumer`](./Events/Consumers/LogFaultConsumer.cs)
4. [`ErrorProducer.Fault`](./Events/Producers/ErrorProducer.cs#L12)
5. [`ErrorController.Fault`](./Controllers/ErrorController.cs#L20)
6. [`Program.cs`](./Program.cs#L26) - FaultyConsumer registration
7. [`Program.cs`](./Program.cs#L28) - LogFaultConsumer configuration