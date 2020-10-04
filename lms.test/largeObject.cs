using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace lms.test
{
    public class LargeObjectTests
    {
        [Theory]
        [InlineData(@"tcp://localhost:1337")]
        [InlineData(@"inproc://foo-bar")]
        [InlineData(@"ipc://foo-bar")]
        public async Task TestLargeObject(string uri)
        {
            var factory = Constants.NngFactory;
            using var server = Lms.CreateServerBuilder()
                .AddFunction<int>("Foo", LargeObject)
                .Build(factory, uri);
            using var client = Lms.CreateClient(factory, uri);
            using var cts = new CancellationTokenSource();
            Task listenTask = null;
            try {
                listenTask = server.Listen(1, cts.Token);
                var r = new Random();
                var s = 1024*1024*2 + r.Next(12, 345);
                var b = await client.Request("Foo", s);
                Assert.Equal(s, b.Length);
            }finally{
                cts.Cancel();
                await listenTask;
            }
        }

        public ValueTask<byte[]> LargeObject(int size, CancellationToken cancellationToken){
            return new ValueTask<byte[]>(new byte[size]);
        }
    }
}
