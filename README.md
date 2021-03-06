# lms
A super simple RPC library that uses [nng](https://github.com/nanomsg/nng) as transport and [MessagePack](https://github.com/neuecc/MessagePack-CSharp)-[RPC](https://github.com/msgpack-rpc/msgpack-rpc) to do serialization.

This project uses [nng.NETCore](https://github.com/jeikabu/nng.NETCore) to help with using nng in C#\
MessagePack serialization and deserialization is handled by [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp)\
LMS has support for pluggable logging support with [Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/)

## Examples
Create nng.NETCore context and factory objects
```C#
var path = Path.GetDirectoryName(typeof(Program).Assembly.Location);
var ctx = new NngLoadContext(path);
var factory = NngLoadContext.Init(ctx);
```

Server is created with
```C#
using var server = Lms.CreateServerBuilder()
    .AddFunction<int,int>("TimesTwenty", i => i * 20))
    .Build(factory, uri);
await server.Listen(8); // For 8 workers
```

Requests are sent with
```C#
using var client = Lms.CreateClient(factory, "ipc://foobar");
var result = await client.Request<int, int>("TimesTwenty", 42);
```
