using System;
using System.Threading;
using System.Threading.Tasks;
using nng;

namespace lms {
    public interface IServerBuilder {
        IServerBuilder AddFunction<TReq, TRes>(string name, Func<TReq, CancellationToken, ValueTask<TRes>> action);
        IServer Build(IAPIFactory<IMessage> factory, string uri);
    }
}