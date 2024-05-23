# [Publish / Subscribe](https://www.rabbitmq.com/tutorials/tutorial-three-dotnet)

The assumption behind a work queue is that each task is delivered to exactly one worker. In this part, we'll do something completely different -- we'll deliver a message to multiple consumers. This pattern is known as "publish / subscribe".

To illustrate the pattern, we're going to build a simple logging system. It will consist of two programs -- the first will emit log message and the second will receive and print them.

In our logging system every running copy of the receiver program will get the messages. That way we'll be able to run one receiver and direct the logs to disk; and at the same time be able to run another receiver and see the logs on the screen.

Essentially, published log messages are going to be broadcast to all the receivers.

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/8b177770-1dfc-4a64-a7e8-812ba648d8de)

## Exchanges

In previous parts of the tutorial we sent and received messages to and from a queue. The core idea in the messaging model in RabbitMQ is that the producer never sends any messages directly to a queue. Actually, quite often the producer doesn't even know if a message will be delivered to any queue at all.

Instead, the producer can only send messages to an *exchange*. An exchange is a very simple thing. On one side it receives messages from producers and the other side it pushes them to queues. The exchange must know exactly what to do with a message it receives. The rules for that are defined by the *exchange type*.

There are a few exchange types available: `direct`, `topic`, `headers`, and `fanout`.

This tutorial will focus on the [fanout exchange](../../readme.md#exchanges) called `logs`:

```cs
channel.ExchangeDeclare(
    "logs",
    ExchangeType.Fanout
);
```

The fanout exchange is very simple. It just broadcasts all the messages it receives to all the queues it knows.

> **The default exchange**  
> In previous tutorials, we knew nothing about exchanges, but were still able to send messages to queues. That was possible because we were using a default exchange, which we identify by the empty string (`""`).
>
> Recall how we published a message before:
>
> ```cs
>string message = GetMessage(args);
>byte[] body = Encoding.UTF8.GetBytes(message);
>channel.BasicPublish(
>    exchange: string.Empty,
>    routingKey: "hello",
>    basicProperties: null,
>    body: body
>);
> ```

Now, we can publish to our named exchange instead:

```cs
channel.BasicPublish(
    exchange: "logs",
    routingKey: string.Empty,
    basicProperties: null,
    body: body
);
```

## Temporary Queues

Giving a queue a name is important when you want to share the queue between producers and consumers. But that's not the case for our lgoger. We want to hear about all log messages, not just a subset of them. We're also interested only in currently flowing messages not in the old ones. To solve that we need two things.

Firstly, whenever we connect to Rabbit we need a fresh, empty queue. To do this we could create a queue with a random name or, even better - let the server choose a random queue name for us.

Secondly, once we disconnect the consumer the queue should be automatically delete.

In the .NET client, when we supply no parameters to `QueueDeclare()` we create a non-durable, exclusive, autodelete queue with a generated name:

```cs
string queueName = channel.QueueDeclare().QueueName;
```

## Bindings

A binding is a relationship between an exchange and a queue. This can be simply read as: the queue is interested in messages from this exchange. We need to tell the exchange to send messages to our queue.

```cs
channel.QueueBind(
    queue: queueName,
    exchange: "logs",
    routingKey: string.Empty
);
```