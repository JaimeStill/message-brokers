using MassTransitApi.Events.Messages;
using MassTransitApi.Events.Producers;
using Microsoft.AspNetCore.Mvc;

namespace MassTransitApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController(SyncProducer producer) : ControllerBase
{
    readonly SyncProducer producer = producer;

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] SyncMessage message)
    {
        await producer.Sync(message);
        return Ok();
    }
}