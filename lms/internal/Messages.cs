using System;
using MessagePack;
using MessagePack.Formatters;

namespace lms.Internal {
    internal sealed class Request : ILmsMessage {
        public int MsgId { get; set; }
        public string Method { get; set; }
        public byte[] Params { get; set; }
    }
    internal sealed class Response : ILmsMessage {
        public int MsgId { get; set; }
        public byte[] Result { get; set; }
        public byte[] Error { get; set; }
    }
    internal interface ILmsMessage { }
    internal sealed class MessageFormatter : IMessagePackFormatter<ILmsMessage>, IMessagePackFormatter<Request>, IMessagePackFormatter<Response>
    {
        public ILmsMessage Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            switch(reader.ReadInt32()) {
                case 0:
                    return DeserializeResponse(ref reader, options);
                case 1:
                    return DeserializeRequest(ref reader, options);
                default:
                    throw new FormatException("Unknown Message");
            }
        }

        public void Serialize(ref MessagePackWriter writer, ILmsMessage value, MessagePackSerializerOptions options)
        {
            switch(value) {
                case Request r:
                    Serialize(ref writer, r, options);
                break;
                case Response r:
                    Serialize(ref writer, r, options);
                break;
            }
        }

        public void Serialize(ref MessagePackWriter writer, Request value, MessagePackSerializerOptions options)
        {
            writer.Write(0);
            writer.Write(value.MsgId);
            writer.Write(value.Method);
            writer.Write(value.Params);
        }
        public void Serialize(ref MessagePackWriter writer, Response value, MessagePackSerializerOptions options)
        {
            writer.Write(1);
            writer.Write(value.MsgId);
            writer.Write(value.Result);
            writer.Write(value.Error);
        }

        Request IMessagePackFormatter<Request>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if(reader.ReadInt32() != 0) throw new Exception();
            return DeserializeRequest(ref reader, options);
        }

        Response IMessagePackFormatter<Response>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if(reader.ReadInt32() != 1) throw new Exception();
            return DeserializeResponse(ref reader, options);
        }
        Response DeserializeResponse(ref MessagePackReader reader, MessagePackSerializerOptions options) =>
            new Response {
                MsgId = reader.ReadInt32(),
                Result = reader.ReadBytes().GetValueOrDefault().First.ToArray(),
                Error = reader.ReadBytes().GetValueOrDefault().First.ToArray()
            };
        Request DeserializeRequest(ref MessagePackReader reader, MessagePackSerializerOptions options) =>
            new Request {
                MsgId = reader.ReadInt32(),
                Method = reader.ReadString(),
                Params = reader.ReadBytes().GetValueOrDefault().First.ToArray()
            };
    }
}