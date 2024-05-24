# [Publisher Confirms](https://www.rabbitmq.com/tutorials/tutorial-seven-dotnet)

[Publisher confirms](https://www.rabbitmq.com/docs/confirms#publisher-confirms) are a RabbitMQ extension to implement reliable publishing. When publisher confirms are enabled on a channel, messages the client publishes are confirmed asynchronously by the broker, meaning they have been taken care of on the server side.

In this tutorial, we're going to use publisher confirms to make sure published messages have safely reached the broker. We will cover several strategies to using publisher confirms and explain their pros and cons.

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/971f765f-ba6b-45dc-9778-af2def1771da)

## Enabling Publisher Confirms on a Channel

Publisher confirms are a RabbitMQ extension to the AMQP 0.9.1 protocol, so they are not enabled by default. Publisher confirms are enabled at the channel level with `ConfirmSelect` method:

```cs
IModel channel = connection.CreateModel();
channel.ConfirmSelect();
```

This method must be called on every channel that you expect to use publisher confirms. Confirms should be enabled just once, not for every message published.

## Strategy #1: Publishing Messages Individually

Publishing a message and waiting synchronously for its confirmation:

```cs
while (ThereAreMessagesToPublish())
{
    byte[] body = ...;
    IBasicProperties properties = ...;
    channel.BasicPublish(exchange, queue, properties, body);
    channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
}
```

Here, we publish a message as usual and wait for its confirmation. The method returns as soon as the message has been confirmed. If thet message is not confirmed within the timeout or if it is nack-ed (meaning the broker could not take care of it for some reason), the method will throw an exception. The handling of the exception usually consists of logging an error message and/or retrying to send the message.

Different client libraries have different ways to synchronously deal with publisher confirms, so make sure to read carefully the documentation of the client you are using.

This technique is very straightforward but also has a major drawback: it **significantly slows down publishing**, as the confirmation of a message blocks the publishing of all subsequent messages. This approach is not going to deliver throughput of more than a few hundreds of published messages per second. Nevertheless, this can be good enough for some applications.

## Strategy #2: Publishing Messages in Batches

We can publish a batch of messages and wait for this whole batch to be confirmed. The following example uses a batch of 100:

```cs
int batchSize = 100;
int outstandingMessageCount = 0;
TimeSpan timeout = TimeSpan.FromSeconds(5);

while (ThereAreMessagesToPublish())
{
    byte[] body = ...;
    IBasicProperties properties = ...;
    channel.BasicPublish(exchange, queue, properties, body);
    outstandingMessageCount++;

    if (outstandingMessageCount == batchSize)
    {
        channel.WaitForconfirmsOrDie(timeout);
        outstandingMessageCount = 0;
    }
}

if (outstandingMessageCount > 0)
{
    channel.WaitForConfirmsOrDie(timeout);
}
```

Waiting for a batch of messages to be confirmed improves throughput drastically over waiting for a confirm for individual message (up to 20 - 30 times with a remote RabbitMQ node). One drawback is that we do not know exactly what went wrong in case of failure, so we may have to keep a whole batch in memory to log something meaningful or re-publish the messages. And this solution is still synchronous, so it blocks the publishing of messages.

## Strategy #3: Handling Publisher Confirms Asynchronously

The broker confirms published messages asynchronously, one just needs to register a callback on the client to be notified of these confirms:

```cs
IModel channel = connection.CreateModel();
channel.ConfirmSelect();

channel.BasicAcks += (sender, ea) =>
{
    // code when message is confirmed
};

channel.BasicNakcs += (sender, ea) =>
{
    // code when message is nack-ed
};
```

There are 2 callbacks: one for confirmed messages and one for nack-ed messages (messages that can be considered lost by the broker). Both callbacks have a corresponsing `EventArgs` parameter (`ea`) containing a:

* **delivery tag** - The sequence number identifying the confirmed or nack-ed message. We will see shortly how to correlate it with the published message.
* **multiple** - This is a boolean value. If false, only one message is confirmed / nack-ed. If true, all messages wiht a lower or equal sequence number are confirmed / nack-ed.

The sequence number can be obtained with `Channel.NextPublishSeqNo` before publishing:

```cs
ulong sequenceNumber = channel.NextPublishSeqNo;
channel.BasicPublish(exchange, queue, properties, body);
```

A simple way to correlate messages with sequence number consists in using a dictionary. Let's assume we want to publish strings because they are easy to turn into an array of bytes for publishing. Here is a code sample that uses a dictionary to correlate the publishing sequence number with the string body of the message:

```cs
ConcurrentDictionary<ulong, string> outstandingConfirms = new();
var body = "...";
outstandingConfirms.TryAdd(channel.NextPublishSeqNo, body);
channel.BasicPublish(exchange, queue, properties, Encoding.UTF8.GetBytes(body));
```

The publishing code now tracks outbound messages with a dictionary. We need to clean this directory when confirms arrive and do something like logging a warning when messages are nack-ed:

```cs
ConcurrentDictionary<ulong, string> outstandingConfirms = new();

void CleanOutstandingConfirms(ulong sequenceNumber, bool multiple)
{
    IEnumerable<ulong> confirmed = multiple
        ? outstandingConfirms
            .Select(x => x.Key)
            .Where(k => k <= sequenceNumber)
        : [sequenceNumber];

    foreach (ulong key in confirmed)
        outstandingConfirms.TryRemove(key, out _);
}

channel.BasicAcks += (sender, ea) =>
    CleanOutstandingConfirms(ea.Deliverytag, ea.Multiple);

channelBasicNacks += (sender, ea) =>
{
    outstandingConfirms.TryGetValue(ea.DeliveryTag, out string body);
    Console.WriteLine($"Message with body {body} has been nack-ed. Sequence number: {ea.DeliveryTag}, multiple: {ea.Multiple}");
    CleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple);
};
```

The previous sample contains a callback that cleans the dictionary when confirms arrive. Note this callback handles both single and multiple confirms. This callback is used when confirms arrive (`Channel.BasicAcks`). The callback for nack-ed messages retrieves the message body and issues a warning. It then re-uses the previous callback to clean the dictionary of outstanding confirms (whether messages are confirmed or nack-ed, their corresponding entries in the dictionary must be removed).

To sum up, handling a publisher confirms asynchronously usually requires the following steps:

* Provide a way to correlate the publishing sequence number with a message.
* Register confirm listeners on the channel to be notified when publisher acks/nacks arrive to perform the appropriate actions, like logging or re-publishing a nack-ed message. The sequence-number-to-message correlation mechanism may also require some cleanup during this step.
* Track the publishing sequence number before publishing a message.

> It can be tempting to re-publish a nack-ed message form the corresponding callback, but this should be avoided as confirm callbacks are dispatched on an I/O thread where channels are not supposed to do operations. A better solution cnosists in enqueuing the message in an in-memory queue which is polled by a publishing thread. A class like `ConcurrentQueue` would be a good candidate to transmit messages between the confirm callbacks and a publishing thread.

## Summary

Making sure published messages made it to the broker can be essential in some applications. Publisher confirms are a RabbitMQ feature that helps to meet this requirement. Publisher confirms are asynchronous in nature, but it is also possible to handle them synchronously. There is no definitive way to implement publisher confirms, this usually comes down to the constraints in the application and in the overall system. Typical techniques are:

* publishing mesasges individually, waiting for the confirmation synchronously: simple, but very limited throughput.
* publishing messages in batch, waiting for the confirmation synchronously for a batch: simple, reasonable throughput, but hard to reason about when something goes wrong.
* asynchronous handling: best performance and use of resources, good control in case of error, but can be involved to implement correctly.