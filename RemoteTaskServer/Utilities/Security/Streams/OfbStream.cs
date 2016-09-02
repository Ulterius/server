using System;
using System.IO;
using System.Security.Cryptography;

namespace UlteriusServer.Utilities.Security.Streams
{
    internal class OfbStream : Stream
    {
        private const int Blocks = 16;
        private const int Eos = 0; // the goddess of dawn is found at the end of the stream
        private readonly CryptoStream _cbcStream;
        private readonly byte[] _keyStreamBuffer;
        private readonly CryptoStreamMode _mode;

        private readonly Stream _parent;
        private readonly byte[] _readWriteBuffer;
        private int _keyStreamBufferOffset;

        public OfbStream(Stream parent, SymmetricAlgorithm algo, CryptoStreamMode mode)
        {
            if (algo.Mode != CipherMode.CBC)
                algo.Mode = CipherMode.CBC;
            if (algo.Padding != PaddingMode.None)
                algo.Padding = PaddingMode.None;
            _parent = parent;
            _cbcStream = new CryptoStream(new ZeroStream(), algo.CreateEncryptor(), CryptoStreamMode.Read);
            _mode = mode;
            _keyStreamBuffer = new byte[algo.BlockSize * Blocks];
            _readWriteBuffer = new byte[_keyStreamBuffer.Length];
        }

        public override bool CanRead => _mode == CryptoStreamMode.Read;

        public override bool CanWrite => _mode == CryptoStreamMode.Write;

        public override bool CanSeek => false;

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
            {
                throw new NotSupportedException();
            }

            var toRead = Math.Min(count, _readWriteBuffer.Length);
            var read = _parent.Read(_readWriteBuffer, 0, toRead);
            if (read == Eos)
                return Eos;

            for (var i = 0; i < read; i++)
            {
                // NOTE could be optimized (branches for each byte)
                if (_keyStreamBufferOffset % _keyStreamBuffer.Length == 0)
                {
                    FillKeyStreamBuffer();
                    _keyStreamBufferOffset = 0;
                }

                buffer[offset + i] = (byte)(_readWriteBuffer[i]
                                             ^ _keyStreamBuffer[_keyStreamBufferOffset++]);
            }

            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
            {
                throw new NotSupportedException();
            }

            var readWriteBufferOffset = 0;
            for (var i = 0; i < count; i++)
            {
                if (_keyStreamBufferOffset % _keyStreamBuffer.Length == 0)
                {
                    FillKeyStreamBuffer();
                    _keyStreamBufferOffset = 0;
                }

                if (readWriteBufferOffset % _readWriteBuffer.Length == 0)
                {
                    _parent.Write(_readWriteBuffer, 0, readWriteBufferOffset);
                    readWriteBufferOffset = 0;
                }

                _readWriteBuffer[readWriteBufferOffset++] = (byte)(buffer[offset + i]
                                                                    ^ _keyStreamBuffer[_keyStreamBufferOffset++]);
            }

            _parent.Write(_readWriteBuffer, 0, readWriteBufferOffset);
        }

        private void FillKeyStreamBuffer()
        {
            var read = _cbcStream.Read(_keyStreamBuffer, 0, _keyStreamBuffer.Length);
            // NOTE undocumented feature
            // only works if keyStreamBuffer.Length % blockSize == 0
            if (read != _keyStreamBuffer.Length)
                throw new InvalidOperationException("Implementation error: could not read all bytes from CBC stream");
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }
}