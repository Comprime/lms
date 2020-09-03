using System;
using System.Threading;
using System.Threading.Tasks;
using nng;

namespace lms {
    public interface IServerBuilder {
        IServerBuilder AddFunction<TReq, TRes>(string name, Func<TReq, CancellationToken, ValueTask<TRes>> action);
        IServerBuilder AddFunction<TRes>(string name, Func<byte[], CancellationToken, ValueTask<TRes>> action);
        IServerBuilder AddFunction<TReq>(string name, Func<TReq, CancellationToken, ValueTask<byte[]>> action);
        IServerBuilder AddFunction(string name, Func<byte[], CancellationToken, ValueTask<byte[]>> action);
        IServer Build(IAPIFactory<IMessage> factory, string uri);
    }
}