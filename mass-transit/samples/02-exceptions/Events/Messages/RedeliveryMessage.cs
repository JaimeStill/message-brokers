namespace MtExceptions.Events.Messages;
public record RedeliveryMessage
{
    public string Value { get; init; } = string.Empty;
    public string Alert { get; init; } = string.Empty;
    public int Retries { get; set; }
}