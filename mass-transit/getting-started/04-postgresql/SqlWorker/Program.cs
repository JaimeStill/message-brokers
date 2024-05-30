using System.Reflection;
using MassTransit;
using SqlWorker;

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

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();