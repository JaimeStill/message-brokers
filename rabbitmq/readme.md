# RabbitMQ

RabbitMQ is a message broker: it accepts and forwards messages. You can think about it as a post office: when you put the mail that you want posting in a post box, you can be sure that the letter carrier will eventually deliver the mail to your recipient. In this analogy, RabbitMQ is a post box, a post office, and a letter carrier.

The major difference between RabbitMQ and the post office is that it doesn't deal with paper, instead it accepts, stores, and forwards binary blobs of data - *messages*.

## Setup

Following the [Using the Installer](https://www.rabbitmq.com/docs/install-windows#installer) instructions:

* Download the supported version of [Erlang](https://www.erlang.org/downloads) and run the installer as administrator
* Download the latest version of [RabbitMQ Server](https://github.com/rabbitmq/rabbitmq-server) from Releases and run the installer as administrator
* Add `C:\Program Files\RabbitMQ Server\rabbitmq_server-3.13.2\sbin` to PATH
* Verify install by running `rabbitmqctl.bat cluster_status`

## Enable Management Plugin

```
rabbitmq-plugins.bat enable rabbitmq_management
```

Navigate to http://localhost:15672 and authenticate with:

* Username: guest
* Password: guest

## Concepts

RabbitMQ, and messaging in general, uses some jargon.

* *Producing* means nothing more than sending. A program that sends messages is a *producer*.

* A *queue* is the name for the post box in RabbitMQ. Although messages flow through RabbitMQ and your applications, they can only be stored inside a *queue*. A *queue* is only boudn by the host's memory & disk limits, it's essentially a large message buffer.

    Many *producers* can send messages that go to one queue, and many *consumers* can try to receive data from one *queue*.

* *Consuming* has a similar meaning to receiving. A *consumer* is a program that mostly waits to receive messages.

### [Exchanges](https://www.rabbitmq.com/tutorials/amqp-concepts#exchanges)

The core idea in the messaging model in RabbitMQ is that the producer never sends any messages directly to a queue. Actually, quite often the producer doesn't even know if a message will be delivered to any queue at all.

Instead, the producer can only send messages to an *exchange*. An exchange is a very simple thing. On one side it receives messages from producers and the other side it pushes them to queues. The exchange must know exactly what to do with a message it receives. Should it be appended to a particular queue? Should it be appended to many queues? Or should it get discarded? The rules for that are defined by the *exchange type*.

There are a few exchange types available:

Exchange Type | Description
--------------|------------
`direct` | A direct exchange delivers messages to queues based on the message routing key. A direct exchange is ideal for the unicast routing of messages. They can be used for multicast routing as well.<br/><br/><ul><li>A queue binds to the exchange with a routing key, `K`.</li><li>When a new message with routing key, `R`, arrives at the direct exchange, the exchange routes it to the queue if `K` = `R`.</li><li>If multiple queues are bound to a direct exchange with the same routing key, `K`, the exchange will route the message to all queues for which `K` = `R`.</li></ul>
`fanout` | A fanout excahnge routes messages to all of the queues that are bound to it and the routing key is ignored. If `N` queues are bound to a fanout exchange, when a new message is published to that exchange, a copy of the message is delivered to all `N` queues. Fanout exchanges are ideal for the broadcast routing of messages.<br/><br/>Because a fanout exchange delivers a copy of a message to every queue bound to it, its use cases are quite similar:<br/><br/><ul><li>Massively multi-player online (MMO) games can use it for leaderboard updates or other global events.</li><li>Sport news sites can use fanout exchanges for distributed score updates to mobile clients in near real-time.</li><li>Distributed systems can broadcast various state and configuration updates.</li><li>Group chats can distribute messages between participants using a fanout exchange (althouth AMQP does not have a built-in concept of presence, so XMPP may be a better choice).</li></ul>
`topic` | Topic exchanges route messages to one or many queues based on matching between a message routing key andt he pattern that was used to bind a queue to an exchange. The topic exchange type is often used to implement various publish / subscribe pattern variations. Topic exchanges are commonly used for the multicast ruoting of messages.<br/><br/>Topic exchanges have a very broad set of use cases. Whenever a problem involves multiple consumers / applications that selectively choose which type of messages they want to receive, the use of topic exchanges should be considered.<br/><br/>Example uses:<ul><li>Distributing data relevant to specific geographic location, for example, points of sale.</li><li>Background task processing done by multiple workers, each capable of handling a specific sets of tasks.</li><li>Stocks price updates (and updates on other kinds of financial data).</li><li>News updates that involve categorization or tagging (for example, only for a particular sport or team).</li><li>Orchestration of services of different kinds in the cloud.</li><li>Distributed architecture / OS-specific software builds or packaging where each builder can handle only one architecture or OS.</li></ul>
`headers` | A headers exchange is designed for routing on multiple attributes that are more easily expressed as message headers than a routing key. Headers exchanges ignore the routing key attribute. Instead, the attributes used for routing are taken from the headers attribute. A message is considered matching if the value of the header equals the value specified upon binding.<br/><br/>It is possible to bind a queue to a headers exchange using more than one header for matching. In this case, the broker needs one more piece of information from the application developer, namely, should it consider messages with any of the headers matching, or all of them? This is what the `x-match` binding argument is for. When the `x-match` argument is set to `any`, just one matching header value is sufficient. Alternatively, setting `x-match` to `all` mandates that all the values must match.<br/><br/>For `any` and `all`, headers beginning with the string `x-` will not be used to evaluate matches. Setting `x-match` to `any-with-x` or `all-with-x` will also use headers beginning with the string `x-` to evaluate matches.<br/><br/>Headers exchanges can be looked upon as "direct exchanges on steroids". Because they route based on header values, they can be used as direct exchanges where the routing key does not have to be a string; it could be an integer or a hash (dictionary) for example.

### Message Properties

The AMQP 0-9-1 protocol predefines a set of 14 properties that go with a message. Most of the properties are rarely used, with the exception of the following:

* `Persistent` - Marks a message as persistent (with a value of `true`) or transient (any other value).
* `DeliveryMode` - Those familiar with the protocol may choose to use this property instead of `Persistent`. They control the same thing.
* `ContentType` - Used to describe the mime-type of the encoding. For example for the often used JSON encoding it is a good practice to set this property to: `application/json`.
* `ReplyTo` - Commonly used to name a callback queue.
* `CorrelationId` - Useful to correlate RPC responses with requests.