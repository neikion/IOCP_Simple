# Simple IOCP Server

This is a simple TCP/IP IOCP echo server.

Implemented via SocketAsyncEventArgsPool.
This project is divided into a Server class and a Client class for simple testing of the server

## How to use
Just run the Are Server class.
```C#
Server(int port,int ConnectionSize);
```
Since it is disposable, you must either execute it using using or call dispose afterward.

```C#
using Server server = new Server(34543,30);
server.Run();
```

## Environment
OS : Window 10

dotnet : net8.0



## Warning

Currently, it is a non-blocking server, so if a blocking occurs in the CallbackMethod, no other connections can be established until the process is complete.