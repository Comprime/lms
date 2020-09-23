using System;
using MessagePack;
using MessagePack.Formatters;

namespace lms.Internal {
	internal sealed class ExceptionFormatter : IMessagePackFormatter<Exception>
	{
		public void Serialize(ref MessagePackWriter writer, Exception value, MessagePackSerializerOptions options)
		{
			writer.Write(value.GetType().FullName);
			writer.Write(value.Message);
			writer.Write(value.Source);
			writer.Write(value.StackTrace);
		}

		public Exception Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
		{
			var type = reader.ReadString();
			var message = reader.ReadString();
			var source = reader.ReadString();
			var stackTrace = reader.ReadString();
			return new RemoteException(type, message, source, stackTrace){};
		}
	}
}