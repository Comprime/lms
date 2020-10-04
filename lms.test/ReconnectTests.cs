using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace lms.test
{
    [Collection("Test")]
    public class ReconnectTests
    {
        [Theory(Timeout = 4000)]
        [InlineData(@"tcp://localhost:1337")]
        [InlineData(@"inproc://foo-bar")]
        [InlineData(@"ipc://foo-bar")]
        public async Task TestReconnectServerGone(string uri)
        {
            var factory = Constants.NngFactory;
            using var client = Lms.CreateClient(factory, uri);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await Task.WhenAll(
                Task.Run(async ()=>
                {
                    while(!cts.IsCancellationRequested)
                    {
                        using var ctsServer = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                        ctsServer.CancelAfter(TimeSpan.FromMilliseconds(200));
                        using var server = Lms.CreateServerBuilder()
                            .AddFunction("Foo", (a,b)=>default(ValueTask<byte[]>))
                            .Build(factory, uri);
                        try {
                            await server.Listen(1, ctsServer.Token);
                        }catch(OperationCanceledException){}
                    }
                }, cts.Token),
                Task.Run(async ()=>{
                    while(!cts.IsCancellationRequested)
                    {
                        try {
                            await client.Request("Foo", null, cts.Token);
                        }catch(OperationCanceledException){}
                    }
                }, cts.Token)
            );
        }
    }
}
