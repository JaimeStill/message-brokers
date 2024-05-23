# RabbitMQ Setup Notes

Following the [Using the Installer](https://www.rabbitmq.com/docs/install-windows#installer) instructions:

* Download the supported version of [Erlang](https://www.erlang.org/downloads) and run the installer as administrator
* Download the latest version of [RabbitMQ Server](https://github.com/rabbitmq/rabbitmq-server) from Releases and run the installer as administrator
* Add `C:\Program Files\RabbitMQ Server\rabbitmq_server-3.13.2\sbin` to PATH
* Verify install by running `rabbitmqctl.bat cluster_status`