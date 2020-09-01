using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using nng;
using MessagePack;
using MessagePack.Resolvers;

namespace lms.Internal {
    internal sealed class ServerBuilder : IServerBuilder {
        private readonly ILogger<IServer> logger;
        private static readonly MessagePackSerializerOptions msgOptions = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
        internal ServerBuilder(ILogger<IServer> logger){
            this.logger = logger;
        }
        List<(string, Func<byte[], CancellationToken, ValueTask<byte[]>>)> Adds = new List<(string, Func<byte[], CancellationToken, ValueTask<byte[]>>)>();
        public IServerBuilder AddFunction<TReq, TRes>(string name, Func<TReq, CancellationToken, ValueTask<TRes>> action) {
            Adds.Add((name, (req,ct) => Serializer(req, action, ct)));
            return this;
        }

        private static async ValueTask<byte[]> Serializer<TReq, TRes>(byte[] req, Func<TReq, CancellationToken, ValueTask<TRes>> handler, CancellationToken cancellationToken = default) =>
            MessagePackSerializer.Serialize(await handler(MessagePackSerializer.Deserialize<TReq>(req, msgOptions), cancellationToken), msgOptions);

        public IServer Build(IAPIFactory<IMessage> factory, string uri)
        {
            var dictBuilder = ImmutableSortedDictionary.CreateBuilder<string, Func<byte[], CancellationToken, ValueTask<byte[]>>>();
            foreach(var (key, value) in Adds)
                dictBuilder.Add(key, value);
            return new Server(dictBuilder.ToImmutableSortedDictionary(), factory, uri, logger);
        }
    }
}