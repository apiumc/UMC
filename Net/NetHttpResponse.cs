using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace UMC.Net
{
    /// <summary>
    /// 功能与HttpWebResponse一样，网络请求只是用
    /// </summary>

    public class NetHttpResponse : HttpMimeBody, IDisposable
    {

        HttpWebRequest webRequest;

        string m_ProtocolVersion;
        public String ProtocolVersion
        {
            get
            {
                return m_ProtocolVersion;
            }
        }
        public bool IsHead => _isHead;
        bool _isHead;

        static void Http(NetContext context, HttpWebRequest webRequest, Action<NetHttpResponse> prepare)
        {
            NetHttpResponse httpResponse = new NetHttpResponse(prepare);
            httpResponse.webRequest = webRequest;
            httpResponse._isHead = String.Equals("HEAD", webRequest.Method, StringComparison.CurrentCultureIgnoreCase);

            NetProxy.Instance(webRequest.RequestUri, webRequest.ReadWriteTimeout, context.ContentLength ?? 0, tcp =>
             {
                 tcp.Before(httpResponse);

                 var header = System.Buffers.ArrayPool<byte>.Shared.Rent(0x200);
                 try
                 {
                     Header(webRequest, new TextWriter(tcp.Header, header));
                 }
                 finally
                 {
                     System.Buffers.ArrayPool<byte>.Shared.Return(header);
                 }
                 context.ReadAsData((b, i, c) =>
                 {
                     if (b.Length == 0 && c == 0)
                     {
                         if (i == -1)
                         {
                             tcp.Dispose();
                             httpResponse.IsHttpFormatError = true;
                             httpResponse.ReceiveError(new WebException("接收Body异常"));
                         }
                         else
                         {
                             try
                             {
                                 tcp.Receive();
                             }
                             catch (Exception ex)
                             {
                                 tcp.Dispose();
                                 httpResponse.IsHttpFormatError = true;
                                 httpResponse.ReceiveError(ex);
                             }
                         }
                     }
                     else
                     {
                         tcp.Body(b, i, c);
                     }
                 });
             }, ex =>
             {
                 httpResponse.IsHttpFormatError = true;
                 httpResponse.ReceiveError(ex);
             });

        }
        static void Http(Stream input, HttpWebRequest webRequest, Action<NetHttpResponse> prepare)
        {
            NetHttpResponse httpResponse = new NetHttpResponse(prepare);
            httpResponse.webRequest = webRequest;
            httpResponse._isHead = String.Equals("HEAD", webRequest.Method);

            NetProxy.Instance(webRequest.RequestUri, webRequest.ReadWriteTimeout, webRequest.ContentLength, tcp =>
            {
                tcp.Before(httpResponse);

                var header = System.Buffers.ArrayPool<byte>.Shared.Rent(0x200);
                try
                {
                    Header(webRequest, new TextWriter(tcp.Header, header));
                    if (input != null)
                    {
                        int i = input.Read(header, 0, header.Length);
                        while (i > 0)
                        {
                            tcp.Body(header, 0, i);
                            i = input.Read(header, 0, header.Length);
                        }
                        input.Close();
                        input.Dispose();
                    }

                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(header);
                }
                tcp.Receive();
            }, ex =>
            {
                httpResponse.IsHttpFormatError = true;
                httpResponse.ReceiveError(ex);
            });
        }
        static NetHttpResponse Http(Action<NetProxy, byte[]> input, HttpWebRequest webRequest)
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            bool IsTimeOut = true;
            NetHttpResponse httpResponse = new NetHttpResponse((r) =>
            {
                if (IsTimeOut)
                {
                    IsTimeOut = false;
                    mre.Set();
                }

            });
            httpResponse.webRequest = webRequest;
            NetProxy.Instance(webRequest.RequestUri, webRequest.ReadWriteTimeout, webRequest.ContentLength, tcp =>
            {
                tcp.Before(httpResponse);

                var hData = System.Buffers.ArrayPool<byte>.Shared.Rent(0x200);
                httpResponse._isHead = String.Equals("HEAD", webRequest.Method, StringComparison.CurrentCultureIgnoreCase);
                try
                {
                    Header(webRequest, new TextWriter(tcp.Header, hData));

                    if (input != null)
                    {
                        input(tcp, hData);
                    }
                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(hData);

                }

                tcp.Receive();

            }, ex =>
            {
                httpResponse.ReceiveError(ex);
            });
            mre.WaitOne(120000);
            mre.Close();
            mre.Dispose();

            if (IsTimeOut)
            {
                IsTimeOut = false;
                httpResponse._error = new TimeoutException();
            }
            if (httpResponse._error != null)
            {
                throw httpResponse._error;
            }
            return httpResponse;
        }
        static void CheckHeader(HttpWebRequest webRequest)
        {
            if (webRequest.CookieContainer != null)
            {
                String cookie;
                if (webRequest.CookieContainer is NetCookieContainer)
                {
                    cookie = ((NetCookieContainer)webRequest.CookieContainer).GetCookieHeader(webRequest.RequestUri);
                }
                else
                {
                    cookie = webRequest.CookieContainer.GetCookieHeader(webRequest.RequestUri);
                }
                if (String.IsNullOrEmpty(cookie) == false)
                {
                    webRequest.Headers[HttpRequestHeader.Cookie] = cookie;
                }
            }
            if (String.IsNullOrEmpty(webRequest.Headers[HttpRequestHeader.Host]))
            {
                webRequest.Headers[HttpRequestHeader.Host] = webRequest.Host;
            }
            webRequest.Headers["Connection"] = "keep-alive";

        }
        Exception _error;

        protected override void ReceiveError(Exception ex)
        {
            if (ex == null)
            {
                _error = new Exception("网络请求异常");
            }
            else
            {
                _error = ex;
            }
            if (this.Prepare != null)
            {
                m_StatusCode = HttpStatusCode.BadGateway;
                this.ContentType = "text/plain; charset=utf-8";
                var errStr = _error.ToString();
                var by = System.Buffers.ArrayPool<byte>.Shared.Rent(System.Text.ASCIIEncoding.UTF8.GetByteCount(errStr));

                var size = System.Text.ASCIIEncoding.UTF8.GetBytes(errStr, 0, errStr.Length, by, 0);
                this.ContentLength = size;

                try
                {
                    this.Prepare(this);
                    this.Body(by, 0, size);
                    this.Finish();
                }
                catch (Exception ex3)
                {
                    UMC.Data.Utility.Error("HTTP", this.webRequest.RequestUri.AbsoluteUri, ex3.ToString());
                }
                finally
                {
                    this.Prepare = null;
                    System.Buffers.ArrayPool<byte>.Shared.Return(by);
                }

            }
            else
            {
                _readData?.Invoke(Array.Empty<byte>(), -1, 0);
            }
        }
        internal static NetHttpResponse Create(HttpWebRequest webRequest, String method)
        {
            CheckHeader(webRequest);
            webRequest.Method = method;

            return Http((r, t) => { }, webRequest);

        }
        internal static NetHttpResponse Create(HttpWebRequest webRequest, String method, string context)
        {
            CheckHeader(webRequest);
            webRequest.Method = method;
            var uf8 = System.Text.Encoding.UTF8;
            var mlength = uf8.GetByteCount(context);
            webRequest.ContentLength = mlength;



            return Http((tcp, ms) =>
            {
                int i = 0, bufferSize = 0;

                while (i < context.Length)
                {
                    if (bufferSize + 6 > ms.Length)
                    {
                        tcp.Body(ms, 0, bufferSize);
                        bufferSize = 0;
                    }

                    bufferSize += uf8.GetBytes(context, i, 1, ms, bufferSize);
                    i++;
                }
                if (bufferSize > 0)
                {
                    tcp.Body(ms, 0, bufferSize);
                }

            }, webRequest);

        }
        static void Header(HttpWebRequest webRequest, TextWriter writer)
        {
            var webHeader = webRequest.Headers;
            writer.Write(webRequest.Method.ToUpper());
            writer.Write(" ");
            writer.Write(webRequest.RequestUri.PathAndQuery);
            writer.Write(" HTTP/1.1\r\n");

            foreach (string item in webHeader)
            {
                string value = webHeader.Get(item);
                writer.Write(item);
                writer.Write(": ");
                writer.Write(value);
                writer.Write("\r\n");
            }
            writer.Write("\r\n");
            writer.Flush();
        }
        public static int Header(HttpWebRequest webRequest, byte[] hData)
        {
            var webHeader = webRequest.Headers;
            int blength = 0;
            blength = System.Text.UTF8Encoding.UTF8.GetBytes(webRequest.Method.ToUpper(), 0, webRequest.Method.Length, hData, 0);
            blength += System.Text.UTF8Encoding.UTF8.GetBytes(" ", 0, 1, hData, blength);
            blength += System.Text.UTF8Encoding.UTF8.GetBytes(webRequest.RequestUri.PathAndQuery, 0, webRequest.RequestUri.PathAndQuery.Length, hData, blength);
            blength += System.Text.UTF8Encoding.UTF8.GetBytes(" HTTP/1.1\r\n", 0, 11, hData, blength);


            foreach (string item in webHeader)
            {
                string value = webHeader.Get(item);
                blength += System.Text.UTF8Encoding.UTF8.GetBytes(item, 0, item.Length, hData, blength);
                blength += System.Text.UTF8Encoding.UTF8.GetBytes(": ", 0, 2, hData, blength);
                blength += System.Text.UTF8Encoding.UTF8.GetBytes(value, 0, value.Length, hData, blength);
                blength += System.Text.UTF8Encoding.UTF8.GetBytes("\r\n", 0, 2, hData, blength);
            }
            blength += System.Text.UTF8Encoding.UTF8.GetBytes("\r\n", 0, 2, hData, blength);
            return blength;
        }
        internal static NetHttpResponse Create(HttpWebRequest webRequest, String method, System.Net.Http.HttpContent context)
        {
            if (context.Headers != null)
            {
                if (context.Headers.ContentType != null)
                {
                    webRequest.ContentType = context.Headers.ContentType.ToString();
                }
            }
            CheckHeader(webRequest);
            webRequest.ContentLength = context.Headers.ContentLength ?? 0;
            webRequest.Method = method;

            return Http((tcp, buffer) =>
            {
                var input = context.ReadAsStreamAsync().Result;

                int i = input.Read(buffer, 0, buffer.Length);
                while (i > 0)
                {
                    tcp.Body(buffer, 0, i);
                    i = input.Read(buffer, 0, buffer.Length);
                }
                input.Close();
                input.Dispose();
                context.Dispose();

            }, webRequest);

        }
        internal static NetHttpResponse Create(HttpWebRequest webRequest, String method, System.IO.Stream context, long contentLength)
        {

            CheckHeader(webRequest);
            if (contentLength >= 0)
            {
                webRequest.ContentLength = contentLength;
            }
            webRequest.Method = method;

            return Http((tcp, buffer) =>
            {
                int i = context.Read(buffer, 0, buffer.Length);
                while (i > 0)
                {
                    tcp.Body(buffer, 0, i);
                    i = context.Read(buffer, 0, buffer.Length);
                }

            }, webRequest);

        }
        internal static void Create(HttpWebRequest webRequest, String method, Action<NetHttpResponse> prepare)
        {
            CheckHeader(webRequest);


            webRequest.Method = method;

            NetHttpResponse httpResponse = new NetHttpResponse(prepare);
            httpResponse.webRequest = webRequest;
            httpResponse._isHead = String.Equals("HEAD", method, StringComparison.CurrentCultureIgnoreCase);


            NetProxy.Instance(webRequest.RequestUri, webRequest.ReadWriteTimeout, 0, tcp =>
            {
                tcp.Before(httpResponse);
                var header = System.Buffers.ArrayPool<byte>.Shared.Rent(0x200);
                try
                {

                    Header(webRequest, new TextWriter(tcp.Header, header));
                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(header);
                }
                tcp.Receive();
            }, ex =>
            {
                httpResponse.IsHttpFormatError = true;
                httpResponse.ReceiveError(ex);
            });



        }
        internal static void Create(HttpWebRequest webRequest, String method, string context, Action<NetHttpResponse> prepare)
        {
            CheckHeader(webRequest);
            webRequest.Method = method;

            NetHttpResponse httpResponse = new NetHttpResponse(prepare);
            httpResponse.webRequest = webRequest;

            NetProxy.Instance(webRequest.RequestUri, webRequest.ReadWriteTimeout, 0, tcp =>
            {
                var header = System.Buffers.ArrayPool<byte>.Shared.Rent(0x200);
                try
                {
                    tcp.Before(httpResponse);

                    webRequest.ContentLength = System.Text.Encoding.UTF8.GetByteCount(context);

                    httpResponse._isHead = String.Equals("HEAD", webRequest.Method);
                    Header(webRequest, new TextWriter(tcp.Header, header));


                    int i = 0, bufferSize = 0;

                    while (i < context.Length)
                    {
                        if (bufferSize + 6 > header.Length)
                        {
                            tcp.Body(header, 0, bufferSize);
                            bufferSize = 0;
                        }

                        bufferSize += System.Text.Encoding.UTF8.GetBytes(context, i, 1, header, bufferSize);
                        i++;
                    }
                    if (bufferSize > 0)
                    {
                        tcp.Body(header, 0, bufferSize);
                    }
                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(header);
                }
                tcp.Receive();
            }, ex =>
            {
                httpResponse.IsHttpFormatError = true;
                httpResponse.ReceiveError(ex);
            });

        }
        internal static void Create(HttpWebRequest webRequest, String method, System.Net.Http.HttpContent context, Action<NetHttpResponse> prepare)
        {
            if (context.Headers != null)
            {
                if (context.Headers.ContentType != null)
                {
                    webRequest.ContentType = context.Headers.ContentType.ToString();
                }
            }
            CheckHeader(webRequest);
            webRequest.Method = method;
            webRequest.ContentLength = context.Headers.ContentLength ?? 0;

            Http(context.ReadAsStreamAsync().Result, webRequest, prepare);

        }
        internal static void Create(HttpWebRequest webRequest, NetContext context, Action<NetHttpResponse> prepare)
        {

            CheckHeader(webRequest);
            if (context.ContentLength >= 0)
            {
                webRequest.ContentLength = context.ContentLength.Value;
            }
            webRequest.Method = context.HttpMethod;
            Http(context, webRequest, prepare);

        }
        internal static void Create(HttpWebRequest webRequest, String method, System.IO.Stream context, long contentLength, Action<NetHttpResponse> prepare)
        {

            CheckHeader(webRequest);
            if (contentLength >= 0)
            {
                webRequest.ContentLength = contentLength;
            }
            webRequest.Method = method;

            Http(context, webRequest, prepare);

        }
        private NetHttpResponse(Action<NetHttpResponse> prepare)
        {
            this.Prepare = prepare;
        }

        public NameValueCollection Headers
        {
            get
            {
                return m_HttpResponseHeaders;
            }
        }
        NameValueCollection m_HttpResponseHeaders = new NameValueCollection();

        public string ContentEncoding
        {
            get
            {
                return this.m_HttpResponseHeaders["Content-Encoding"];
            }
        }


        HttpStatusCode m_StatusCode;
        public HttpStatusCode StatusCode
        {
            get
            {
                return this.m_StatusCode;
            }
        }
        String m_StatusDescription;
        public String StatusDescription
        {
            get
            {
                return this.m_StatusDescription;
            }
        }
        public string ContentType
        {
            get; private set;
        }


        protected override void Header(byte[] data, int offset, int size)
        {
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
                                this.m_ProtocolVersion = heaerValue.Substring(0, l);
                                heaerValue = heaerValue.Substring(l + 1);
                                var fhv = heaerValue.IndexOf(' ');
                                if (fhv > 0)
                                {

                                    this.m_StatusCode = UMC.Data.Utility.Parse(heaerValue.Substring(0, fhv), HttpStatusCode.Continue);
                                    this.m_StatusDescription = heaerValue.Substring(fhv + 1);

                                }
                                else
                                {
                                    this.m_StatusCode = UMC.Data.Utility.Parse(heaerValue, HttpStatusCode.Continue);
                                }
                            }
                            else
                            {
                                this.IsHttpFormatError = true;
                                this.ReceiveError(new FormatException("Http格式异常"));
                                return;
                            }
                        }
                        else
                        {
                            var vi = heaerValue.IndexOf(':');
                            var key = heaerValue.Substring(0, vi);
                            var value = heaerValue.Substring(vi + 2);
                            switch (key.ToLower())
                            {
                                case "set-cookie":
                                    if (webRequest.CookieContainer != null)
                                    {
                                        if (webRequest.CookieContainer is NetCookieContainer)
                                        {
                                            NetCookieContainer netCookieContainer = (NetCookieContainer)webRequest.CookieContainer;
                                            netCookieContainer.SetCookies(webRequest.RequestUri, value);
                                        }
                                        else
                                        {
                                            webRequest.CookieContainer.SetCookies(webRequest.RequestUri, value);
                                        }
                                    }

                                    break;
                                case "content-type":
                                    this.ContentType = value;
                                    break;
                                case "content-disposition":
                                    if (value.StartsWith("attachment", StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        var n = "filename";
                                        var fIndex = value.IndexOf(n, StringComparison.CurrentCultureIgnoreCase);
                                        if (fIndex > 0)
                                        {
                                            var filename = value.Substring(fIndex + n.Length + 1).Trim(' ', '=', '"', '\'');
                                            _AttachmentFile = Uri.UnescapeDataString(filename);
                                        }
                                    }
                                    break;
                            }
                            this.m_HttpResponseHeaders.Add(key, value);

                        }

                        start = index + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                this.IsHttpFormatError = true;
                this.ReceiveError(ex);
                return;
            }
            try
            {
                Prepare(this);
                Prepare = null;
            }
            catch (Exception ex)
            {
                Prepare = null;
                this.IsHttpFormatError = true;
                this.ReceiveError(ex);
            }
            if (this._isHead)
            {
                this.IsHttpFormatError = true;
            }
        }
        String _AttachmentFile;
        public string AttachmentFile => _AttachmentFile;
        Action<NetHttpResponse> Prepare;
        public bool IsReadBody
        {
            get;
            private set;
        }
        public override void Finish()
        {
            _readData?.Invoke(Array.Empty<byte>(), 0, 0);
            this.IsReadBody = true;
        }

        NetReadData _readData;
        ManualResetEvent bmre;
        protected override void Body(byte[] data, int offset, int size)
        {
            if (size > 0 && IsHttpFormatError == false)
            {
                if (_readData == null)
                {
                    if (bmre == null)
                    {
                        bmre = new ManualResetEvent(false);
                        bmre.WaitOne(100);
                    }
                    else
                    {
                        this.IsHttpFormatError = true;
                        this.Finish();
                        return;
                    }
                }

                _readData?.Invoke(data, offset, size);
            }
        }
        public void ReadAsStream(Action<System.IO.Stream> action, Action<Exception> error)
        {
            var ms = new TempStream();

            this.ReadAsData((b, i, c) =>
            {
                if (c == 0 && b.Length == 0)
                {
                    if (i == -1)
                    {
                        ms.Close();
                        ms.Dispose();
                        error(_error);
                    }
                    else
                    {
                        ms.Flush();
                        ms.Position = 0;
                        try
                        {
                            action(ms);
                        }
                        catch (Exception ex)
                        {
                            ms.Close();
                            ms.Dispose();
                            error(ex);
                        }
                    }
                }
                else
                {
                    ms.Write(b, i, c);
                }
            });


        }
        public Exception Error => _error;
        public void ReadAsStream(System.IO.Stream stream)
        {
            bool isTimeOut = true;
            ManualResetEvent mre = new ManualResetEvent(false);
            this.ReadAsData((b, i, c) =>
            {
                if (c == 0 && b.Length == 0)
                {
                    if (isTimeOut)
                    {
                        isTimeOut = false;
                        mre.Set();
                    }
                }
                else
                {
                    stream.Write(b, i, c);
                }
            });
            mre.WaitOne(60000);
            if (isTimeOut)
            {
                isTimeOut = false;
                this._error = new TimeoutException();
            }
            mre.Close();
            mre.Dispose();
            if (this._error != null)
            {
                throw _error;
            }

        }
        public void ReadAsData(Net.NetReadData readData)
        {

            if (this._isHead)
            {
                readData(Array.Empty<byte>(), 0, 0);
            }
            else
            {
                if (IsReadBody)
                {
                    readData(Array.Empty<byte>(), this.IsHttpFormatError ? -1 : 0, 0);
                }
                else
                {
                    this._readData = readData;
                }

                bmre?.Set();
            }
        }

        private bool disposedValue;
        public void Dispose()
        {
            if (!disposedValue)
            {
                bmre?.Dispose();
                disposedValue = true;
            }

            GC.SuppressFinalize(this);
        }
    }


}
