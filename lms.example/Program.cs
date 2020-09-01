using System;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using nng;

namespace lms.proj
{
    class Program
    {
        static ILoggerFactory loggerFactory = LoggerFactory.Create(fact => {
                fact.AddConsole();
                fact.AddFilter("lms", LogLevel.Information);
                fact.AddFilter("lms.proj", LogLevel.Information);
                fact.SetMinimumLevel(LogLevel.Information);
            });
        static ILogger logger = loggerFactory.CreateLogger<Program>();
        static async Task Main(string[] args)
        {
            var uri = "ipc://tester";
            var path = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var ctx = new NngLoadContext(path);
            var factory = NngLoadContext.Init(ctx);
            
            using var cts = new CancellationTokenSource(8000);
            await Task.WhenAll(

                // Set up server interface
                Task.Run(async()=>{
                    await using var server = 
                        Lms.CreateServerBuilder(loggerFactory.CreateLogger<IServer>())
                            .AddFunction<int,int>("Tester", Tester)
                            .Build(factory, uri);
                    await server.Listen(8, cts.Token);
                }),

                // Client request loop
                Task.Run(async()=>{
                    await using var client = Lms.CreateClient(factory, uri, loggerFactory.CreateLogger<IClient>());
                    var random = new Random();
                    while(!cts.Token.IsCancellationRequested){
                        var request = random.Next();
                        var result = await client.Request<int, int>("Tester", request, cts.Token);
                        logger.LogInformation("Requested {Request} and got {Result}", request, result);
                    }
                })
            );
        }

        static async ValueTask<int> Tester(int test, CancellationToken token = default) {
            await Task.Delay(200);
            return test;
        }
    }
}
