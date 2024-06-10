using System.Text.Json;
using MassTransit;
using MtExceptions.Events.Messages;

namespace MtExceptions.Events.Consumers;
public class LogFaultConsumer(ILogger<LogFaultConsumer> logger) : IConsumer<Fault<FaultyMessage>>
{
    readonly ILogger<LogFaultConsumer> logger = logger;

    public async Task Consume(ConsumeContext<Fault<FaultyMessage>> context)
    {
        string result = JsonSerializer.Serialize(context.Message);

        string temp = Path.Join(
            Path.GetTempPath(),
            "MassTransit",
            "Faults"
        );

        if (!Directory.Exists(temp))
            Directory.CreateDirectory(temp);

        string tempFile = Path.Join(
            temp,
            $"FaultMessage-{DateTime.Now.ToFileTime()}.json"
        );

        using StreamWriter writer = new(tempFile);
        await writer.WriteAsync(result);

        logger.LogInformation(
            "Fault logged at {Path}",
            new FileInfo(tempFile).FullName
        );
    }
}