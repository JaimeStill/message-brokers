using System.Text.Json;
using System.Text.Json.Serialization;
using MassTransit;
using MtExceptions.Events.Configuration;
using MtExceptions.Events.Consumers;
using MtExceptions.Events.Producers;

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

    // add consumers
    mt.AddConsumer<DoomedConsumer>();
    mt.AddConsumer<FaultyConsumer>();
    mt.AddConsumer<FilterConsumer>();
    mt.AddConsumer<LogFaultConsumer>();
    mt.AddConsumer<RedeliveryConsumer>();
    mt.AddConsumer<RetryConsumer>();

    mt.UsingRabbitMq((context, cfg) =>
    {
        RabbitMqHostConfig
            .Load("MassTransit:RabbitMq", builder.Configuration)
            .Configure(cfg);

        cfg.ReceiveEndpoint("filter", e =>
        {
            /*
                Immediately retry up to 3 times for all Exceptions
                other than an ArgumentException with a ParamName
                value of volatile.
            */
            e.UseMessageRetry(r =>
            {
                r.Immediate(3);
                r.Ignore<ArgumentException>(err =>
                    err.ParamName?.ToLower() == "volatile"
                );
            });

            e.ConfigureConsumer<FilterConsumer>(context);
        });

        cfg.ReceiveEndpoint("redelivery", e =>
        {
            /*
                Retry 3 times at an interval of 500ms before
                delaying 5 seconds between 3 more retries.
            */
            e.UseDelayedRedelivery(r => r.Interval(3, 5000));
            e.UseMessageRetry(r => r.Interval(3, 500));
            e.ConfigureConsumer<RedeliveryConsumer>(context);            
        });

        cfg.ReceiveEndpoint("retry", e =>
        {
            /*
                Process up to 4 times before faulting.
                The initial attempt + 3 retries.
            */
            e.UseMessageRetry(r => r.Interval(3, 1000));            
            e.ConfigureConsumer<RetryConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddScoped<ErrorProducer>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.MapControllers();

app.Run();