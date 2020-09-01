using System;
using Microsoft.Extensions.Logging;
using lms.Internal;
using nng;

namespace lms {
    public static class Lms {
        public static IServerBuilder CreateServerBuilder(ILogger<IServer> logger = null) =>
            new ServerBuilder(logger);
        public static IClient CreateClient(IAPIFactory<IMessage> factory, string uri, ILogger<IClient> logger = null) =>
            new Client(factory, uri, logger);
    }
}