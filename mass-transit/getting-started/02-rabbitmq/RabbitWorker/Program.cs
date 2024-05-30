using System.Reflection;
using MassTransit;
using RabbitWorker;

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

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
