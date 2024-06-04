using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using MassTransit;
using MassTransitApi.Events.Configuration;
using MassTransitApi.Events.Producers;

var builder = WebApplication.CreateBuilder(args);

builder
    .Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddMassTransit(mt =>
{
    mt.SetKebabCaseEndpointNameFormatter();

    Assembly? ea = Assembly.GetEntryAssembly();

    mt.AddConsumers(ea);
    
    mt.UsingRabbitMq((context, cfg) =>
    {

        RabbitMqHostConfig
            .Load("MassTransit:RabbitMq", builder.Configuration)
            .Configure(cfg);

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddScoped<SyncProducer>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.MapControllers();

app.Run();