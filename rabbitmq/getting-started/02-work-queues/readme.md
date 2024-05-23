# [Work Queues](https://www.rabbitmq.com/tutorials/tutorial-two-dotnet)

In this tutorial, we'll create a *Work Queue* that will be used to distribute time-consuming tasks among multiple workers.

The main idea behind Work Queues (aka: *Task Queues*) is to avoid doing a resource-intensive task immediately and having to wait for it to complete. Instead we schedule the task to be done later. We encapsulate a *task* as a message and send it to a queue. A worker process running in the background will pop the tasks and eventually execute the job. When you run many workers the tasks will be shared between them.

This concept is especially useful in web applications where it's impossible to handle a complex task during a short HTTP request window.

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/8e4b6c17-535e-4f4a-9510-b62e2addb734)

## Message Acknowledgement

An acknowledgment is sent back to the consumer to tell RabbitMQ that a particular message has been received, processed and that RabbitMQ is free to delete it.

If a consumer dies (its channel is closed, connection is closed, or TCP connection is lost) wihtout sending an ack, RabbitMQ will understand that a message wasn't processed fully and will re-queue it. If there are other consumers online at the same time, it will then quickly redeliver it to another consumer. That way, you can be sure that no message is lost, even if the workers occasionally die.

Manual message acknowledgements are turned on by default. In previous examples, we explicitly turned them off by setting the `autoAck` parameter to `true`.

```cs
    Console.WriteLine("[x] Done");

    channel.BasicAck(
        deliveryTag: ea.DeliveryTag,
        multiple: false
    );
};

channel.BasicConsume(
    queue: "task_queue",
    autoAck: false,
    consumer: consumer
);
```

## Message Durability

Two things are required to make sure that messages aren't lost: we need to mark both the queue and messages as durable:

```cs
channel.QueueDeclare(
    queue: "tasks_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null
);
```

Now we need to mark our messages as persistent:

```cs
byte[] body = Encoding.UTF8.GetBytes(message);

IBasicProperties properties = channel.CreateBasicProperties();
properties.Persistent = true;
```

## Fair Dispatch

By default, RabbitMQ just dispatches a message when the message enters the queue. It doesn't look at the number of unacknowledged messages for a consumer, it just blindly dispatches every n-th message to the n-th consumer. In a situation with two workers, when all odd messages are heavy and even messages are light, one worker will be constantly busy and the other will do hardly any work at all.

In order to change this behavior, we can use the `BasicQos` methodw ith the `prefetchCount` = `1` setting. This tells RabbitMQ not to give more than one message to a worker at a time. In other words, don't dispatch a new message to a worker until it has processed and acknowledged the previous one. Instead, it will dispatch it to the next worker that is not still busy.

```cs
channel.QueueDeclare(
    queue: "task_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

channel.BasicQos(
    prefetchSize: 0,
    prefetchCount: 1,
    global: false
);
```

> If all of the workers are busy, your queue can fill up. You will want to keep an eye on that, and maybe add more workers, or have some other strategy.