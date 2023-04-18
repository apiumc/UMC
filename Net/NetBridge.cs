using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.IO.Pipes;
using System.Net;
using System.Xml.Linq;

namespace UMC.Net
{

    public abstract class NetBridge
    {

        public static bool ResponseHeader(byte[] data, int offset, int size, NameValueCollection headers, out HttpStatusCode m_StatusCode)
        {
            m_StatusCode = HttpStatusCode.InternalServerError;
            var utf = System.Text.Encoding.UTF8;

            var start = offset;
            try
            {
                for (var ci = 0; ci < size - 2; ci++)
                {
                    var index = ci + offset;

                    if (data[index] == 10 && data[index - 1] == 13)
                    {
                        var heaerValue = utf.GetString(data, start, index - start - 1);
                        if (start == offset)
                        {

                            var l = heaerValue.IndexOf(' ');
                            if (l > 0 && heaerValue.StartsWith("HTTP/"))
                            {
                                heaerValue = heaerValue.Substring(l + 1);
                                var fhv = heaerValue.IndexOf(' ');
                                if (fhv > 0)
                                {

                                    m_StatusCode = UMC.Data.Utility.Parse(heaerValue.Substring(0, fhv), HttpStatusCode.Continue);


                                }
                                else
                                {
                                    m_StatusCode = UMC.Data.Utility.Parse(heaerValue, HttpStatusCode.Continue);
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            var vi = heaerValue.IndexOf(':');
                            var key = heaerValue.Substring(0, vi);
                            var value = heaerValue.Substring(vi + 2);
                            headers.Add(key, value);
                        }

                        start = index + 1;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        protected const byte STX = 0x02;
        protected const byte ETX = 0x03;
        System.IO.Stream _read;
        protected System.IO.Stream _writer;

        public void Bridge(System.IO.Stream writeStream, System.IO.Stream readStream)
        {
            this._writer = writeStream;
            this._read = readStream;
            this._IsBridging = true;
            try
            {
                this._data = new byte[0x600];
                Read();
            }
            catch
            {
                this.Close();
            }

        }
        async void Read()
        {
            var l = 0;
            try
            {

                l = await this._read.ReadAsync(this._data, 0, this._data.Length);
            }
            catch
            {
                Close();
                return;
            }
            if (l > 0)
            {
                this.Receive(_data, 0, l);
            }
            else
            {

                Close();
                return;
            }
            Read();

        }

        byte[] _data;
        bool _IsBridging;
        byte[] _header = new byte[9];
        public bool IsBridging => _IsBridging;
        public virtual void Close()
        {
            _data = null;
            _IsBridging = false;
            this._writer.Close();
            this._writer.Dispose();
            if (this._writer != null)
            {
                if (this._read != _writer)
                {
                    this._read.Close();
                    this._read.Dispose();
                }
            }
        }
        public virtual void Write(int pid, Span<byte> bytes)
        {
            _header[0] = STX;
            BitConverter.TryWriteBytes(_header.AsSpan(1), pid);
            BitConverter.TryWriteBytes(_header.AsSpan(5), bytes.Length);
            try
            {

                this._writer.Write(_header, 0, 9);
                if (bytes.Length > 0)
                {
                    this._writer.Write(bytes);
                }

            }
            catch
            {
                this.Close();
            }

        }
        public virtual void Write(int pid, Span<byte> bytes1, Span<byte> bytes2)
        {
            _header[0] = STX;
            BitConverter.TryWriteBytes(_header.AsSpan(1), pid);
            BitConverter.TryWriteBytes(_header.AsSpan(5), bytes1.Length+ bytes2.Length);
            try
            {

                this._writer.Write(_header, 0, 9);
                if (bytes1.Length > 0)
                {
                    this._writer.Write(bytes1);
                }
                if (bytes2.Length > 0)
                {
                    this._writer.Write(bytes2);
                }

            }
            catch
            {
                this.Close();
            }
        }

        public virtual void Write(int pid, byte[] data, int offset, int count)
        {
            _header[0] = STX;
            BitConverter.TryWriteBytes(_header.AsSpan(1), pid);
            BitConverter.TryWriteBytes(_header.AsSpan(5), count);
            try
            {

                this._writer.Write(_header, 0, 9);
                if (count > 0)
                {
                    this._writer.Write(data, offset, count);
                }

            }
            catch
            {
                this.Close();
            }


        }

        int curpid = 0, length = 0, _bufferSize;
        byte[] _buffer = new byte[9];


        protected void Receive(byte[] buffer, int offset, int size)
        {
            int len = length, pid = curpid;
            if (_bufferSize > 0)
            {
                var s = 9 - _bufferSize;
                Array.Copy(buffer, offset, _buffer, _bufferSize, s);
                pid = BitConverter.ToInt32(_buffer, 1);
                len = BitConverter.ToInt32(_buffer, 5);

                if (len == 0)
                {
                    Read(pid, buffer, offset, len);
                }

                _bufferSize = 0;
                size -= s;
                offset += s;
            }
            int postion = offset;

            while (size + offset > postion)
            {
                if (len == 0)
                {
                    if (buffer[postion] == STX)
                    {
                        if (postion + 9 < size + offset)
                        {
                            pid = BitConverter.ToInt32(buffer, postion + 1);
                            len = BitConverter.ToInt32(buffer, postion + 5);
                            if (len == 0)
                            {
                                Read(pid, buffer, postion, len);

                            }
                            postion += 9;
                        }
                        else
                        {
                            _bufferSize = size + offset - postion;

                            Array.Copy(buffer, postion, _buffer, 0, _bufferSize);
                            break;
                        }
                    }
                    else if (buffer[postion] == ETX)
                    {
                        postion++;
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }


                if (len > 0)
                {
                    var index = postion;
                    int count = size + offset - postion;
                    if (count > 0)
                    {
                        int length = len;
                        if (count > len)
                        {
                            postion += len;
                            len = 0;
                        }
                        else
                        {
                            length = count;
                            postion += count;
                            len -= count;
                        }


                        Read(pid, buffer, index, length);

                    }

                }
            }
            curpid = pid;
            length = len;

        }
        protected virtual void Read(int pid, byte[] buffer, int index, int length)
        {

        }
    }
}
