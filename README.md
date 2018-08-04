**Shark**

Shark is a simple http server library. The idea is that you want to respond to some HTTP requests and don't want to spend a lot of time learning or configuring.

The basic idea is that you have a class which contains a set of methods that are annotated with a `PathAttribute` which specifies the path and any arguments to be parsed from the path. The method will take parameters matching the arguments specified in the path and return a `Response`.

Then you can launch a server with `Server<TestServer> server = new Server<TestServer>();` and run it with `server.Run();` where TestServer is your class with the methods that have a `PathAttribute`.

Hopefully at some point I'll make better documentation, but right now the best way is to look at TestServer.cs in SharkConsole to get an idea of how to use it.
