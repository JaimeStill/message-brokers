# [Requests](https://masstransit.io/documentation/concepts/requests)

Request / response is a commonly used message pattern where one service sends a request to another service, continuing after the response is received. In a distributed system, this can increase the latency of an application since the service may be hosted in another process, on another machine, or may even be a remote service in another network. While in many cases it is best to avoid request / response use in distributed applications, particularly when the request is a command, it is often necessary and preferred over more complex solutions.

In MassTransit, developers use a *request client* to send or publish requests and wait for a response. The request client is asynchronous, and supports use of the `await` keyword since it returns a `Task`.

## [Message Contracts](https://masstransit.io/documentation/concepts/requests#message-contracts)

To use the request client, create two message contracts: one for the request and one for the response:

```cs
public record CheckOrderStatus
{
    public string OrderId { get; init; }
}

public record OrderStatusResult
{
    public string OrderId { get; init; }
    public DateTime Timestamp { get; init; }
    public short StatusCode { get; init; }
    public string StatusText { get; init; }
}
```

## [Request Consumer](https://masstransit.io/documentation/concepts/requests#request-consumer)

Request message can be handled by any consumer type, including consumers, sagas, and routing slips. In this case, the consumer below consumes the `CheckOrderStatus` message and responds with the `OrderStatusResult` message:

```cs
public class CheckOrderStatusConsumer(IOrderRepoository orders)
: IConsumer<CheckOrderStatus>
{
    readonly IOrderRepository orders = orders;

    public async Task Consume(ConsumeContext<CheckOrderStatus> context)
    {
        var oder = await orders.Get(context.Message.OrderId);

        if (order is null)
            throw new InvalidOperationException("Order not found");

        await context.RespondAsync<OrderStatusResult>(new
        {
            OrderId = order.Id,
            order.Timestamp,
            order.StatusCode,
            order.StatusText
        });
    }
}
```

If the `OrderId` is found in the repository, an `OrderStatusResult` message will be sent to the response address included with the request. The waiting client will handle the response and complete the returned `Task` allowing the requested application to continue.

If the `OrderId` was not found, the consumer throws an exception. MassTransit catches the exception, generates a `Fault<CheckOrderStatus>` message, and sends it to the response address. The request client handles the fault message and throws a `RequestFaultException` via the awaited `Task` containing the exception detail.

## [Request Client](https://masstransit.io/documentation/concepts/requests#request-client)

To use the request client, add the request client as a dependency as shwon in the example API controller below:

```cs
public class RequestController(IRequestClient<CheckOrderStatus> client)
: Controller
{
    IRequestClient<CheckOrderStatus> client = client;
    
    [HttpGet("{orderId}")]
    public async Task<IActionResult> Get(string orderId, CancellationToken cancellationToken)
    {
        var response = await _client.GetResponse<OrderStatusResult>(new { orderId }, cancellationToken);

        return Ok(response.Message);
    }
}
```

The controller method will send the request and return the order status after the response has been received.

If the `cancellationToken` passed to `GetResponse` is canceled, the request client will stop waiting for a response. However, the request message produced remains in the queue until it is consumed or the message time-to-live expires. By default, the message time-to-live is set to the request timeout (which defaults to 30 seconds).

### [Client Configuration](https://masstransit.io/documentation/concepts/requests#client-configuration)

A request client can be resolved using dependency injection for any valid message type, no configuration is required. By default, request messages are *published*  and should be consumed by only one consumer / receive endpoint connected to the message broker. Multiple consumers connected to to the same receive endpoint are fine, requests will be load balanced across the connected consumers.

To configure the request client for a message type, add the request client to the configuration explicitly:

```cs
services.AddMassTransit(mt =>
{
    // configure the consumer on a specific endpoint address
    mt.AddConsumer<CheckOrderStatusConsumer>()
      .Endpoint(e => e.Name = "order-status");

    // Sends the request to the specified address, instead of publishing it
    mt.AddRequestClient<CheckOrderStatus>(new Uri("exchange:order-status"));

    mt.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});
```

### [Request Headers](https://masstransit.io/documentation/concepts/requests#request-headers)

To create a request header and add a header to the `SendContext`, one option is to add an execute filter to the request pipeline.

```cs
await client.GetResponse<OrderStatusResult>(
    new GetOrderStatus{ OrderId = orderId },
    x => x.UseExecute(context =>
        context.Headers.Set("tenant-id", "some-value")
    )
);
```

Another option is to use the *object values* overload, which uses a message initializer, to specify the header value:

```cs
await client.GetResopnse<OrderstatusResult>(new
{
    orderId,
    __Header_Tenant_Id = "some-value"
});
```

### [Multiple Response Types](https://masstransit.io/documentation/concepts/requests#multiple-response-types)

Another powerful feature with the request client is the ability to support multiple (such as positive and negative) result types. For example, adding an `OrderNotFound` response type to the consumer as shown eliminates throwing an exception since a missing order isn't really a fault:

```cs
public class CheckOrderStatusConsumer(IOrderRepository orders)
: IConsumer<CheckOrderStatus>
{
    readonly IOrderRepository orders = orders;

    public async Task Consume(ConsumeContext<CheckOrderStatus> context)
    {
        var order = await orders.Get(context.Message.OrderId);

        return order is null
            ? await context.RespondAsync<OrderNotFound>(context.Message)
            : await context.RespondAsync<OrderStatusResult>(new
            {
                OrderId = order.Id,
                order.Timestamp,
                order.StatusCode,
                order.StatusText
            });
    }
}
```

