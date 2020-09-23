using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using MessagePack;
using MessagePack.Resolvers;
using nng;
using nng.Native;

namespace lms.Internal {
    internal sealed class Server : IServer
    {
        private readonly ImmutableSortedDictionary<string, Func<byte[], CancellationToken, ValueTask<byte[]>>> dict;
        private readonly IAPIFactory<IMessage> factory;
        private readonly CancellationTokenSource disposeTokenSource;
        
        private static readonly MessagePackSerializerOptions intMsgOptions = MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create(
                new MessageFormatter()
            ));
        private static readonly MessagePackSerializerOptions excMsgOptions = MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create(
                new ExceptionFormatter()
            ));
        private readonly string uri;
        private readonly ILogger<IServer> logger;

        internal Server(ImmutableSortedDictionary<string, Func<byte[], CancellationToken, ValueTask<byte[]>>> dict, IAPIFactory<IMessage> factory, string uri, ILogger<IServer> logger = null) {
            this.dict = dict;
            this.factory = factory;
            this.uri = uri;
            this.logger = logger;
            disposeTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            disposeTokenSource.Cancel();
            disposeTokenSource.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
        
        private async Task RecieveLoop(IRepSocket socket, CancellationToken cancellationToken = default) {
            using var ctx = socket.CreateAsyncContext(factory).Unwrap();
            cancellationToken.Register(ctx.Cancel);
            while(!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Request req;
                    
                    var reqRes = (await ctx.Receive());
                    if(reqRes.IsErr())
                    {
                        var err = reqRes.Err();
                        if(err == Defines.NngErrno.ECANCELED) {
                            break;
                        }
                        throw new NngException(err);
                    }
                    using(var msg = reqRes.Unwrap()){
                        using var stream = new NngMessageStream(msg);
                        req = MessagePackSerializer.Deserialize<Request>(stream, intMsgOptions);
                    }
                    logger?.LogInformation("<- {Method}", req.Method, req.MsgId);
                    var res = new Response{
                        MsgId = req.MsgId
                    };
                    try {
                        res.Result = await dict[req.Method](req.Params, cancellationToken);
                    } catch(Exception e) {
                        res.Error = MessagePackSerializer.Serialize(e, excMsgOptions);
                    }
                    using(var msg = factory.CreateMessage()) {
                        using var stream = new NngMessageStream(msg);
                        MessagePackSerializer.Serialize(stream, res, intMsgOptions);
                        logger?.LogDebug(">- {Method}", req.Method, req.MsgId);
                        (await ctx.Reply(msg)).Unwrap();
                    }
                }catch(Exception e) {
                    logger?.LogDebug(e, "");
                }
            }
        }

        public async Task Listen(int workers = 8, CancellationToken cancellationToken = default)
        {
            logger?.LogDebug("Listen called");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(disposeTokenSource.Token, cancellationToken);
            using var socket = factory.ReplierOpen().ThenListenAs(out var listener, uri).Unwrap();
            using(listener) {
                Task[] tWorkers = new Task[workers];
                for(int i = 0; i < workers; ++i){
                    tWorkers[i] = Task.Run(async () => await RecieveLoop(socket, cts.Token));
                }
                logger?.LogDebug("Listen loops opened");
                await Task.WhenAll(tWorkers);
                logger?.LogDebug("All listen loops closed");
            }
        }
    }
}