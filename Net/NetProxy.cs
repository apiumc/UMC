using System;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.ComTypes;

namespace UMC.Net
{

    public class NetPool
    {
        ConcurrentStack<NetProxy> _pool = new ConcurrentStack<NetProxy>();
        public ConcurrentStack<NetProxy> Unwanted
        {
            get
            {
                return _pool;
            }
        }
        ConcurrentDictionary<int, NetProxy> _Links = new ConcurrentDictionary<int, NetProxy>();
        public ConcurrentDictionary<int, NetProxy> Bursts
        {
            get
            {
                return _Links;
            }
        }
        /// <summary>
        /// 连接错误
        /// </summary>
        public int BurstError
        {
            get;
            set;
        }
    }

    /// <summary>
    /// HTTP请求代码与HttpClient机制增加了端口复用
    /// </summary>
    public abstract class NetProxy
    {

        /// <summary>
        ///  是否新建连接，新连接异常直接把异常返回客户端
        /// </summary>
        public abstract bool IsNew
        {
            get;
        }
        /// <summary>
        /// 确认是连接是否可用
        /// </summary>
        /// <returns></returns>
        public abstract bool Active();

        /// <summary>
        /// 注册接收HTTP的接收器
        /// </summary>
        public virtual void Before(HttpMimeBody httpMime)
        {

        }

        /// <summary>
        /// 发送HTTP头部信息
        /// </summary>
        public abstract void Header(byte[] buffer, int offset, int size);
        /// <summary>
        /// 发送HTTP正文内容
        /// </summary>
        public abstract void Body(byte[] buffer, int offset, int size);
        /// <summary>
        /// 回收端口
        /// </summary>
        public virtual void Recovery()
        {


        }
        /// <summary>
        /// 
        /// </summary>
        public virtual void Dispose()
        {

        }
        //public abstract
        /// <summary>
        /// 准备接收
        /// </summary>
        public abstract void Receive();

        /// <summary>
        /// 创建新连接
        /// </summary>
        public abstract NetProxy Create(Uri uri, int timeout);
        /// <summary>
        /// 准备新连接异步
        /// </summary>
        public abstract void Create(Uri uri, int timeout, Action<NetProxy> action, Action<Exception> error);
        /// <summary>
        /// 连接池
        /// </summary>
        static ConcurrentDictionary<string, NetPool> _pool = new ConcurrentDictionary<string, NetPool>();

        /// <summary>
        /// 连接池
        /// </summary>
        public static ConcurrentDictionary<string, NetPool> Pool
        {
            get
            {
                return _pool;
            }
        }

        /// <summary>
        /// 采用连接池的方式获取网络请求代理
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="timeout"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static NetProxy Instance(Uri uri, int timeout, long length)
        {
            var key = uri.Authority;
            if (length < 102400)
            {

                NetProxy tc;
                NetPool pool;
                if (Pool.TryGetValue(key, out pool))
                {
                    if (pool.Unwanted.TryPop(out tc))
                    {
                        if (tc.Active())
                        {
                            return tc;
                        }
                    }
                    while (pool.Unwanted.TryPop(out tc))
                    {
                        tc.Dispose();
                    }
                }


            }
            NetProxy proxy = new NetTcp();

            return proxy.Create(uri, timeout);


        }
        public static void Check()
        {

            var ps = _pool.Values.ToArray();
            foreach (var p in ps)
            {
                var ks = p.Unwanted.ToArray();
                foreach (var k in ks)
                {
                    k.Active();
                }
            }
        }
        public static void CloseBurst()
        {
            var ps = _pool.Values.ToArray();
            foreach (var p in ps)
            {
                if (p.BurstError > 3)
                {
                    var ks = p.Bursts.Values.ToArray();
                    foreach (var k in ks)
                    {
                        k.Dispose();
                    }
                }
            }
        }
        /// <summary>
        /// 采用连接池的方式获取网络请求代理异步
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="timeout"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static void Instance(Uri uri, int timeout, long length, Action<NetProxy> action, Action<Exception> error)
        {
            var key = uri.Authority;

            if (length < 102400)
            {
                NetPool pool;
                if (Pool.TryGetValue(key, out pool))
                {
                    NetProxy tc;
                    if (pool.Unwanted.TryPop(out tc))
                    {
                        if (tc.Active())
                        {
                            action(tc);
                            return;
                        }
                    }
                    if (pool.BurstError > 10)
                    {
                        error(new System.Net.WebException($"{key}正忙碌"));
                        return;

                    }
                }

            }

            NetProxy proxy = new NetTcp();
            proxy.Create(uri, timeout, action, error);


        }
    }
    /// <summary>
    /// 对NetProxy默认实现
    /// </summary>
    class NetTcp : NetProxy, IDisposable
    {
        class AsyncResult
        {
            public Action<NetProxy> action;
            public Action<Exception> error;
        }
        static bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;

        }

