# [RabbitMQ](https://masstransit.io/quick-starts/rabbitmq)

This tutorial will get you from zero to up and running with [RabbitMQ](https://masstransit.io/documentation/transports/rabbitmq)S and MassTransit.

## Prerequisites

This quick start assumes that you have gone through the [RabbitMQ Setup](../../../rabbitmq/readme.md#setup) and the RabbitMQ service is running on your machine.

## Create the Project

To create a service using MassTransit, create and configure a worker:

```bash
# initialize the worker service
dotnet new worker -n RabbitWorker

# add MassTransit references
cd RabbitWorker
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ
```

## Configure MassTransit

Modify [`Program.cs`](./RabbitWorker/Program.cs) to configure MassTransit and RabbitMQ:

```cs
using System.Reflection;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransit(mt =>
{
    mt.SetKebabCaseEndpointNameFormatter();
    mt.SetInMemorySagaRepositoryProvider();

    Assembly? ea = Assembly.GetEntryAssembly();

    mt.AddConsumers(ea);
    mt.AddSagaStateMachines(ea);
    mt.AddSagas(ea);
    mt.AddActivities(ea);

    mt.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
```

The host name, *localhost*, is where the RabbitMQ service is running. we are inferring the default port of *5672* and are using '/' as the [virtual host](https://www.rabbitmq.com/docs/vhosts). The default username *guest* and default password *guest* can be used to connect to the broker and sign in to the management interface - http://localhost:15672/.

## Create a Contract

Create a `Contracts` folder in the root of the project, and within that folder, create a file named `RabbitContract`:

```cs
namespace RabbitWorker.Contracts;
public record RabbitContract
{
    public string Value { get; init; } = string.Empty;
}
```

## Add a Background Service

In the root of the project, add `Worker.cs`:

```cs
using MassTransit;
using RabbitWorker.Contracts;

namespace RabbitWorker;

public class Worker(IBus bus) : BackgroundService
{
    readonly IBus bus = bus;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await bus.Publish(new RabbitContract
            {
                Value = $"The time is {DateTimeOffset.Now}"
            }, stoppingToken);

            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

In [`Program.cs`](./RabbitWorker/Program.cs), register the `Worker` service:

```cs
using RabbitWorker;

// AddMassTransit...

builder.Services.AddHostedService<Worker>();

// Build and run host...
```

## Create a Consumer

Create a `Consumers` folder in the root of your project, and within that folder create a file named `RabbitConsumer` with the following contents:

```cs
using MassTransit;
using RabbitWorker.Contracts;

namespace RabbitWorker.Consumers;

public class RabbitConsumer(ILogger<RabbitConsumer> logger) : IConsumer<RabbitContract>
{
    readonly ILogger<RabbitConsumer> logger = logger;

    public Task Consume(ConsumeContext<RabbitContract> context)
    {
        logger.LogInformation($"Received Text: {context.Message.Value}");
        return Task.CompletedTask;
    }
}
```

## Run the Project

Run the `RabbitWorker` project:

```bash
dotnet run
```

The output should have changed to show the message consumer generating the output. Use <kbd>Ctrl+C</kbd> to exit.

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/4619c98a-0ffd-45be-9de8-1b59fc990f1b)

At this point the service is connecting to RabbitMQ on *localhost* and publishing messages, which are received by the consumer.