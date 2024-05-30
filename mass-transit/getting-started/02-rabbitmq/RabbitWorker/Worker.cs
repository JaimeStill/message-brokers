using MassTransit;
using RabbitWorker.Contracts;

namespace RabbitWorker;

public class Worker(IBus bus) : BackgroundService
{
    readonly IBus bus = bus;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await bus.Publish(new RabbitContract
            {
                Value = $"The time is {DateTimeOffset.Now}"
            }, stoppingToken);

            await Task.Delay(1000, stoppingToken);
        }
    }
}