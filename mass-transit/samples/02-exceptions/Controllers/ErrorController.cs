using Microsoft.AspNetCore.Mvc;
using MtExceptions.Events.Producers;

namespace MtExceptions.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ErrorController(ErrorProducer producer) : ControllerBase
{
    readonly ErrorProducer producer = producer;

    [HttpGet("[action]/{message}")]
    public async Task<IActionResult> Doom([FromRoute]string message)
    {
        await producer.Doom(new() { Value = message});
        return Ok();
    }

    [HttpGet("[action]/{message}/{iterations:int}")]
    public async Task<IActionResult> Retry([FromRoute]string message, [FromRoute]int iterations)
    {
        await producer.Retry(new()
        {
            Value = message,
            Iterations = iterations,
            Attempts = 1
        });

        return Ok();
    }

    [HttpGet("[action]/{message}/{parameter}/{iterations:int}")]
    public async Task<IActionResult> Filter(
        [FromRoute]string message,
        [FromRoute]string parameter,
        [FromRoute]int iterations
    )
    {
        await producer.Filter(new()
        {
            Value = message,
            Parameter = parameter,
            Iterations = iterations,
            Attempts = 1
        });

        return Ok();
    }

    [HttpGet("[action]/{message}/{alert}")]
    public async Task<IActionResult> Redelivery(
        [FromRoute]string message,
        [FromRoute]string alert
    )
    {
        await producer.Redelivery(new()
        {
            Value = message,
            Alert = alert,
            Retries = 0
        });

        return Ok();
    }
}