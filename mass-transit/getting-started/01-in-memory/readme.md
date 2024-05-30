# In-Memory

This tutorial will get you from zero to up and running with [In Memory](https://masstransit.io/documentation/transports/in-memory) and MassTransit.

## Create the Project

To create a service using MassTransit, create and configure a worker:

```bash
# initialize the worker service
dotnet new worker -n InMemoryWorker

# add MassTransit reference
cd InMemoryWorker
dotnet add package MassTransit
```

## Configure MassTransit

Modify [`Program.cs`](./InMemoryWorker/Program.cs) to configure MassTransit:

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

    mt.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
```

## Create a Contract

Create a `Contracts` folder in the root of the project, and within that folder, create a file named `MemoryContract`:

```cs
namespace InMemoryWorker.Contracts;
public record MemoryContract
{
    public string Value { get; init; } = string.Empty;
}
```

## Add a Background Service

In the root of the project, add `Worker.cs`:

```cs
using InMemoryWorker.Contracts;
using MassTransit;

namespace InMemoryWorker;

public class Worker(IBus bus) : BackgroundService
{
    readonly IBus bus = bus;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await bus.Publish(new MemoryContract
            {
                Value = $"The time is {DateTimeOffset.Now}"
            }, stoppingToken);

            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

In [`Program.cs`](./InMemoryWorker/Program.cs), register the `Worker` service:

```cs
using InMemoryWorker;

// AddMassTransit...

builder.Services.AddHostedService<Worker>();

// Build and run host...
```

## Create a Consumer

Create a `Consumers` folder in the root of your project, and within that folder create a file named `MemoryConsumer` with the following contents:

```cs
using InMemoryWorker.Contracts;
using MassTransit;

namespace InMemoryWorker.Consumers;

public class MemoryConsumer(ILogger<MemoryConsumer> logger) : IConsumer<MemoryContract>
{
    readonly ILogger<MemoryConsumer> logger = logger;

    public Task Consume(ConsumeContext<MemoryContract> context)
    {
        logger.LogInformation($"Received Text: {context.Message.Value}");
        return Task.CompletedTask;
    }
}
```

## Run the Project

Run the `InMemoryWorker` project:

```bash
dotnet run
```

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/9b402373-976d-4f92-b997-628d860fce8b)

The output should change to show the message consumer generating the output. Use <kbd>Ctrl+C</kbd> to exit.