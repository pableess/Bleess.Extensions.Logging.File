using System;
using System.Buffers;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// An <see cref="IBufferWriter{T}"/> backed by a pooled array that can grow. Used by the JSON
    /// formatter so large entries (long messages, exceptions, scopes) do not need a copy or a new
    /// allocation per message.
    /// </summary>
    internal sealed class PooledBufferWriter : IBufferWriter<byte>, IDisposable
    {
        private byte[] _buffer;
        private int _written;
        private bool _disposed;

        public PooledBufferWriter(int initialCapacity)
        {
            _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        }

        public int WrittenCount => _written;

        public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _written);

        public void Advance(int count)
        {
            if (count < 0 || _written + count > _buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            _written += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsMemory(_written);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsSpan(_written);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = Array.Empty<byte>();
            _written = 0;
        }

        private void EnsureCapacity(int sizeHint)
        {
            if (sizeHint < 1)
            {
                sizeHint = 1;
            }

            if (_buffer.Length - _written >= sizeHint)
            {
                return;
            }

            int required = _written + sizeHint;
            int newSize = Math.Max(_buffer.Length * 2, required);

            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
            _buffer.AsSpan(0, _written).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
        }
    }
}
