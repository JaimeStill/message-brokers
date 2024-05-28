# [Getting Started with Apache Kafka and .NET](https://developer.confluent.io/get-started/dotnet/)

The steps that follow assume that the steps from the quickstart, documented in the [Kafka readme](../readme.md), have been followed and you have a local Kafka cluster.

## Configure Kafka Server

In order to connect to the local Kafka cluster running in WSL from .NET in Windows, the `server.properties` file must be configured to allow a connection using an IPv6 address:

```bash
nano config/server.properties
```

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/03596ea0-5e4b-4a58-93a1-e20779374f15)

This configuration will allow WSL-based commands to still use `--bootstrap-server localhost:9096`, but allow .NET to be configured with `BootstrapServers = "[::1]:9092"`.

## Create Project

Initialize two .NET console app projects:

```bash
dotnet new console -o Producer
dotnet new console -o Consumer
```

Add the Kafka library package reference to both projects:

```bash
dotnet add package Confluent.Kafka
```

## Create Topic

```bash
# wsl tab 1 - start zookeeper
bin/zookeeper-server-start.sh config/zookeeper.properties

# wsl tab 2 - start kafka
bin/kafka-server-start.sh config/server.properties

# wsl tab 3 - create topic
bin/kafka-topics.sh --create --topic purchases --bootstrap-server localhost:9092
```

## Execute

In a terminal pointed to the [`Consumer`](./Consumer/) project, run `dotnet run`. This will setup a listener that consumes Kafka events on the `purchases` topic.

In a terminal pointed to the [`Producer`](./Producer/), run `dotnet run`. This will generate a series of Kafka events on the `purchases` topic.

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/ffa27e0e-ee51-4e86-9c9c-d047426b253c)