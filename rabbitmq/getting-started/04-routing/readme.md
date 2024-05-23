# [Routing](https://www.rabbitmq.com/tutorials/tutorial-four-dotnet)

In the [publish / subscribe](../03-publish-subscribe/) tutorial, we built a simple logging system that was able to broadcast log messages to many receivers. In this tutorial, we will add a feature to it - we're going to make it possible to subscribe only to a subset of the messages. For example, we will be able to direct only critical error messages to the log file (to save disk space), while still being able to print all of the log messages to the console.

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/d69709fd-bdf7-488a-b0a1-7f2f90b23ebe)