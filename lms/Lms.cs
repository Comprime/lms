using System;
using Microsoft.Extensions.Logging;
using lms.Internal;
using nng;

namespace lms {
    public static class Lms {
        public static IServerBuilder CreateServerBuilder() =>
            new ServerBuilder();
        public static IClient CreateClient(IAPIFactory<IMessage> factory, string uri, Action<IOptions> socketConfigurator = null, ILogger<IClient> logger = null) =>
            new Client(factory, uri, socketConfigurator, logger);
    }
}