using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using lms;

namespace lms.test
{
    [Collection("Test")]
    public class ExceptionTests
    {
        [Theory]
        [InlineData(@"tcp://localhost:1337")]
        [InlineData(@"inproc://foo-bar")]
        [InlineData(@"ipc://foo-bar")]
        public async Task TestRemoteException(string uri)
        {
            var factory = Constants.NngFactory;
            using var server = Lms.CreateServerBuilder()
                .AddFunction<string>("Throw", ThrowException)
                .Build(factory, uri);
            using var client = Lms.CreateClient(factory, uri);

            using var cts = new CancellationTokenSource();
            Task listenTask = null;
            try {
                listenTask = server.Listen(1, cts.Token);
                var exception = await Assert.ThrowsAsync<RemoteException>(async()=>await client.Request("Throw", "Foo Bar"));
                Assert.Equal("lms.test", exception.Source);
                Assert.Equal("Foo Bar", exception.Message);
                Assert.Contains($"at {typeof(ExceptionTests).FullName}.{nameof(ThrowException)}(String message, CancellationToken cancellationToken)", exception.StackTrace);
            }finally{
                cts.Cancel();
                await listenTask;
            }
        }

        public ValueTask<byte[]> ThrowException(string message, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(message);
        }
    }
}
