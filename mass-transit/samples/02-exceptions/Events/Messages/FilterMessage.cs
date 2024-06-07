namespace MtExceptions.Events.Messages;
public record FilterMessage
{
    public string Parameter { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Iterations { get; set; }
    public int Attempts { get; set; }
}