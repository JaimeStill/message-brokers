using MassTransit;
using SqlWorker.Contracts;

namespace SqlWorker;

public class Worker(IBus bus) : BackgroundService
{
    readonly IBus bus = bus;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await bus.Publish(new SqlContract
            {
                Value = $"The time is {DateTimeOffset.Now}"
            }, stoppingToken);

            await Task.Delay(1000, stoppingToken);
        }
    }
}