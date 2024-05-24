# [Remote Procedure Call (RPC)](https://www.rabbitmq.com/tutorials/tutorial-six-dotnet)

What if we need to run a function on a remote computer and wait for the result? This pattern is commonly known as *Remote Procedure Call* or *RPC*.

In this tutorial, we're going to use RabbitMQ to build an RPC system: a client and a scalable RPC server. As we don't have any time consuming tasks that are worth distributing, we're going to create a dummy RPC service that returns Fibonacci numbers.

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/ffab1f1f-08f1-4208-a057-a80c162dbee2)

## Callback Queue

In general doing RPC over RabbitMQ is easy. A client sends a request message and a server replies with a response message. In order to receive a response, we need to send a 'callback' queue address with the request:

```cs
IBasicProperties props = channel.CreateBasicProperties();
props.ReplyTo = replyQueueName;

byte[] body = Encoding.UTF8.GetBytes(message);
channel.BasicPublish(
    exchange: string.Empty,
    routingKey: "rpc_queue",
    basicProperties: props,
    body: body
);
```

## Correlation Id

In the method presented above, we suggest creating a callback queue for every RPC request. That's pretty inefficient, but fortunately there is a better way - let's create a single callback queue per client.

That raisies a new issue, having received a response in that queue it's not clear to which request the respones belongs. That's when the `CorrelationId` property is used. We're going to set it to a unique value for every request. Later, when we receive a message in the callback queue we'll look at this property, and based on that we'll be able to match a response with a request. If we see an unknown `CorrelationId` value, we may safely discard the message - it doesn't belong to our requests.

## Summary

The RPC will work like this:

* When the Client starts up, it creates an anonymous exclusive callback queue.
* For an RPC request, the Client sends a message with two properties: `ReplyTo`, which is set to the callback queue and `CorrelationId`, which is set to a unique value for every request.
* The request is sent to a `rpc_queue` queue.
* The RPC worker (aka: server) is waiting for requests on that queue. When a request appears, it does the job and sends a message with the result back to the Client, using the queue from the `ReplyTo` property.
* The Client waits for data on the callback queue. When a message appears, it checks the `CorrelationId` property. If it matches the value from the request it returns the response to the application.

The design presented here is not the only possible implementation of a RPC service, but it has some important advantages:

* If the RPC server is too slow, you can scale up by just running another one. Try running a second `Rpc.Server` in a new console.
* On the client side, the RPC requires sending and receiving only one message. No synchronous calls like `QueueDeclare` are required. As a result the RPC client needs only one network round trip for a single RPC request.

Our code is pretty simplistic and doesn't try to solve more complex (but important) problems, like:

* How should the client react if there are no servers running?
* Should a client have some kind of timeout for the RPC?
* If the server malfunctions and raises an exception, should it be forwarded to the client?
* Protecting against invalid incoming messages (e.g. checking bounds, type) before processing.