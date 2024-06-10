namespace MtExceptions.Events.Messages;
public record FaultyMessage
{
    public string Value { get; init; } = string.Empty;
}