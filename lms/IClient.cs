using System;
using System.Threading;
using System.Threading.Tasks;

namespace lms {
    public interface IClient : IDisposable, IAsyncDisposable {
        Task<TRes> Request<TReq, TRes>(string name, TReq request, CancellationToken cancellationToken = default);
    }
}