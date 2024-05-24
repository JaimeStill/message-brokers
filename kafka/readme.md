# Kafka

Kafka is a distributed system consiting of **servers** and **clients** that communicate via a high-performance [TCP network protocol](https://kafka.apache.org/protocol.html). It can be deployed on bare-metal hardware, virtual machines, and containers in on-premise as well as cloud environments.

**Servers**: Kafka is run as a cluster of one or more servers that can span multiple datacenters or cloud regions. Some of these servers form the storage layer, called brokers. Other servers run [Kafka Connect](https://kafka.apache.org/documentation/#connect) to continously importa nd export data as event streams to integrate Kafka with your existing systems such as relational database as well as other Kafka clusters. To let you implement mission-critical use cases, a Kafka cluster is highly scalable and fault-tolerant: if any of its servers fails, the other servers will take over their work to ensure continuous operations without any data loss.

**Clients**: They allow you to write distributed applications and microservices that read, write, and process streams of events in parallel, at scale, and in a fault-tolerant manner even in the case of network problems or machine failures. Kafka ships with some such clients included, which are augmented by [dozens of clients]() provided by the Kafka community: clients are available for Java and Scala including the higher-level [Kafka Streams](https://kafka.apache.org/documentation/streams/) library, for Go, Python, C/C++, and many other programming languages as well as REST APIs.

## Setup

Follow the [Quickstart](https://kafka.apache.org/quickstart) guide in WSL:

### Step 1: Get Kafka

Open Windows Terminal and run the following:

```bash
# open wsl
wsl

# install java
sudo apt update && sudo apt upgrade
sudo apt install default-jre

# setup directory
mkdir /mnt/c/kafka/
cd /mnt/c/kafka/

# download kafka
curl -o kafka_2.13-3.7.0.tgz https://dlcdn.apache.org/kafka/3.7.0/kafka_2.13-3.7.0.tgz

# extract and change directory
tar -xzf kafka_2.13-3.7.0.tgz
rm -f kafka_2.13-3.7.0.tgz
```

### Step 2: Start the Kafka Environment

Open Windows Terminal and run the following:

```bash
# open wsl and navigate to kafka
wsl
cd /mnt/c/kafka/kafka_2.13-3.7.0

# start the ZooKeeper service
bin/zookeeper-server-start.sh config/zookeeper.properties
```

In a new Windows Terminal tab, run the following:

```bash
# open wsl and navigate to kafka
wsl
cd /mnt/c/kafka/kafka_2.13-3.7.0

# start the Kafka broker service
bin/kafka-server-start.sh config/server.properties
```

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/cc5556aa-fce6-4faa-a520-d6bfaf839a21)

### Step 3: Create a Topic to Store Your Events

Kafka is a distributed *event streaming platform* that lets you read, write, store, and process [*events*](https://kafka.apache.org/documentation/#messages) (also called *records* or *messages* in the documentation) across many machines.

Example events are payment transactions, geolocation updates from mobile phones, shipping orders, sensor measurements from IoT devices or medical equipment, and much more. These events are organized and stored in [*topics*](https://kafka.apache.org/documentation/#intro_concepts_and_terms). Very simplified, a topic is similar to a folder in a filesystem, and the events are the files in that folder.

Before you can write your first events, you must create a topic.

In a new Windows Terminal tab, run the following:

```bash
# open wsl and navigate to kafka
wsl
cd /mnt/c/kafka/kafka_2.13-3.7.0

# create the quickstart-events topic
bin/kafka-topics.sh --create --topic quickstart-events --bootstrap-server localhost:9092

# describe the new quickstart-events topic
bin/kafka-topics.sh --describe --topic quickstart-events --bootstrap-server localhost:9092
```

### Step 4: Write Events Into the Topic

A Kafka client communicates with the Kafka brokers via the network for writing (or reading) events. Once received, the brokers will store the events in a durable and fault-tolerant manner for as long as you need - even forever.

Run the console producer client to write a few events into your topic. By default, each line you enter will result in a separate event being written to the topic.

> Copy the producer client with <kbd>Ctrl-C</kbd> at any time.

```bash
bin/kafka-console-producer.sh --topic quickstart-events --bootstrap-server localhost:9092
>This is my first event
>This is my second event
```

### Step 5: Read the Events

Open another terminal session and run the console consumer client to read the events you just created:

> You can stop the consumer client with <kbd>Ctrl-C</kbd> at any time.

```bash
bin/kafka-console-consumer.sh --topic quickstart-events --from-beginning --bootstrap-server localhost:9092

# outputs
# This is my first event
# This is my second event
```

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/1b5e18d4-8400-47b3-af81-8f3739c3a70b)

> TODO: Follow steps 6 and 7

### Step 8: Terminate the Kafka Environment

To tear down the Kafka environment:

1. Stop the producer and consumer clients with <kbd>Ctrl-C</kbd>.
2. Stop the Kafka broker with <kbd>Ctrl-C</kbd>.
3. Stop the ZooKeeper server with <kbd>Ctrl-C</kbd>.

If you also want to delete any data of your local Kafka environment, including any events you have created along the way, run the command:

```bash
rm -rf /tmp/kafka-logs /tmp/zookeeper
```