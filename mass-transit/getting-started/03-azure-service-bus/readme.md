# [Azure Service Bus](https://masstransit.io/quick-starts/azure-service-bus)

This tutorial will get you from zero to up and running with [Azure Service Bus](https://masstransit.io/documentation/transports/azure-service-bus) and MassTransit.

## Prerequisites

This quick start assumes that you have a valid Azure subscription.

## Setup Azure Service Bus

1. Navigate to [Service Bus](https://portal.azure.com/#create/Microsoft.ServiceBus).

2. Create a namespace:

    Pricing tier **must** be Standard or Premium.

    ![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/c7894ce8-a3ec-4693-9359-75e4fa7ae48a)

3. Once the namespace is deployed, create a **Shared access policy**:

    Policy name is `MassTransitKey` with **Manage** permissions:

    ![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/b73314f2-c087-49ac-8634-42efd0967ac3)

    Select `MassTransitKey` and copy the *Primary Connection String*:

    ![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/131977a9-6b7d-4ac3-89e7-b1c63a752b5e)

## Create the Project

To create a service using MassTransit, create and configure a worker:

```bash
# initialize the worker service
dotnet new worker -n AsbWorker

# add MassTransit references
cd AsbWorker
dotnet add package MassTransit
dotnet add package MassTransit.Azure.ServiceBus.Core
```

## Configure MassTransit

Add the `MassTransitKey` connection string, copied above, to [`appsettings.json`](./AsbWorker/appsettings.json):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "ConnectionStrings": {
    "ServiceBus": "Endpoint=sb://jps-mt-test.servicebus.windows.net/;SharedAccessKeyName=MassTransitKey;SharedAccessKey=ZeTVM8rfNZVLvRfpLmUivBVTfxISmKcVk+ASbFaWEM0="
  }
}
```

Modify [`Program.cs`](./AsbWorker/Program.cs) to configure MassTransit and Azure Service Bus:

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

    mt.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(
            builder
                .Configuration
                .GetConnectionString("ServiceBus")
        );

        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
```

## Create a Contract

Create a `Contracts` folder in the root of the project, and within that folder, create a file named `AsbContract`:

```cs
namespace AsbWorker.Contracts;
public record AsbContract
{
    public string Value { get; init; } = string.Empty;
}
```

## Add a Background Service

In the root of the project, add `Worker.cs`:

```cs
using AsbWorker.Contracts;
using MassTransit;

namespace AsbWorker;

public class Worker(IBus bus) : BackgroundService
{
    readonly IBus bus = bus;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await bus.Publish(new AsbContract
            {
                Value = $"The time is {DateTimeOffset.Now}"
            }, stoppingToken);

            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

In [`Program.cs`](./AsbWorker/Program.cs), register the `Worker` service:

```cs
using AsbWorker;

// AddMassTransit...

builder.Services.AddHostedService<Worker>();

// Build and run host...
```

## Create a Consumer

Create a `Consumers` folder in the root of your project, and within that folder create a file named `AsbConsumer` with the following contents:

```cs
using AsbWorker.Contracts;
using MassTransit;

namespace AsbWorker.Consumers;

public class AsbConsumer(ILogger<AsbConsumer> logger) : IConsumer<AsbContract>
{
    readonly ILogger<AsbConsumer> logger = logger;

    public Task Consume(ConsumeContext<AsbContract> context)
    {
        logger.LogInformation($"Received Text: {context.Message.Value}");
        return Task.CompletedTask;
    }
}
```

## Run the Project

Run the `AsbWorker` project:

```bash
dotnet run
```

The output should have changed to show the message consuemr generating the output. Use <kdb>Ctrl+C</kbd> to exit.

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/f2342110-765d-4d64-8dbf-2ade7d960f04)

At this point, the service is connecting to Azure Service Bus and publishing messages which are received by the consumer.