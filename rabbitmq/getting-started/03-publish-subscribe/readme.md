# [Publish / Subscribe](https://www.rabbitmq.com/tutorials/tutorial-three-dotnet)

The assumption behind a work queue is that each task is delivered to exactly one worker. In this part, we'll do something completely different -- we'll deliver a message to multiple consumers. This pattern is known as "publish / subscribe".

To illustrate the pattern, we're going to build a simple logging system. It will consist of two programs -- the first will emit log message and the second will receive and print them.

In our logging system every running copy of the receiver program will get the messages. That way we'll be able to run one receiver and direct the logs to disk; and at the same time be able to run another receiver and see the logs on the screen.

Essentially, published log messages are going to be broadcast to all the receivers.

This tutorial will focus on the [fanout exchange](../../readme.md#exchanges) called `logs`:

```cs
channel.ExchangeDeclare(
    "logs",
    ExchangeType.Fanout
);
```

The fanout exchange is very simple. It just broadcasts all the messages it receives to all the queues it knows.

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/8b177770-1dfc-4a64-a7e8-812ba648d8de)
