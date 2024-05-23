# [Topics](https://www.rabbitmq.com/tutorials/tutorial-five-dotnet)

In our logging system we might want to subscribe to not only logs based on severity, but also based on the source which emitted the log. You might know this concept from the [`syslog`]() unix tool, which routes logs based on both severity (info/warn/crit...) and facility (auth/cron/kern...).

That would give us a lot of flexibility - we may want to listen to just critical errors coming from 'cron' but also all logs from 'kern'.

To implement that in our logging system we need to learn about the more complex `topic` exchange.

![image](https://github.com/JaimeStill/JaimeStill/assets/14102723/c1a91702-1618-45f4-8c90-2a01017e5a82)

## Topic Exchange

Messages sent to a `topic` exchange can't have an arbitrary `routing key` - it must be a list of words, delimited by dots. The words can be anything, but usually they specify some features connected to the message. A few valid routing examples: "`stock.usd.nyse`", "`nyse.vmw`", "`quick.orange.rabbit`". There can be as many words in the routing key as you like, up to the limit of 255 bytes.

The binding key must also be in the same form. The logic behind the `topic` exchange is similar to a `direct` one - a message sent with a particular routing key will be delievered to all the queues that are bound with a matching binding key. However there are two important special cases for binding keys:

* `*` (start) can substitute for exactly one word.
* `#` (hash) can substitue for zero or more words.

In this example, we're going to send messages which describe animals. The messages will be sent with a routing key that consists of three words (two dots). The first word in the routing key will describe speed, the second a color, and third a species: "`<speed>.<color>.<species>`".

There are three binding keys:

**Q1**  
* "`*.orange.*`"

**Q2**  
* "`*.*.rabbit`"
* "`lazy.#`"

These bindings can be summarized as:

* **Q1** is interested in all the orange animals.
* **Q2** wants to hear everything about rabbits, and everything about lazy animals.

A message with a routing key set to "`quick.orange.rabbit`" will be delievered to both queues. Message "`lazy.orange.elephant`" will also go to both of them. On the other hand, "`quick.orange.fox`" will only go to the first queue, and "`lazy.brown.fox`" only to the second. "`lazy.pink.rabbit`" will be delievered to the second queue only once, even through it matches two bindings. "`quick.brown.fox`" doesn't match any binding so it will be discarded.

What happens if we break our contract and send a message with one or four words, like "`orange`" or "`quick.orange.new.rabbit`"? These won't match any bindings and will be lost.

On the other hand "`lazy.orange.new.rabbit`", even though it has four words, will match the last binding and will be delivered to the second queue.