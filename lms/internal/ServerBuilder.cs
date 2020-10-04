using System;
using System.Buffers;
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
        private static readonly MessagePackSerializerOptions msgOptions = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
        List<(string, Func<ReadOnlySequence<byte>?, CancellationToken, ValueTask<ReadOnlySequence<byte>?>>)> Adds = new List<(string, Func<ReadOnlySequence<byte>?, CancellationToken, ValueTask<ReadOnlySequence<byte>?>>)>();
        public IServerBuilder AddFunction<TReq, TRes>(string name, Func<TReq, CancellationToken, ValueTask<TRes>> action) =>
            AddFunction(name, (req,ct) => Serializer(req, action, ct));

        public IServerBuilder AddFunction<TReq>(string name, Func<TReq, CancellationToken, ValueTask<byte[]>> action) =>
            AddFunction(name, (req,ct) => Serializer(req, action, ct));

        public IServerBuilder AddFunction<TRes>(string name, Func<byte[], CancellationToken, ValueTask<TRes>> action) =>
            AddFunction(name, (req,ct) => Serializer(req, action, ct));

        public IServerBuilder AddFunction(string name, Func<byte[], CancellationToken, ValueTask<byte[]>> action) => 
            AddFunction(name, async (req, ct) => (await action(req.ToArray(), ct)).AsSequence());

        public IServerBuilder AddFunction(string name, Func<ReadOnlySequence<byte>?, CancellationToken, ValueTask<ReadOnlySequence<byte>?>> action) {
            Adds.Add((name, action));
            return this;
        }

        private static async ValueTask<byte[]> Serializer<TReq, TRes>(byte[] req, Func<TReq, CancellationToken, ValueTask<TRes>> handler, CancellationToken cancellationToken = default) =>
            MessagePackSerializer.Serialize(await handler(MessagePackSerializer.Deserialize<TReq>(req, msgOptions), cancellationToken), msgOptions);
        private static async ValueTask<byte[]> Serializer<TRes>(byte[] req, Func<byte[], CancellationToken, ValueTask<TRes>> handler, CancellationToken cancellationToken = default) =>
            MessagePackSerializer.Serialize(await handler(req, cancellationToken), msgOptions);
        private static async ValueTask<byte[]> Serializer<TReq>(byte[] req, Func<TReq, CancellationToken, ValueTask<byte[]>> handler, CancellationToken cancellationToken = default) =>
            await handler(MessagePackSerializer.Deserialize<TReq>(req, msgOptions), cancellationToken);

        public IServer Build(IAPIFactory<IMessage> factory, string uri, Action<IOptions> socketConfigurator = null, ILogger<IServer> logger = null)
        {
            var dictBuilder = ImmutableSortedDictionary.CreateBuilder<string, Func<ReadOnlySequence<byte>?, CancellationToken, ValueTask<ReadOnlySequence<byte>?>>>();
            foreach(var (key, value) in Adds)
                dictBuilder.Add(key, value);
            return new Server(dictBuilder.ToImmutableSortedDictionary(), factory, uri, socketConfigurator, logger);
        }
    }
}