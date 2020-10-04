using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace lms.test
{
    [Collection("Test")]
    public class ConnectionTests
    {
        [Theory]
        [InlineData(@"tcp://localhost:1337")]
        [InlineData(@"inproc://foo-bar")]
        [InlineData(@"ipc://foo-bar")]
        public async Task TestProtocols(string uri)
        {
            var factory = Constants.NngFactory;
            using var server = Lms.CreateServerBuilder()
                .AddFunction("Foo", (a,b)=>default(ValueTask<byte[]>))
                .Build(factory, uri);
            using var client = Lms.CreateClient(factory, uri);
            using var cts = new CancellationTokenSource();
            Task listenTask = null;
            try {
                listenTask = server.Listen(1, cts.Token);
                await client.Request("Foo", null);
            }finally{
                cts.Cancel();
                await listenTask;
            }
        }
    }
}
