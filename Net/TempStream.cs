using System;
using System.IO;

namespace UMC.Net
{
    class TempStream : System.IO.Stream
    {
        byte[] _buffers;
        int _bufferSize, _Position;
        FileStream _stream;
        public TempStream()
        {
            _buffers = System.Buffers.ArrayPool<Byte>.Shared.Rent(0x1000);// new byte[0x1000];
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;


        public override long Length => _Position == -1 ? _stream.Length : _bufferSize;

        public override long Position
        {
            get => _Position == -1 ? _stream.Position : _Position; set
            {
                if (_Position == -1)
                {
                    _stream.Position = value;
                }
                else
                {
                    _Position = (int)value;
                }
            }
        }

        public override void Flush()
        {
            if (_Position == -1)
            {
                if (_bufferSize > 0)
                {
                    _stream.Write(_buffers, 0, _bufferSize);
                    _bufferSize = 0;
                }
                _stream.Flush();
            }
        }
        public override void Close()
        {
            if (_Position == -1)
            {
                _stream.Close();
            }
            base.Dispose(true);
        }
        protected override void Dispose(bool disposing)
        {
            if (_buffers != null)
            {
                System.Buffers.ArrayPool<Byte>.Shared.Return(_buffers);
            }
            _buffers = null;

            if (_Position == -1)
            {
                _stream.Dispose();
                System.IO.File.Delete(_stream.Name);
            }
            base.Dispose(disposing);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_Position == -1)
            {
                return _stream.Read(buffer, offset, count);
            }
            else
            {
                if (_bufferSize >= count + _Position)
                {
                    Array.Copy(_buffers, _Position, buffer, offset, count);
                    _Position += count;
                    return count;
                }
                else
                {
                    int size = _bufferSize - _Position;
                    Array.Copy(_buffers, _Position, buffer, offset, size);
                    _Position += size;
                    return size;

                }

            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (_Position == -1)
            {
                return _stream.Seek(offset, origin);
            }
            else
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        _Position = (int)offset;
                        break;
                    case SeekOrigin.Current:
                        _Position += (int)offset;
                        break;
                    case SeekOrigin.End:
                        _Position = _bufferSize + (int)offset - 1;

                        break;
                }
                return _Position;
            }
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
        public override void CopyTo(Stream destination, int bsize)
        {
            if (_Position > -1)
            {
                destination.Write(_buffers, _Position, _bufferSize - _Position);
                _Position = _bufferSize;
            }
            else
            {
                int count;
                while ((count = _stream.Read(_buffers, 0, _buffers.Length)) != 0)
                {
                    destination.Write(_buffers, 0, count);
                }

            }
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_Position == -1)
            {
                _stream.Write(buffer, offset, count);
                return;
            }
            if (_bufferSize + count > _buffers.Length)
            {

                if (_Position > -1)
                {
                    _stream = File.Open(Path.GetTempFileName(), FileMode.Create);
                    _Position = -1;

                }

                _stream.Write(_buffers, 0, _bufferSize);
                _bufferSize = 0;
                _stream.Write(buffer, offset, count);

            }
            else
            {
                Array.Copy(buffer, offset, _buffers, _bufferSize, count);
                _bufferSize += count;
            }
        }
    }
}

