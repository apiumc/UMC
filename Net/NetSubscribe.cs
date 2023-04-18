using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UMC.Data;
using System.Linq;

namespace UMC.Net
{
    public class NetSubscribe : NetBridge
    {
        ConcurrentQueue<byte[]> buffers = new ConcurrentQueue<byte[]>();
        protected void Write(byte[] buffer)
        {

            buffers.Enqueue(buffer);
            if (_IsError)
            {
                if (_IsTempWriting == false && _IsErrorModel)
                {
                    _IsTempWriting = true;
                    System.Threading.Tasks.Task.Factory.StartNew(WriteTemp);
                }
            }
            else
            {
                if (_isWriting == false)
                {
                    _isWriting = true;
                    System.Threading.Tasks.Task.Factory.StartNew(Write);
                }
            }
        }
        void WriteTemp()
        {
            var filename = Reflection.ConfigPath(String.Format("Subscribe\\{0}.log", this._Key));
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(filename)))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filename));
            }

            using (var log = System.IO.File.Open(filename, System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
            {
                byte[] b;
                while (buffers.TryDequeue(out b))
                {
                    log.Write(b, 0, b.Length);
                }
                log.Close();
                log.Dispose();
            }
            _IsTempWriting = false;

        }
        bool _isWriting = false, _IsError = false, _IsTempWriting, _IsErrorModel;
        void Write()
        {
            byte[] b;
            while (buffers.TryPeek(out b))
            {
                try
                {
                    this._writer.Write(b, 0, b.Length);
                    buffers.TryDequeue(out b);

                }
                catch
                {
                    this.Close();
                    return;
                }
            }
            _isWriting = false;

        }
        public void Publish()
        {
            _IsErrorModel = false;

            var filename = Reflection.ConfigPath(String.Format("Subscribe\\{0}.log", this._Key));

            if (System.IO.File.Exists(filename))
            {

                using (System.IO.FileStream reader = System.IO.File.OpenRead(filename))
                {
                    var buffer = new byte[9];

                    while (reader.Read(buffer, 0, 9) == 9)
                    {
                        var len = BitConverter.ToInt32(buffer, 5) + 1;
                        var lenBuffer = new byte[len + 10];

                        if (reader.Read(lenBuffer, 9, len) == len)
                        {
                            Array.Copy(buffer, 0, lenBuffer, 0, 9);
                            _writer.Write(lenBuffer, 0, lenBuffer.Length);
                        }
                    }
                    reader.Close();
                    reader.Dispose();
                }
                System.IO.File.Delete(filename);
                _IsError = false;

            }
            if (_isWriting == false && buffers.Count > 0)
            {
                _isWriting = true;
                System.Threading.Tasks.Task.Factory.StartNew(Write);
            }
        }

        String _ip, _Key;
        int _port;

        public string Key => _Key;

        public string Address => _ip;
        public int Port => _port;
        private NetSubscribe()
        {

        }
        public NetSubscribe(String point, String ip, int port, string appSecret)
        {

            this._point = point;
            this._ip = ip;
            this._port = port;
            _Subscribes[this._ip] = this;

            Connect();
            this.appSecret = appSecret;
        }
        string appSecret;
        public void Connect()
        {
            if (this._port > 0)
            {
                TcpClient tcpClient = new TcpClient();
                try
                {
                    tcpClient.BeginConnect(this._ip, this._port, ConnectEnd, tcpClient);
                }
                catch
                {
                    System.Threading.Tasks.Task.Factory.StartNew(async delegate
                    {
                        await System.Threading.Tasks.Task.Delay(10000);

                        Connect();

                    });

                }
            }
        }

        void ConnectEnd(IAsyncResult result)
        {
            var tcpClient = result.AsyncState as TcpClient;

            var buffers = System.Buffers.ArrayPool<byte>.Shared.Rent(0x200);
            try
            {
                tcpClient.EndConnect(result);
                var bridge = tcpClient.GetStream();
                var sb = new StringBuilder();
                sb.Append($"GET / HTTP/1.1\r\n");
                sb.Append("Connection: upgrade\r\n");
                sb.Append("Upgrade: websocket\r\n");
                var nvs = new System.Collections.Specialized.NameValueCollection();
                nvs.Add("umc-request-time", UMC.Data.Utility.TimeSpan().ToString());
                nvs.Add("umc-subscribe-key", this._point);
                nvs.Add("umc-request-protocol", "Subscribe");
                nvs.Add("umc-request-sign", UMC.Data.Utility.Sign(nvs, this.appSecret));

                for (var c = 0; c < nvs.Count; c++)
                {
                    sb.AppendFormat("{0}: {1}\r\n", nvs.GetKey(c), nvs[c]);
                }

                sb.Append($"Host: {this._ip}\r\n\r\n");
                int len = System.Text.Encoding.UTF8.GetBytes(sb.ToString(), buffers);
                bridge.Write(buffers, 0, len);
                len = bridge.Read(buffers, 0, buffers.Length);

                var end = UMC.Data.Utility.FindIndex(buffers, 0, len, HttpMimeBody.HeaderEnd);
                if (end > -1)
                {
                    var headSize = end + 4;
                    var header = new NameValueCollection();

                    HttpStatusCode m_StatusCode;
                    if (ResponseHeader(buffers, 0, headSize, header, out m_StatusCode))
                    {
                        if (m_StatusCode == HttpStatusCode.SwitchingProtocols)
                        {
                            var subscribeKey = header["umc-subscribe-key"];
                            this._Key = subscribeKey;

                            this.Bridge(bridge, bridge);
                            if (len > end + 4)
                            {
                                this.Receive(buffers, headSize, len - headSize);
                            }
                        }
                        else
                        {
                            tcpClient.Close();
                        }
                    }
                    else
                    {
                        tcpClient.Close();
                    }
                }
                else
                {
                    tcpClient.Close();
                }
            }
            catch
            {
                System.Threading.Tasks.Task.Factory.StartNew(async delegate
                {
                    await System.Threading.Tasks.Task.Delay(10000);

                    Connect();

                });
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffers);
            }

        }
        String _point;
        public string Point => _point;
        public override void Close()
        {
            this._IsError = false;
            this._IsErrorModel = true;
            base.Close();
            if (_port > 0)
            {
                if (_Subscribes.ContainsKey(this._ip))
                {
                    System.Threading.Tasks.Task.Factory.StartNew(async delegate
                    {
                        await System.Threading.Tasks.Task.Delay(10000);

                        Connect();

                    });
                }
            }
        }
        public static NetSubscribe[] Subscribes => _Subscribes.Values.ToArray();

        public static NetSubscribe[] Publishes => _Publishes.Values.ToArray();
        public static NetSubscribe Subscribe(NameValueCollection header, String ip, String point, System.IO.Stream stream, string appSecret)
        {
            var ns = new NameValueCollection();
            var hs = header;
            var sign = String.Empty;
            for (var i = 0; i < hs.Count; i++)
            {
                var key = hs.GetKey(i).ToLower();
                switch (key)
                {
                    case "umc-request-sign":
                        sign = hs[i];
                        break;
                    default:
                        if (key.StartsWith("umc-"))
                        {
                            ns.Add(key, Uri.UnescapeDataString(hs[i]));
                        }
                        break;
                }
            }
            var subscribeKey = hs["umc-subscribe-key"];
            if (String.IsNullOrEmpty(subscribeKey) == false)
            {
                if (String.Equals(subscribeKey, point) == false)
                {
                    if (String.IsNullOrEmpty(sign) == false)
                    {
                        //var secret = appSecret
                        if (String.Equals(sign, UMC.Data.Utility.Sign(ns, appSecret)))
                        {
                            if (_Publishes.ContainsKey(subscribeKey))
                            {
                                NetSubscribe slave = _Publishes[subscribeKey];
                                slave.Bridge(stream, stream);
                                return slave;
                            }
                            else
                            {
                                var subscribe = new NetSubscribe();
                                subscribe._Key = subscribeKey;
                                subscribe._point = point;
                                subscribe.appSecret = appSecret;
                                subscribe._ip = ip;
                                subscribe.Bridge(stream, stream);
                                _Publishes[subscribeKey] = subscribe;
                                return subscribe;
                            }

                        }
                    }
                }
            }
            return null;

        }


        protected override void Read(int pid, byte[] buffer, int offset, int count)
        {
            if (pid == 0)
            {
                if (count > 0)
                {
                    var eIndex = UMC.Data.Utility.FindIndex(buffer, offset, offset + count, new byte[] { 46 });
                    if (eIndex > -1)
                    {
                        var len = eIndex - offset;
                        var key = System.Text.Encoding.UTF8.GetString(buffer, offset, len);
                        var message = System.Text.Encoding.UTF8.GetString(buffer, eIndex + 1, count - len - 1);

                        switch (key)
                        {
                            case "Subscribe":
                                switch (message)
                                {
                                    case "Delete":
                                        if (this._port > 0)
                                        {
                                            _Subscribes.Remove(this._ip);

                                            _RemoveConfig();
                                        }
                                        else
                                        {
                                            _Publishes.Remove(this._Key);
                                        }
                                        this.Close();
                                        break;
                                }
                                break;
                            case "Export":
                                var nameCode = UMC.Data.Utility.IntParse(message, 0);
                                if (nameCode != 0)
                                {
                                    var cacheset = HotCache.Caches().FirstOrDefault(r => r.NameCode == nameCode);
                                    if (cacheset != null)
                                    {
                                        System.Threading.Tasks.Task.Factory.StartNew(() => this.Export(cacheset));
                                    }
                                }
                                break;
                            default:

                                _MessageSubscribes[key]?.Subscribe(message);

                                break;
                        }
                    }
                }
            }
            else
            {

                _DataSubscribes[pid]?.Subscribe(buffer, offset, count);

            }
        }

        void Export(UMC.Data.Caches.ICacheSet cacheSet)
        {
            var buffer = new byte[0x200];
            cacheSet.Export(row =>
            {
                var size = 10;
                foreach (var cell in row)
                {
                    size += cell.Length + 6;
                }
                size += 2;
                if (buffer.Length < size)
                {
                    buffer = new byte[size];
                }

                buffer[0] = STX;
                Array.Copy(BitConverter.GetBytes(cacheSet.NameCode), 0, buffer, 1, 4);
                Array.Copy(BitConverter.GetBytes(size - 10), 0, buffer, 5, 4);

                buffer[size - 1] = ETX;
                var index = 9;
                foreach (var cell in row)
                {
                    byte cindex = cell[0];
                    cindex += 16;
                    buffer[index] = cindex;
                    index++;
                    var l = cell.Length - 1;
                    Array.Copy(BitConverter.GetBytes(l), 0, buffer, index, 4);
                    index += 4;

                    Array.Copy(cell, 1, buffer, index, l);
                    index += l;
                    buffer[index] = 13;
                    index++;
                    buffer[index] = 10;
                    index++;

                }
                buffer[index] = 13;
                buffer[index + 1] = 10;
                if (buffer[index + 2] == ETX)
                {
                    _writer.Write(buffer, 0, size);
                }

            });

        }

        public static void Publish(String key, String message)
        {
            var pub = System.Text.Encoding.UTF8.GetBytes($"{key}.{message}");
            if (pub.Length > 0)
            {
                Publish(0, pub, 0, pub.Length);
            }
        }

        static Dictionary<String, IStringSubscribe> _MessageSubscribes = new Dictionary<string, IStringSubscribe>();
        static Dictionary<String, NetSubscribe> _Publishes = new Dictionary<string, NetSubscribe>();
        static Dictionary<String, NetSubscribe> _Subscribes = new Dictionary<string, NetSubscribe>();
        static Dictionary<int, IDataSubscribe> _DataSubscribes = new Dictionary<int, IDataSubscribe>();


        public static void Publish(int publishId, byte[] buffer, int index, int length)
        {
            var m = _Publishes.GetEnumerator();

            while (m.MoveNext())
            {
                m.Current.Value.Write(publishId, buffer, index, length);
            }
        }
        public void Import(int nameCode)
        {

            var data = System.Text.Encoding.UTF8.GetBytes($"Export.{nameCode}");

            var size = data.Length + 10;
            var buffer = new byte[size];
            buffer[0] = STX;
            Array.Copy(BitConverter.GetBytes(0), 0, buffer, 1, 4);
            Array.Copy(BitConverter.GetBytes(data.Length), 0, buffer, 5, 4);
            Array.Copy(data, 0, buffer, 9, data.Length);
            buffer[buffer.Length - 1] = ETX;
            try
            {
                _writer.Write(buffer, 0, size);

            }
            catch
            {
                this.Close();
            }
        }
        public void Remove()
        {

            var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(100);
            var l = System.Text.Encoding.UTF8.GetBytes("Subscribe.Delete", buffer.AsSpan(9));

            buffer[0] = STX;
            BitConverter.TryWriteBytes(buffer.AsSpan(1), 0);
            BitConverter.TryWriteBytes(buffer.AsSpan(5), l);

            buffer[l + 9] = ETX;
            try
            {
                if (this.IsBridging)
                {
                    _writer.Write(buffer, 0, l + 10);
                }
            }
            catch
            {
                this.Close();
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
            if (this._port > 0)
            {
                _Subscribes.Remove(this.Address);
                _RemoveConfig();
                base.Close();
            }
            else
            {
                _Publishes.Remove(this.Key);
                this.Close();
            }

        }
        void _RemoveConfig()
        {

            var Subscribe = Reflection.Configuration("Subscribe") ?? new ProviderConfiguration();

            for (var i = 0; i < Subscribe.Count; i++)
            {
                var subscr = Subscribe[i];
                var url = subscr.Attributes["url"];
                if (String.IsNullOrEmpty(url) == false)
                {
                    if (url.Contains(this._ip))
                    {
                        Subscribe.Remove(subscr.Name);//.RemoveAll(r => r.Name == subscr.Name);
                        break;
                    }
                }

            }
            Subscribe.WriteTo(Reflection.AppDataPath("UMC//Subscribe.xml"));
        }

        public static void Subscribe(String key, IStringSubscribe subscriber)
        {
            _MessageSubscribes[key] = subscriber;
        }

        public static void Subscribe(int nameCode, IDataSubscribe subscriber)
        {
            _DataSubscribes[nameCode] = subscriber;
        }

    }

}
