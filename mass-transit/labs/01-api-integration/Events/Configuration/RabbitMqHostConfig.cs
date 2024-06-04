using MassTransit;

namespace MassTransitApi.Events.Configuration;
public record RabbitMqHostConfig
{
    public string Host { get; init; } = string.Empty;
    public string VirtualHost { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    public static RabbitMqHostConfig Load(string key, IConfiguration config) =>
        config
            .GetRequiredSection(key)
            .Get<RabbitMqHostConfig>()
        ?? throw new Exception($"MassTransit: No RabbitMQ configuration found for {key}");

    public void Configure(IRabbitMqBusFactoryConfigurator cfg) =>
        cfg.Host(Host, VirtualHost, h =>
        {
            h.Username(Username);
            h.Password(Password);
        });
}