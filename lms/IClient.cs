using System;
using System.Threading;
using System.Threading.Tasks;

namespace lms {
    public interface IClient : IDisposable, IAsyncDisposable {
        Task<TRes> Request<TReq, TRes>(string name, TReq request, CancellationToken cancellationToken = default);
        Task<byte[]> Request<TReq>(string name, TReq request, CancellationToken cancellationToken = default);
        Task<TRes> Request<TRes>(string name, byte[] request, CancellationToken cancellationToken = default);
        Task<byte[]> Request(string name, byte[] request, CancellationToken cancellationToken = default);
    }
}