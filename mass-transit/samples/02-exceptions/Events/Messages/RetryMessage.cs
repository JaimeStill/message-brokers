namespace MtExceptions.Events.Messages;

public record RetryMessage
{
    public string Value { get; init; } = string.Empty;
    public int Attempts { get; set; }
    public int Iterations { get; init; }
}