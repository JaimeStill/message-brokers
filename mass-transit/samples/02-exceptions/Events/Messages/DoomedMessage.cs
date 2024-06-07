namespace MtExceptions.Events.Messages;

public record DoomedMessage
{
    public string Value { get; init; } = string.Empty;
}