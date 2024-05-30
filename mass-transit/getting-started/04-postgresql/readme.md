# [PostgreSQL](https://masstransit.io/quick-starts/postgresql)

This tutorial will get you from zero to up and running with [SQL](https://masstransit.io/documentation/transports/sql) and MassTransit.

## Prerequisites

This quick start assumes that you have [PostgreSQL]() installed and running. Alternatively, you can run the Docker PostgreSQL image:

```bash
> $ docker run -p 5432:5432 postgres
```

## Configure PostgreSQL Logins

Create the following logins:

Name | Password | Privileges
-----|----------|-----------
**masstransit** | `H4rd2Gu3ss!` | `LOGIN`, `SUPERUSER`
**migrationuser** | `H4rderTooGu3ss!!` | `LOGIN`, `SUPERUSER`

## Create the Project

To create a service using MassTransit, create and configure a worker:

```bash
# initialize the worker service
dotnet new worker -n SqlWorker

# add MassTransit references
cd SqlWorker
dotnet add package MassTransit
dotnet add package MassTransit.SqlTransport.PostgreSQL
```

## Configure MassTransit

Modify [`Program.cs`](./SqlWorker/Program.cs) to configure MassTransit and PostgreSQL:

```cs
using System.Reflection;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

builder
    .Services
    .AddOptions<SqlTransportOptions>()
    .Configure(options =>
    {
        options.Host = "localhost";
        options.Database = "sample";
        options.Schema = "transport";
        options.Role = "transport";
        options.Username = "masstransit";
        options.Password = "H4rd2Gu3ss!";

        // credentials to run migrations
        options.AdminUsername = "migrationuser";
        options.AdminPassword = "H4rderTooGu3ss!!";
    });

builder.Services.AddPostgresMigrationHostedService();

builder.Services.AddMassTransit(mt =>
{
    mt.SetKebabCaseEndpointNameFormatter();
    mt.SetInMemorySagaRepositoryProvider();

    Assembly? ea = Assembly.GetEntryAssembly();

    mt.AddConsumers(ea);
    mt.AddSagaStateMachines(ea);
    mt.AddSagas(ea);
    mt.AddActivities(ea);

    mt.UsingPostgres((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
```

Setting | Description
--------|------------
`Host` | The host to connect to. We are using `localhost` to connect to the local PostgreSQL instance.
`Port` | We are using the default `5432`, so we aren't setting it.
`Database` | The name of the database to connect to.
`Schema` | The schema to place the tables and functions inside of.
`Role` | The role to assign for all created tables, functions, etc.
`Username` | The username of the user to login as for normal operations.
`Password` | The password of the user to login as for normal operations.
`AdminUsername` | The username of the admin user to login as when running migration commands.
`AdminPassword` | The password of the admin user to login as when running migration commands.

## Create a Contract

Create a `Contracts` folder in the root of the project, and within that folder, create a file named `SqlContract`:

```cs
namespace SqlWorker.Contracts;
public record SqlContract
{
    public string Value { get; init; } = string.Empty;
}
```

## Add a Background Service

In the root of the project, add `Worker.cs`:

```cs
using MassTransit;
using SqlWorker.Contracts;

namespace SqlWorker;

public class Worker(IBus bus) : BackgroundService
{
    readonly IBus bus = bus;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await bus.Publish(new SqlContract
            {
                Value = $"The time is {DateTimeOffset.Now}"
            }, stoppingToken);

            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

In [`Program.cs`](./SqlWorker/Program.cs), register the `Worker` service:

```cs
using SqlWorker;

// AddMassTransit...

builder.Services.AddHostedService<Worker>();

// Build and run host...
```

## Create a Consumer

Create a `Consumers` folder in the root of your project, and within that folder create a file named `SqlConsumer` with the following contents:

```cs
using MassTransit;
using SqlWorker.Contracts;

namespace SqlWorker.Consumers;

public class SqlConsumer(ILogger<SqlConsumer> logger) : IConsumer<SqlContract>
{
    readonly ILogger<SqlConsumer> logger = logger;

    public Task Consume(ConsumeContext<SqlContract> context)
    {
        logger.LogInformation($"Received Text: {context.Message.Value}");
        return Task.CompletedTask;
    }
}
```

## Run the Project

Run the `SqlWorker` project:

```bash
dotnet run
```

The output should have changed to show the message consumer generating the output. Use <kbd>Ctrl+C</kbd> to exit.

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/2a8e3dd8-6379-4ff0-8ed5-502c717caa28)

At this point the service is connecting to PostgreSQL on *localhost* and publishing messages which are received by the consumer.