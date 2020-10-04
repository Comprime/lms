using System;
using System.Buffers;

namespace lms.Internal {
	internal static class ByteAsSequemce {
		internal static ReadOnlySequence<byte>? AsSequence(this byte[] buffer) =>
			buffer is object && buffer.Length > 0 ? new ReadOnlySequence<byte>(buffer) : default;
		internal static byte[] ToArray(this ReadOnlySequence<byte>? sequence) =>
			sequence is ReadOnlySequence<byte> seq && seq.Length > 0 ? seq.ToArray() : null;
	}
}