        void Init(String key, String host, int post, bool isSsl, int timeout, AsyncResult result)
        {

            var tcp = this;
            tcp.key = key;
            tcp.port = post;
            tcp.host = host;
            tcp.activeTime = DateTime.Now;
            tcp.isSsl = isSsl;
           
            tcp._IsNew = true;
            Connect(tcp, host, port, timeout, result);


        }
        async void Connect(NetTcp tcp, string host, int port, int timeout, AsyncResult result)
        {
            var client = new Socket(SocketType.Stream, ProtocolType.Tcp);
            if (timeout > 0)
            {
                client.ReceiveTimeout = timeout;
            }
            NetPool pool = Pool.GetOrAdd(this.key, r => new NetPool());
            try
            {
                await client.ConnectAsync(host, port);
                if (isSsl)
                {
                    SslStream ssl = new SslStream(new NetworkStream(client, true), false, RemoteCertificateValidationCallback);
                    await ssl.AuthenticateAsClientAsync(this.host, new X509CertificateCollection(), SslProtocols.None, false);

                    this._stream = ssl;
                    pool.Bursts[this.GetHashCode()] = this;
                    result.action(this);

                }
                else
                {
                    this._stream = new NetworkStream(client, true);
                    pool.Bursts[this.GetHashCode()] = this;
                    result.action(this);
                }
            }
            catch (Exception ex)
            {
                pool.BurstError++;

                result.error(ex);
            }

        }
        void Init(String key, String host, int post, bool isSSL, int timeout)
        {
            var tcp = this;
            tcp.key = key;
            var client = new Socket(SocketType.Stream, ProtocolType.Tcp);
            tcp.port = post;
            tcp.host = host;
            tcp.activeTime = DateTime.Now;
            tcp.isSsl = isSSL;
            if (timeout > 0)
            {
                client.ReceiveTimeout = timeout;
            }
            client.Connect(tcp.host, tcp.port);
            tcp._IsNew = true;

            if (isSSL)
            {
                SslStream ssl = new SslStream(new NetworkStream(client, true), false, RemoteCertificateValidationCallback);
                ssl.AuthenticateAsClient(this.host, new X509CertificateCollection(), SslProtocols.None, false);

                this._stream = ssl;
            }
            else
            {
                this._stream = new NetworkStream(client, true);// this.client.GetStream();
            }

            NetPool pool = Pool.GetOrAdd(this.key, r => new NetPool());
            pool.Bursts[this.GetHashCode()] = this;


        }
        public override NetProxy Create(Uri uri, int timeout)
        {
            Init(uri.Authority, uri.Host, uri.Port, string.Equals(uri.Scheme, "https"), timeout);
            return this;
        }
        public override void Create(Uri uri, int timeout, Action<NetProxy> action, Action<Exception> error)
        {
            Init(uri.Authority, uri.Host, uri.Port, string.Equals(uri.Scheme, "https"), timeout, new AsyncResult { action = action, error = error });

        }
        String key;
        Stream _stream;
        public DateTime activeTime;
        int _keepAlive = 60;
        String host;
        int port;
        bool _IsNew, isSsl;

        byte[] _sendBuffers = new byte[0x200];
        int _sendBufferSize = 0;
        public override bool IsNew => _IsNew;

        bool disposed = false;
        public override void Before(HttpMimeBody httpMime)
        {
            this.mimeBody = httpMime;
        }
        bool IsError = false;
        Exception _error;
        public override void Body(byte[] buffer, int offset, int size)
        {
            if (_IsNew == false)
            {
                if (_sendBufferSize + size > _sendBuffers.Length)
                {
                    byte[] headerByte = new byte[_sendBufferSize + size + 1024];
                    Array.Copy(_sendBuffers, 0, headerByte, 0, _sendBufferSize);
                    this._sendBuffers = headerByte;
                }
                Array.Copy(buffer, offset, _sendBuffers, _sendBufferSize, size);
                _sendBufferSize += size;
            }

            try
            {
                if (IsError == false)
                    _stream.Write(buffer, offset, size);
            }
            catch (Exception ex)
            {
                IsError = true;
                _error = ex;
            }
        }
        public override void Header(byte[] buffer, int offset, int size)
        {
            _sendBufferSize = 0;
            Body(buffer, offset, size);
        }

        public override void Recovery()
        {
            if (disposed == false)
            {
                if (this.mimeBody.IsClose || this.mimeBody.IsHttpFormatError)
                {
                    NetPool pool;
                    if (Pool.TryGetValue(this.key, out pool))
                    {
                        pool.BurstError = 0;
                        NetProxy netProxy;
                        pool.Bursts.TryRemove(GetHashCode(), out netProxy);
                    }

                    this.Dispose();
                }
                else
                {
                    this._keepAlive = this.mimeBody.KeepAlive;
                    var qu = Pool.GetOrAdd(this.key, k => new NetPool());
                    qu.BurstError = 0;
                    this.activeTime = DateTime.Now;
                    qu.Unwanted.Push(this);
                    NetProxy netProxy;
                    qu.Bursts.TryRemove(GetHashCode(), out netProxy);
                    this.mimeBody = null;
                }
            }
        }
        public override void Dispose()
        {
            if (disposed == false)
            {
                NetPool pool;
                if (Pool.TryGetValue(this.key, out pool))
                {
                    pool.BurstError = 0;

                    NetProxy netProxy;
                    pool.Bursts.TryRemove(GetHashCode(), out netProxy);
                }

                disposed = true;
                if (_stream != null)
                {
                    _stream.Close();
                    _stream.Dispose();
                }
                _buffers = null;
                mimeBody = null;
                this._sendBuffers = null;
                GC.SuppressFinalize(this);
            }
        }

