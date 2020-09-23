using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MessagePack;
using MessagePack.Resolvers;
using nng;
using nng.Native;

namespace lms.Internal {
    internal sealed class Client : IClient
    {
        private readonly IAPIFactory<IMessage> factory;
        private readonly string uri;
        private readonly ILogger<IClient> logger;
        private readonly CancellationTokenSource disposeTokenSource;
        private static readonly MessagePackSerializerOptions msgOptions = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
        private static readonly MessagePackSerializerOptions intMsgOptions = MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create( new MessageFormatter() ));
        private static readonly MessagePackSerializerOptions excMsgOptions = MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create( new ExceptionFormatter() ));
        private IReqSocket socket;
        private int counter = 0;
        private SemaphoreSlim socketSemaphore = new SemaphoreSlim(1);
        internal Client(IAPIFactory<IMessage> factory, string uri, ILogger<IClient> logger = null) {
            this.factory = factory;
            this.uri = uri;
            this.logger = logger;
            disposeTokenSource = new CancellationTokenSource();
        }

        private async ValueTask OpenSocket(CancellationToken cancellationToken = default) {
            if(socket is object) return;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await socketSemaphore.WaitAsync(-1, cts.Token);
            try {
                while(socket is null && !cts.Token.IsCancellationRequested) {
                    try {
                        socket = factory.RequesterOpen().ThenDial(uri).Unwrap();
                    }catch (NngException exception) {
                        if(exception.ErrorCode == nng.Native.Defines.NNG_ECONNREFUSED)
                            await Task.Delay(100, cts.Token);
                        else
                            throw;
                    }
                }
            }
            finally
            {
                socketSemaphore.Release();
            }
        }

        public void Dispose()
        {
            disposeTokenSource.Cancel();
            disposeTokenSource.Dispose();
            socket?.Dispose();
            socketSemaphore.Dispose();
        }
        
        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
        public async Task<TRes> Request<TReq, TRes>(string name, TReq request, CancellationToken cancellationToken = default) =>
            MessagePackSerializer.Deserialize<TRes>(await Request(name, MessagePackSerializer.Serialize(request, msgOptions, cancellationToken)), msgOptions);
        public async Task<TRes> Request<TRes>(string name, byte[] request, CancellationToken cancellationToken = default) =>
            MessagePackSerializer.Deserialize<TRes>(await Request(name, request, cancellationToken), msgOptions);
        public async Task<byte[]> Request<TReq>(string name, TReq request, CancellationToken cancellationToken = default) =>
            await Request(name, MessagePackSerializer.Serialize(request, msgOptions), cancellationToken);
        public async Task<byte[]> Request(string name, byte[] request, CancellationToken cancellationToken = default)
        {
            while(!cancellationToken.IsCancellationRequested) {
                await OpenSocket(cancellationToken);
                
                int id = Interlocked.Increment(ref counter);
                var req = factory.CreateMessage();
                using (var stream = new NngMessageStream(req)){
                    MessagePackSerializer.Serialize(stream, new Request {
                        MsgId = id,
                        Method = name,
                        Params = request
                    }, intMsgOptions);
                }

                var s = socket;
                using var ctx = socket.CreateAsyncContext(factory).Unwrap();
                ctx.SetTimeout(60000);

                IMessage res;
                try {
                    logger.LogInformation("-> {Method}", name);
                    using(req)
                        res = (await ctx.Send(req)).Unwrap();
                    logger.LogInformation("-< {Method}", name);
                }catch(NngException exception){
                    if(exception.ErrorCode == Defines.NNG_ETIMEDOUT)
                        throw;
                    logger.LogWarning(exception, "Failed to send {Method}", name);
                    continue;
                }
                using(res) {
                    using var stream = new NngMessageStream(res);
                    var resD = MessagePackSerializer.Deserialize<Response>(stream, intMsgOptions);
                    if(resD.MsgId != id) throw new Exception("ID Missmatch");
                    if(resD.Error is object && resD.Error.Length > 0)
                        throw MessagePackSerializer.Deserialize<Exception>(resD.Error, excMsgOptions);
                    return resD.Result;
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            throw new Exception();
        }
    }
}