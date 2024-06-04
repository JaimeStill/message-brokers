namespace MassTransitApi.Events.Messages;
public record SyncMessage
{
    public string Value { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public uint State { get; init; }
}