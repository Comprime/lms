using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using lms;

namespace lms.test
{
    [Collection("Test")]
    public class LargeObjectTests
    {
        [Theory]
        //[InlineData(@"tcp://localhost:1337")]
        //[InlineData(@"inproc://foo-bar")]
        [InlineData(@"ipc://foo-bar")]
        public async Task TestLargeObject(string uri)
        {
            var factory = Constants.NngFactory;
            using var server = Lms.CreateServerBuilder(Constants.LogFactory.CreateLogger<IServer>())
                .AddFunction("Foo", LargeObject)
                .Build(factory, uri);
            using var client = Lms.CreateClient(factory, uri, Constants.LogFactory.CreateLogger<IClient>());
            using var cts = new CancellationTokenSource();
            Task listenTask = null;
            try {
                listenTask = server.Listen(1, cts.Token);
                var b = await client.Request("Foo", null);
                Assert.Equal(1024, b.Length);
            }finally{
                cts.Cancel();
                await listenTask;
            }
        }

        public async ValueTask<byte[]> LargeObject(byte[] _, CancellationToken cancellationToken){
            return new byte[1024];
        }
    }
}