        ~NetTcp()
        {
            Dispose();
        }
        public override bool Active()
        {
            this._IsNew = false;
            if (disposed == false)
            {
                if (this._keepAlive > 0)
                {
                    var now = DateTime.Now;
                    if (this.activeTime.AddSeconds(this._keepAlive) < now)
                    {
                        this.Dispose();
                        return false;
                    }

                    var qu = Pool.GetOrAdd(this.key, k => new NetPool());
                    qu.Bursts[this.GetHashCode()] = this;
                    return true;
                }
                else
                {
                    var now = DateTime.Now;
                    if (this.activeTime.AddSeconds(60) < now)
                    {
                        this.Dispose();
                        return false;
                    }
                    var qu = Pool.GetOrAdd(this.key, k => new NetPool());
                    qu.Bursts[this.GetHashCode()] = this;
                    return true;
                }
            }
            return false;


        }

        HttpMimeBody mimeBody;
        public override void Receive()
        {
            if (IsError)
            {
                if (this.IsNew || _sendBufferSize <= 0)
                {
                    throw _error;
                }
                Reset();
            }
            else
            {
                if (this.mimeBody == null)
                {
                    throw new ArgumentNullException("MimeBody");
                }
                try
                {
                    HeaderRead();
                }
                catch
                {
                    if (this.IsNew || _sendBufferSize <= 0)
                    {
                        throw;
                    }

                    Reset();

                }
            }

        }
        void Reset()
        {
            NetPool queue;
            if (Pool.TryGetValue(key, out queue))
            {
                NetProxy tc;
                while (queue.Unwanted.TryPop(out tc))
                {
                    tc.Dispose();
                }
            }
            this.IsError = false;
            this._stream.Close();
            this._stream.Dispose();
            this.Init(this.key, this.host, this.port, this.isSsl, 0, new AsyncResult
            {
                action = async (tcp) =>
                {
                    await _stream.WriteAsync(_sendBuffers, 0, _sendBufferSize);
                    HeaderRead();
                },
                error = ex =>
                {
                    var m = this.mimeBody;
                    this.Dispose();
                    m.ReceiveException(ex);
                }
            });


        }

        async void HeaderRead()
        {
            if (disposed)
            {
                return;
            }
            var m = this.mimeBody;
            if (m == null)
            {
                this.Dispose();
                return;
            }
            Exception _error = null;
            int l = 0;
            try
            {
                l = await _stream.ReadAsync(_buffers, 0, _buffers.Length);

            }
            catch (Exception ex)
            {
                _error = ex;
            }
            if (l == 0)
            {
                if (this.IsNew == false && _sendBufferSize > 0)
                {
                    Reset();
                }
                else
                {
                    NetPool pool;
                    if (Pool.TryGetValue(this.key, out pool))
                    {
                        pool.BurstError++;
                    }
                    this.Dispose();
                    m.ReceiveException(_error ?? new ArgumentOutOfRangeException("请求长度不正常"));

                }
                return;
            }
            _sendBufferSize = 0;
            this.activeTime = DateTime.Now;


            try
            {
                m.Receive(_buffers, 0, l);

            }
            catch (Exception ex)
            {

                this.Dispose();
                m.ReceiveException(ex);
                return;
            }

            if (m.IsMimeFinish)
            {
                this.Recovery();
            }
            else if (m.IsHttpFormatError)
            {
                this.Dispose();
            }
            else
            {
                DataRead();
            }
        }
        int ctime = 0;

        async void DataRead()
        {
            if (disposed)
            {
                return;
            }
            ctime++;
            var m = this.mimeBody;
            var l = 0;
            try
            {
                l = await _stream.ReadAsync(_buffers, 0, _buffers.Length);
            }
            catch (Exception ex)
            {
                NetPool pool;
                if (Pool.TryGetValue(this.key, out pool))
                {
                    pool.BurstError++;
                }
                this.Dispose();
                m.ReceiveException(ex);
                return;
            }
            if (l == 0)
            {
                NetPool pool;
                if (Pool.TryGetValue(this.key, out pool))
                {
                    pool.BurstError++;
                }
                this.Dispose();
                m.ReceiveException(new Exception("接口数据长度为零"));
                return;
            }
            this.activeTime = DateTime.Now;



            m.Receive(_buffers, 0, l);

            if (m.IsMimeFinish)
            {
                this.Recovery();
            }
            else if (m.IsHttpFormatError)
            {

                this.Dispose();
            }
            else
            {
                if (ctime > 30000)
                {
                    this.Dispose();
                    m.ReceiveException(new StackOverflowException("获取内容超过30000次"));
                    return;
                }

                DataRead();
            }
        }

        byte[] _buffers = new byte[0x1000];


    }
}
