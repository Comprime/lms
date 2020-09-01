using System;
using System.Threading;
using System.Threading.Tasks;

namespace lms {
    public interface IServer : IDisposable, IAsyncDisposable {
        Task Listen(int workers, CancellationToken cancellationToken = default);
    }
}