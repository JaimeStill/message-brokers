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