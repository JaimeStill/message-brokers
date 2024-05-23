# [Routing](https://www.rabbitmq.com/tutorials/tutorial-four-dotnet)

In the [publish / subscribe](../03-publish-subscribe/) tutorial, we built a simple logging system that was able to broadcast log messages to many receivers. In this tutorial, we will add a feature to it - we're going to make it possible to subscribe only to a subset of the messages. For example, we will be able to direct only critical error messages to the log file (to save disk space), while still being able to print all of the log messages to the console.

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/d69709fd-bdf7-488a-b0a1-7f2f90b23ebe)

## Bindings

Bindings can take an extra `routingKey` parameter. To avoid the confusion with a `BasicPublish` parameter w'ere going to call it a `binding key`. This is how we could createa a binding with a key:

```cs
channel.QueueBind(
    queue: queueName,
    exchange: "direct_logs",
    routingKey: "black"
);
```

## Direct Exchange

The logging system from the previous tutorial broadcasts all messages to all consumers. We want to extend that to allow filtering messages based on their severity.

We were using a `fanout` exchange, which doesn't give us much flexibility - it's only capable of mindless broadcasting.

We will use a `direct` exchange instead. The routing algorithm behind a `direct` exchange is simple - a message goes to the queues who `binding key` exactly matches the `routing key` of the message.

It is perfectly legal to bind multiple queues with the same binding key. In that case, the `direct` exchange will behave like `fanout` and will broadcast the message to all the matching queues.

## Emitting Logs

Create the exchange:

```cs
channel.ExchangeDeclare(
    exchange: "direct_logs",
    type: ExchangeType.Direct
);
```

then send a message:

```cs
byte[] body = Encoding.UTF8.GetBytes(message);

channel.BasicPublish(
    exchange: "direct_logs",
    routingKey: severity,
    basicProperties: null,
    body: body
);
```

## Subscribing

Receiving messages will work just like in the previous tutorial, with one exception - we're going to create a new binding for each severity we're interested in:

```cs
string queueName = channel.DeclareQueue().QueueName;

foreach (string severity in args)
{
    channel.QueueBind(
        queue: queueName,
        exchange: "direct_logs",
        routingKey: severity
    );
}
```