The client can now wait for multiple response types (in this case, two) by using a little tuple magic:

```cs
var response = await client.GetResponse<OrderStatusResult, 
OrderNotFound>(new { OrderId = id });

if (response.Is(out Response<OrderStatusResult> orderResult))
{
    // do something with the order
}
else if (response.Is(out Response<OrderNotFound> notFoundResult))
{
    // the order was not found
}
```

This cleans up the processing, and eliminates the need to catch a `RequestFaultException`.

It's also possible to use some of the switch expressions via deconstruction, but this requires the response variable to be explicitly specified as `Response`.

```cs
Response response = await client
    .GetResponse<OrderStatusResult, OrderNotFound>(
        new { OrderId = id }
    );

// using a regular switch statement
switch (response)
{
    case (_, OrderStatusResult a) statusResult:
        // order found
        break;
    case (_, OrderNotFound b) notFoundResult:
        // order not found
        break;
}

// using a switch expression
bool accepted = response switch
{
    (_, OrderStatusResult a) => true,
    (_, OrderNotFound b) => false,
    _ => throw new InvalidOperationException()
};
```

### [Accept Response Types](https://masstransit.io/documentation/concepts/requests#accept-response-types)

The request client sets a message header, `MT-Request-AcceptType`, that contains the response types supported by the request client. This allows the request consumer to determine if the client can handle a response type, which can be useful as services evolve and new response types may be added to handle new conditions. For instance, if a consumer adds a new response type, such as `OrderAlreadyShipped`, if the response type isn't supported an exception may be thrown instead.

To see this in code, check out the client code:

```cs
var response = await client
    .GetResponse<OrderCanceled, OrderNotFound>(
        new CancelOrder()
    );

if (response.Is(out Reponse<OrderCanceled> canceled))
{
    return Ok();
}
else if (response.Is(out Response<OrderNotFound> notFound))
{
    return NotFound();
}
```

The original consumer, prior to adding the new response type:

```cs
public async Task Consume(ConsumeContext<CancelOrder> context)
{
    var order = orders.Get(context.Message.OrderId);

    if (order is null)
    {
        await context.RespondAsync<OrderNotFound>(
            new { context.Message.OrderId }
        );

        return;
    }

    order.Cancel();

    await context.RespondAsync<OrderCanceled>(
        new { context.Message.OrderId }
    );
}
```

Now, the new consumer that checks if the order has already shipped:

```cs
public async Task Consume(ConsumeContext<CancelOrder> context)
{
    var order = orders.Get(context.Message.OrderId);

    if (order is null)
    {
        await context.RespondAsync<OrderNotFound>(
            new { context.Message.OrderId }
        );

        return;
    }

    if (order.HasShipped)
    {
        if (context.IsResponseAccepted<OrderAlreadyShipped>())
        {
            await context.RespondAsync<OrderAlreadyShipped>(
                new {
                    context.Message.OrderId,
                    order.ShipDate
                }
            );

            return;
        }
        else
            throw new InvalidOperationException("The order has already shipped");
    }

    order.Cancel();

    await context.RespondAsync<OrderCanceled>(
        new { context.Message.OrderId }
    );
}
```

This way, the consumer can check the request client response types and act accordingly.

> For backwards compatibility, if the new `MT-Request-AcceptType` header is not found, `IsResponseAccepted` will return true for all message types.

### [Concurrent Requests](https://masstransit.io/documentation/concepts/requests#concurrent-requests)

If there were multiple requests to be performed, it is easy to wait on all results at the same time, benefiting from the concurrent operation.

```cs
public class RequestController(
    IRequestClient<RequestA> clientA,
    IRequestClient<RequestB> clientB
) : Controller
{
    IRequestClient<RequestA> clientA = clientA;
    IRequestClient<RequestB> clientB = clientB;

    public async Task<IActionResult> Get()
    {
        var resultA = clientA.GetResponse(new RequestA());
        var resultB = clientB.GetResponse(new RequestB());

        await Task.WhenAll(resultA, resultB);

        var a = await resultA;
        var b = await resultB;

        var model = new Model(a.Message, b.Message);

        return Ok(model);
    }
}
```

### [Request Handle](https://masstransit.io/documentation/concepts/requests#request-handle)

Client factories or the request client can also be used to create a request instead of calling `GetResponse`. This is an uncommon scenario, but is available as an option and may make sense depending on the situation. If a request is created (which returns a `RequestHandle<T>`), the request handle must be disposed after the request completes.

> Using `Create` returns a request handle, which can be used to set headers and other attributes of teh request before it is sent.

```cs
public interface IRequestClient<TRequest> where TRequest : class
{
    RequestHandle<TRequest> Create(
        TRequest request,
        CancellationToken cancellationToken,
        RequestTimeout timeout
    );
}
```

> For `RequestTimeout` three options are available: `None`, `Default`, and a factory with `RequestTimeout.After`. `none` would never be recommended since it would essentially wait forever for a response. There is always a releveant timeout, or you're using the wrong pattern.