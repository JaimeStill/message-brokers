using System.Reflection;
using InMemoryWorker;
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

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
