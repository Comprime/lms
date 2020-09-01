# lms
A super simple RPC library that uses [nng](https://github.com/nanomsg/nng) to deliver messages and [Messagepack](https://github.com/neuecc/MessagePack-CSharp)-[RPC](https://github.com/msgpack-rpc/msgpack-rpc) and to format them.

This project uses https://github.com/jeikabu/nng.NETCore to help with using nng in C#
MessagePack serialization and deserialization is handled by https://github.com/neuecc/MessagePack-CSharp
It has support for pluggable logging with Microsoft.Extensions.Logging.Abstractions

## Examples
Create nng.NETCore context and factory objects
```C#
var ctx = new NngLoadContext(path);
var factory = NngLoadContext.Init(ctx);
```

Server is created with
```C#
using var server = Lms.CreateServerBuilder()
    .AddFunction<int,int>("TimesTwenty", i => i * 20))
    .Build(factory, uri);
await server.Listen();
```

Requests are sent with
```C#
using var client = Lms.CreateClient(factory, "ipc://foobar");
var result = await client.Request<int, int>("TimesTwenty", 42);
```