using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// Accessing a FileStream's length, results in an platform I/O call.  This stream wrapper will track written bytes.
    /// Not suitable for random seeking.  If stream's position is set, then the length will reset to that position
    /// </summary>
    internal class WriteCountingStream : Stream
    {
        private readonly Stream _stream;
        private long _length;

        public WriteCountingStream(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            _stream = stream;

            _length = stream.Length;
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _length;

        public override long Position 
        { 
            get => _stream.Position;
            set
            {
                _stream.Position = value;
                _length = value;
            }
        } 

        public override void Flush() => _stream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
        {   
            long res = _stream.Seek(offset, origin);

            // track the length as the position
            if (origin == SeekOrigin.Begin)
            {
                _length = offset;
            }
            else if (origin == SeekOrigin.End)
            {
                _length = _stream.Length;
            }
            else 
            {
                _length = _stream.Position;
            }

            return res;
        }

        public override void SetLength(long value) 
        {
            _stream.SetLength(value);
            _length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
            _length += count;
        }

        public override void Close() => _stream.Close();
    }

    internal static class StreamExtensions 
    {
        /// <summary>
        /// Wraps the stream with a write counting stream
        /// </summary>
        /// <returns></returns>
        public static Stream ToWriteCountingStream(this Stream stream) => new WriteCountingStream(stream);
    }

}
