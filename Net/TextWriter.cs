using System;
using System.IO;
using System.Text;

namespace UMC.Net
{

    public class TextWriter : System.IO.TextWriter
    {
        NetReadData _hlr;
        byte[] buffers;
        byte[] _buffers;
        int bufferSize = 0;
        char[] _chars = new char[1];
        Encoding _encoding;
        public TextWriter(NetReadData readData)
        {
            _buffers = System.Buffers.ArrayPool<byte>.Shared.Rent(0x200);
            buffers = _buffers;//new byte[0x200];
            this._hlr = readData;
            _encoding = System.Text.Encoding.UTF8;
        }
        public TextWriter(NetReadData readData, byte[] buffer)
        {
            buffers = buffer;
            this._hlr = readData;
            _encoding = System.Text.Encoding.UTF8;
        }
        public override Encoding Encoding
        {
            get
            {
                return _encoding;
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (_buffers != null)
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(_buffers);
                _buffers = null;
            }
            buffers = null;
            base.Dispose(disposing);
        }
        public override void Write(char value)
        {
            if (bufferSize + 6 > buffers.Length)
            {
                _hlr(buffers, 0, bufferSize);
                bufferSize = 0;
            }
            _chars[0] = value;
            bufferSize += _encoding.GetBytes(_chars, 0, 1, buffers, bufferSize);


        }
        public void Write(byte[] buffer, int offset, int count)
        {
            if (bufferSize + count > buffers.Length)
            {
                _hlr(buffers, 0, bufferSize);
                _hlr(buffer, offset, count);
                bufferSize = 0;

            }
            else
            {
                Array.Copy(buffer, offset, buffers, bufferSize, count);
                bufferSize += count;
            }

        }
        public override void Flush()
        {
            if (bufferSize > 0)
            {
                _hlr(buffers, 0, bufferSize);
                bufferSize = 0;
            }
        }
    }
}
