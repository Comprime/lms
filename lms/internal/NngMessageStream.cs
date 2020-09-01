using System;
using System.IO;
using nng;

namespace lms.Internal {
    internal class NngMessageStream : Stream
    {
        internal NngMessageStream(IMessagePart part){
            this.part = part;
        }
        private readonly IMessagePart part;
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => part.Length;

        public override long Position { get; set; }

        public override void Flush()
        {}

        public override int Read(Span<byte> buffer) {
            var s = part.AsSpan().Slice((int)Position, Math.Min(buffer.Length, part.Length - (int)Position));
            s.CopyTo(buffer);
            Position += s.Length;
            return s.Length;
        }

        public override void Write(ReadOnlySpan<byte> buffer) {
            part.Append(buffer);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new System.NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }
    }
}