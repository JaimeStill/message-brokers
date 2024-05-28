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

### Step 6: Import / Export Your Data as Streams of Events With Kafka Connect

[Kafka Connect](https://kafka.apache.org/documentation/#connect) allows you to continuously ingest data from external systems into Kafka, and vice versa. It is an extensible tool that runs *connectors*, which implement the custom logic for interacting with an external system. It is thus very easy to integrate existing systems with Kafka. To make this process even easier, there are hundreds of such connectors readily available.

In this quickstart, we'll see how to run Kafka Connect with simple connectors that import data from a file to a Kafka topic and export data from a Kafka topic to a file.

Make sure to add the `connect-file-3.7.0.jar` to the `plugin.path` property in the Connect worker's configuration:

```bash
# open wsl and navigate to kafka
wsl
cd /mnt/c/kafka/kafka_2.13-3.7.0

# open connect-standalone.properties
nano config/connect-standalone.properties
```

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/99328a47-d7c3-43ed-83f5-2fe9c4bec1d1)

Create seed data to test with:

```bash
echo -e "foo\nbar" > test.txt
```

We'll start with two connectors running in *standalone* mode, which means they run in a single, local, dedicated process. We provide three configuration files as parameters:

* The first is always the configuration for the Kafka Connect process, containing common configuration such as the Kafka brokers to connecto to and the serialization format for data.
* The remaining configuration files each specify a connector to create. These files include a unique connector name, the connector class to instantiate, and any other configuration required by the connector.

```bash
bin/connect-standalone.sh config/connect-standalone.properties config/connect-file-source.properties config/connect-file-sink.properties
```

These sample configuration files, included with Kafka, use the default local cluster configuration you started earlier and create two connectors: the first is a source connector that reads lines from an input file and produces each to a Kafka topic and the second is a sink connector that reads messages from a Kafka topic and produces each as a line in an output file.

During startup, you'll see a number of log messages, including some indicating that the connectors are being instantiated. Once the Kafka Connect process has started, the source connector should start reading lines from `test.txt` and producing them to the topic `connect-test`, and the sink connector should start reading messages from the topic `connect-test` and write them to the file `test.sink.txt`. We can verify the data has been delivered through the entire pipeline by examining the contents of the output file:

```bash
more test.sink.txt
```

Note that the data is being stored in the Kafka topic `connect-test`, so we can also run a console consumer to see the data in the topic (or use custom consumer code to process it):

```bash
bin/kafka-console-consumer.sh --bootstrap-server localhost:9092 --topic connect-test --from-beginning
```

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/6be8017d-3bd8-47e7-92de-32e25a2d1ea0)

THe connectors continue to process data, so we can add data to the file and see it move through the pipeline:

```bash
echo baz>> test.txt
```

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/e232b2cf-495a-44ef-8f72-d60fcebb3c27)

### Step 7: Process Your Events With Kafka Streams

Once your data is stored in Kafka as events, you can process the data with the [Kafka Streams]() client library for Java / Scala. It allows you to implement mission-critical real-time applications and microservices, where the input and/or output data is stored in Kafka topics. Kafka Streams combines the simplicity of writing an deploying standard Java and Scala applications on the client side with the benefits of Kafka's server-side cluster technology to make these applications highly scalable, elastic, fault-tolerant, and distributed. The library supports exactly-one processing, stateful operations and aggregations, windowing, joins, processing based on event-time, and much more.

To give a first taste, here's how one would implement the popular `WordCount` algorithm:

```java
KStream<String, String> textLines = builder.stream("quickstart-events");

KTable<String, Long> wordCounts = textLines
    .flatMapValues(line -> Arrays.asList(line.toLowerCase().split(" ")))
    .groupBy((keyIgnored, word) -> word)
    .count();

wordCounts.toStream().to("output-topic", Produced.with(Serdes.String(), Serdes.Long()));
```

The [Kafka Streams demo](https://kafka.apache.org/documentation/streams/quickstart) and the [app development tutorial](https://kafka.apache.org/37/documentation/streams/tutorial) demonstrate how to code and run such a streaming application from start to finish.

### Step 8: Terminate the Kafka Environment

To tear down the Kafka environment:

1. Stop the producer and consumer clients with <kbd>Ctrl-C</kbd>.
2. Stop the Kafka broker with <kbd>Ctrl-C</kbd>.
3. Stop the ZooKeeper server with <kbd>Ctrl-C</kbd>.

If you also want to delete any data of your local Kafka environment, including any events you have created along the way, run the command:

```bash
rm -rf /tmp/kafka-logs /tmp/zookeeper
```