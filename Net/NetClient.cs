using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Net.Cache;
using UMC.Web;
using System.Text;

namespace UMC.Net
{

    public static class NetClient
    {
        static void Copy(System.IO.Stream d, System.IO.Stream t)
        {

            var buffer = new byte[1];
            try
            {
                while (d.Read(buffer, 0, 1) == 1)
                {
                    t.Write(buffer, 0, 1);
                }
            }
            catch
            { }
            t.Flush();
        }
        public static string GetIgnore(this System.Collections.Specialized.NameValueCollection nv, string Key)
        {
            for (var i = 0; i < nv.Count; i++)
            {
                if (String.Equals(nv.GetKey(i), Key, StringComparison.CurrentCultureIgnoreCase))
                {
                    return nv.Get(i);
                }
            }
            return null;
        }
        public static void ReadAsStream(this HttpWebResponse webResponse, System.IO.Stream writer)
        {
            // System.IO.Stream 
            var stream = webResponse.GetResponseStream();
            Copy(stream, writer);
            stream.Close();
            webResponse.Close();
        }
        public static String ReadAsString(this HttpWebResponse webResponse)
        {
            var str = new System.IO.StreamReader(Decompress(webResponse.GetResponseStream(), webResponse.ContentEncoding));
            var value = str.ReadToEnd();
            str.Close();
            webResponse.Close();
            return value;
        }
        public static String ReadAsString(this NetHttpResponse webResponse)
        {
            using (var ms = new TempStream())
            {
                webResponse.ReadAsStream(ms);
                ms.Flush();
                ms.Position = 0;
                var str = new System.IO.StreamReader(Decompress(ms, webResponse.ContentEncoding));
                var value = str.ReadToEnd();
                return value;
            }
        }
        public static byte[] ReadAsByteArray(this NetHttpResponse webResponse)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                webResponse.ReadAsStream(ms);
                return ms.ToArray(); ;
            }
        }
        public static byte[] ReadAsByteArray(this HttpWebResponse webResponse)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                var stream = webResponse.GetResponseStream();
                var t = Decompress(stream, webResponse.ContentEncoding);
                Copy(t, ms);
                stream.Close();
                t.Close();
                webResponse.Close();

                return ms.ToArray(); ;
            }
        }

        static Stream Decompress(Stream response, string encoding)
        {
            switch (encoding)
            {
                case "gzip":
                    return new GZipStream(response, CompressionMode.Decompress);
                case "deflate":
                    return new DeflateStream(response, CompressionMode.Decompress);
                default:
                    return response;
            }
        }
        public static HttpWebRequest Header(this HttpWebRequest webRequest, String header)
        {
            if (String.IsNullOrEmpty(header) == false)
            {
                var hs = header.Split('\n');
                foreach (var h in hs)
                {
                    var hi = h.IndexOf(":");
                    if (hi > 0)
                    {
                        webRequest.Headers[h.Substring(0, hi).Trim()] = h.Substring(hi + 1).Trim();
                    }
                }
            }
            return webRequest;
        }
        public static NetHttpResponse Get(this HttpWebRequest webRequest)
        {
            return NetHttpResponse.Create(webRequest, "GET");
        }
        public static NetHttpResponse Delete(this HttpWebRequest webRequest)
        {
            return NetHttpResponse.Create(webRequest, "DELETE");
        }
        public static NetHttpResponse Put(this HttpWebRequest webRequest, System.Net.Http.HttpContent context)
        {
            return NetHttpResponse.Create(webRequest, "PUT", context);
        }
        public static NetHttpResponse Put(this HttpWebRequest webRequest, string context)
        {
            return NetHttpResponse.Create(webRequest, "PUT", context);
        }
        public static NetHttpResponse Post(this HttpWebRequest webRequest, System.Net.Http.HttpContent context)
        {
            return NetHttpResponse.Create(webRequest, "POST", context);
        }

        public static void Get(this HttpWebRequest webRequest, Action<NetHttpResponse> prepare)
        {
            NetHttpResponse.Create(webRequest, "GET", prepare);
        }
        public static void Delete(this HttpWebRequest webRequest, Action<NetHttpResponse> prepare)
        {
            NetHttpResponse.Create(webRequest, "DELETE", prepare);
        }
        public static void Put(this HttpWebRequest webRequest, Object value, Action<NetHttpResponse> prepare)
        {
            webRequest.ContentType = "application/json; charset=UTF-8";
            NetHttpResponse.Create(webRequest, "PUT", UMC.Data.JSON.Serialize(value, "ts"), prepare);
        }
        public static void Put(this HttpWebRequest webRequest, System.Net.Http.HttpContent context, Action<NetHttpResponse> prepare)
        {
            NetHttpResponse.Create(webRequest, "PUT", context, prepare);
        }
        public static void Post(this HttpWebRequest webRequest, System.Net.Http.HttpContent context, Action<NetHttpResponse> prepare)
        {
            NetHttpResponse.Create(webRequest, "POST", context, prepare);
        }
        public static void Post(this HttpWebRequest webRequest, string context, Action<NetHttpResponse> prepare)
        {
            NetHttpResponse.Create(webRequest, "POST", context, prepare);
        }
        public static void Put(this HttpWebRequest webRequest, string context, Action<NetHttpResponse> prepare)
        {
            NetHttpResponse.Create(webRequest, "PUT", context, prepare);
        }
        public static void Post(this HttpWebRequest webRequest, Object value, Action<NetHttpResponse> prepare)
        {
            webRequest.ContentType = "application/json; charset=UTF-8";
            NetHttpResponse.Create(webRequest, "POST", UMC.Data.JSON.Serialize(value, "ts"), prepare);
        }
        public static NetHttpResponse Post(this HttpWebRequest webRequest, Object value)
        {
            webRequest.ContentType = "application/json; charset=UTF-8";
            return NetHttpResponse.Create(webRequest, "POST", UMC.Data.JSON.Serialize(value, "ts"));
        }

        public static NetHttpResponse Post(this HttpWebRequest webRequest, string value)
        {
            // webRequest.ContentType = "application/json; charset=UTF-8";
            return NetHttpResponse.Create(webRequest, "POST", value);
        }

        public static Task<HttpWebResponse> GetAsync(this HttpWebRequest webRequest)
        {
            return SendAsync(webRequest, "GET", null);
        }
        public static Task<HttpWebResponse> DeleteAsync(this HttpWebRequest webRequest)
        {
            return SendAsync(webRequest, "DELETE", null);
        }
        public static Task<HttpWebResponse> PutAsync(this HttpWebRequest webRequest, System.Net.Http.HttpContent context)
        {
            return SendAsync(webRequest, "PUT", context);
        }
        public static Task<HttpWebResponse> PostAsync(this HttpWebRequest webRequest, System.Net.Http.HttpContent context)
        {
            return SendAsync(webRequest, "POST", context);
        }
        public static Task<HttpWebResponse> SendAsync(this HttpWebRequest webRequest, String method, System.Net.Http.HttpContent context)
        {
            webRequest.Method = method;
            webRequest.ConnectionGroupName = webRequest.RequestUri.Host;
            webRequest.KeepAlive = true;
            if (context != null)
            {
                if (context.Headers != null)
                {
                    if (context.Headers.ContentType != null)
                    {
                        webRequest.ContentType = context.Headers.ContentType.ToString();
                    }
                    if (context.Headers.ContentLength.HasValue)
                    {
                        webRequest.ContentLength = context.Headers.ContentLength ?? 0;
                    }
                }
                var stream = webRequest.GetRequestStream();
                context.ReadAsStreamAsync().Result.CopyTo(stream, 1024);

                stream.Close();
            }
            Task<HttpWebResponse> webResponse = new Task<HttpWebResponse>(() =>
             {

                 HttpWebResponse response;
                 try
                 {
                     response = (HttpWebResponse)webRequest.GetResponse();
                 }
                 catch (WebException we)
                 {
                     response = (HttpWebResponse)we.Response;
                     if (response == null)
                     {
                         throw we;
                     }
                 }
                 return response;
             });
            return webResponse;
        }

        public static NetHttpResponse Net(this HttpWebRequest webRequest, String method, string context)
        {
            return NetHttpResponse.Create(webRequest, method.ToUpper(), context);
        }
        public static NetHttpResponse Net(this HttpWebRequest webRequest, String method, System.IO.Stream context, long contextLength)
        {
            return NetHttpResponse.Create(webRequest, method.ToUpper(), context, contextLength);
        }
        public static void ReadAsString(this NetHttpResponse webResponse, Action<String> action)
        {
            ReadAsString(webResponse, action, e => { });
        }
        public static void ReadAsString(this NetHttpResponse webResponse, Action<String> action, Action<Exception> error)
        {
            var ms = new TempStream();


            webResponse.ReadAsData((b, i, c) =>
            {
                if (c == 0 && b.Length == 0)
                {
                    if (i == -1)
                    {
                        ms.Close();
                        ms.Dispose();
                        error(webResponse.Error);
                    }
                    else
                    {
                        ms.Flush();
                        ms.Position = 0;
                        try
                        {
                            var str = new System.IO.StreamReader(Decompress(ms, webResponse.ContentEncoding));
                            var value = str.ReadToEnd();
                            action(value);
                        }
                        catch (Exception ex)
                        {
                            error(ex);
                        }
                        finally
                        {
                            ms.Close();
                            ms.Dispose();
                        }
                    }
                }
                else
                {
                    ms.Write(b, i, c);
                }
            });

        }
        public static void ReadAsStream(this Net.NetContext context, Action<System.IO.Stream> action)
        {
            ReadAsStream(context, action, context.Error);
        }
        public static void ReadAsStream(this Net.NetContext context, Action<System.IO.Stream> action, Action<Exception> error)
        {
            var ms = new TempStream();

            context.ReadAsData((b, i, c) =>
            {
                if (c == 0 && b.Length == 0)
                {
                    if (i == -1)
                    {
                        ms.Close();
                        ms.Dispose();
                        error(new WebException("接收Body错误"));
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
        public static void Net(this HttpWebRequest webRequest, String method, System.Net.Http.HttpContent context, Action<NetHttpResponse> prepare)
        {
            NetHttpResponse.Create(webRequest, method.ToUpper(), context, prepare);
        }
        public static void Net(this HttpWebRequest webRequest, String method, System.IO.Stream context, long contextLength, Action<NetHttpResponse> prepare)
        {
            NetHttpResponse.Create(webRequest, method.ToUpper(), context, contextLength, prepare);
        }
        public static int WriteBytes(this string str, byte[] bytes, int index)
        {
            return System.Text.Encoding.UTF8.GetBytes(str, 0, str.Length, bytes, index);
        }
        public static Stream TempStream()
        {
            return new TempStream();
        }
        public static void Net(this HttpWebRequest webRequest, Net.NetContext context, Action<NetHttpResponse> prepare)
        {
            webRequest.Method = context.HttpMethod;
            switch (webRequest.Method)
            {
                case "GET":
                case "DELETE":
                    NetHttpResponse.Create(webRequest, webRequest.Method, prepare);
                    break;
                default:
                    webRequest.ContentType = context.ContentType;
                    NetHttpResponse.Create(webRequest, context, prepare);
                    break;
            }
        }
        public static HttpWebResponse Send(this HttpWebRequest webRequest, String method, System.Net.Http.HttpContent context)
        {
            webRequest.ConnectionGroupName = webRequest.RequestUri.Host;
            webRequest.KeepAlive = true;
            webRequest.Method = method;
            if (context != null)
            {
                if (context.Headers != null)
                {
                    if (context.Headers.ContentType != null)
                    {
                        webRequest.ContentType = context.Headers.ContentType.ToString();
                    }
                    if (context.Headers.ContentLength.HasValue)
                    {
                        webRequest.ContentLength = context.Headers.ContentLength ?? 0;
                    }
                }
                var stream = webRequest.GetRequestStream();

                Copy(context.ReadAsStreamAsync().Result, stream);
                stream.Close();
            }
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)webRequest.GetResponse();
            }
            catch (WebException we)
            {
                response = (HttpWebResponse)we.Response;
                if (response == null)
                {
                    throw we;
                }
            }
            return response;
        }
        // public static void Header(this NetHttpResponse webResponse, Net.NetContext context)
        // {
        //     Header(webResponse, context, true);
        // }
        public static void Header(this NetHttpResponse webResponse, Net.NetContext context)
        {
            context.StatusCode = Convert.ToInt32(webResponse.StatusCode);

            var ContentType = webResponse.ContentType;

            if (String.IsNullOrEmpty(ContentType) == false)
            {
                context.ContentType = ContentType;
            }
            var headers = webResponse.Headers;
            for (var i = 0; i < headers.Count; i++)
            {
                var key = headers.GetKey(i);

                switch (key.ToLower())
                {
                    case "content-type":
                    case "server":
                    case "connection":
                        break;
                    case "transfer-encoding":
                    case "content-length":
                        if (webResponse.IsHead)
                        {
                            context.AddHeader(key, headers.Get(i));
                        }
                        break;
                    case "set-cookie":
                        var values = headers.GetValues(i);
                        foreach (var value in values)
                        {
                            var vs = new List<String>(value.Split(';'));
                            for (var c = 0; c < vs.Count; c++)
                            {
                                var v = vs[c].TrimStart();
                                if (String.IsNullOrEmpty(v) || String.Equals("secure", v, StringComparison.CurrentCultureIgnoreCase) || v.StartsWith("domain=", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    vs.RemoveAt(c);
                                    c--;
                                }

                            }

                            context.AddHeader(key, String.Join(";", vs.ToArray()));
                        }
                        break;

                    default:
                        context.AddHeader(key, headers.Get(i));
                        break;
                }
            }
        }
        public static void Transfer(this NetHttpResponse webResponse, Net.NetContext context)
        {

            Header(webResponse, context);

            if (webResponse.IsHead == false)
            {
                if (webResponse.ContentLength > 0)
                {
                    context.ContentLength = webResponse.ContentLength;
                }
            }

            if (context.AllowSynchronousIO)
            {
                webResponse.ReadAsData((b, i, c) =>
                {
                    if (c == 0 && b.Length == 0)
                    {
                        if (i == -1)
                        {
                            context.Error(webResponse.Error);
                        }
                        else
                        {
                            context.OutputFinish();
                        }
                    }
                    else
                    {
                        context.OutputStream.Write(b, i, c);
                    }
                });
            }
            else
            {
                webResponse.ReadAsStream(context.OutputStream);
            }
        }
        public static void Transfer(this HttpWebResponse webResponse, Net.NetContext context)
        {

            context.StatusCode = Convert.ToInt32(webResponse.StatusCode);
            if (webResponse.ContentLength > 0)
            {
                context.ContentLength = webResponse.ContentLength;
            }
            var ContentType = webResponse.ContentType;
            var transferencoding = false;
            if (String.IsNullOrEmpty(ContentType) == false)
            {
                context.ContentType = ContentType;
            }
            var headers = webResponse.Headers;
            for (var i = 0; i < headers.Count; i++)
            {
                var key = headers.GetKey(i);

                switch (key.ToLower())
                {
                    case "content-type":
                    case "content-length":
                    case "server":
                    case "connection":
                        break;
                    case "set-cookie":
                        var values = headers.GetValues(i);
                        foreach (var v in values)
                        {
                            context.AddHeader(key, v);
                        }
                        break;
                    case "transfer-encoding":
                        transferencoding = true;
                        break;

                    default:
                        context.AddHeader(key, headers.Get(i));
                        break;
                }
            }
            if (webResponse.ContentLength > 0 || transferencoding)
            {
                webResponse.ReadAsStream(context.OutputStream);
            }

        }
        public static HttpWebRequest WebRequest(this Uri url, CookieContainer cookieContainer = null)
        {
            var webRequest = HttpWebRequest.CreateHttp(url);
            webRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            webRequest.AllowAutoRedirect = false;
            webRequest.CookieContainer = cookieContainer;
            webRequest.KeepAlive = true;
            webRequest.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            webRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/36.0.1985.143 Safari/537.36";
            return webRequest;
        }

        public static bool OutputCache(this NetContext context, Stream mime)
        {
            String match = context.Headers["If-None-Match"];

            var data = System.Buffers.ArrayPool<byte>.Shared.Rent(200);
            try
            {
                var size = mime.Read(data, 0, data.Length);

                var eTag = String.Empty;
                var end = UMC.Data.Utility.FindIndexIgnoreCase(data, 0, size, HttpMimeBody.HeaderEnd);
                if (end > 0)
                {
                    var isText = false;
                    var isOptimal = false;
                    var ss = System.Text.Encoding.UTF8.GetString(data, 0, end).Split(new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);// ';
                    foreach (var heaerValue in ss)
                    {

                        var vi = heaerValue.IndexOf(':');
                        var key = heaerValue.Substring(0, vi);
                        var value = heaerValue.Substring(vi + 1);
                        switch (key)
                        {
                            case "ETag":
                                if (String.Equals(match, value))
                                {
                                    context.StatusCode = 304;
                                    return true;
                                }
                                else
                                {
                                    eTag = value;
                                    context.AddHeader("ETag", value);
                                }
                                break;
                            case "ContentType":
                                context.ContentType = value;
                                isText = value.Contains("text");
                                isOptimal = value.Contains("image/Optimal") || size == mime.Length;
                                break;
                            case "Content-Encoding":
                                context.AddHeader("Content-Encoding", value);
                                isText = false;
                                break;
                        }
                    }

                    mime.Seek(end + 4, SeekOrigin.Begin);

                    if (isText)
                    {
                        var ac = context.Headers["Accept-Encoding"];
                        if (String.IsNullOrEmpty(ac))
                        {
                            context.ContentLength = mime.Length - end - 4;

                            mime.CopyTo(context.OutputStream);
                        }
                        else
                        {
                            if (mime is FileStream)
                            {
                                var fStream = mime as FileStream;
                                var nfile = fStream.Name;
                                if (ac.Contains("br"))
                                {
                                    context.AddHeader("Content-Encoding", "br");

                                    var file = $"{nfile}.{eTag}.br";
                                    if (!File.Exists(file))
                                    {
                                        using (var ffU = File.Create(file))
                                        {
                                            var tem = new BrotliStream(ffU, CompressionMode.Compress);
                                            mime.CopyTo(tem);
                                            tem.Flush();
                                            tem.Close();
                                        }

                                    }
                                    using (var fU = File.OpenRead(file))
                                    {
                                        context.ContentLength = fU.Length;
                                        fU.CopyTo(context.OutputStream);
                                    }
                                }
                                else if (ac.Contains("gzip"))
                                {
                                    context.AddHeader("Content-Encoding", "gzip");
                                    var file = $"{nfile}.{eTag}.gz";
                                    if (!File.Exists(file))
                                    {
                                        using (var ffU = File.Create(file))
                                        {
                                            var tem = new GZipStream(ffU, CompressionMode.Compress);
                                            mime.CopyTo(tem);
                                            tem.Flush();
                                            tem.Close();
                                        }

                                    }
                                    using (var fU = File.OpenRead(file))
                                    {
                                        context.ContentLength = fU.Length;
                                        fU.CopyTo(context.OutputStream);
                                    }

                                }
                                else if (ac.Contains("deflate"))
                                {
                                    context.AddHeader("Content-Encoding", "deflate");
                                    var file = $"{nfile}.{eTag}.df";
                                    if (!File.Exists(file))
                                    {
                                        using (var ffU = File.Create(file))
                                        {
                                            var tem = new DeflateStream(ffU, CompressionMode.Compress);
                                            mime.CopyTo(tem);
                                            tem.Flush();
                                            tem.Close();
                                        }

                                    }
                                    using (var fU = File.OpenRead(file))
                                    {
                                        context.ContentLength = fU.Length;
                                        fU.CopyTo(context.OutputStream);
                                    }

                                }
                                else
                                {
                                    context.ContentLength = mime.Length - end - 4;
                                    mime.CopyTo(context.OutputStream);
                                }
                            }
                            else
                            {
                                context.ContentLength = mime.Length - end - 4;
                                mime.CopyTo(context.OutputStream);
                            }
                        }

                    }
                    else if (isOptimal)
                    {
                        if (mime is FileStream)
                        {
                            var fStream = mime as FileStream;
                            var nfile = fStream.Name;
                            var v = context.Headers["Accept"];
                            if (String.IsNullOrEmpty(v) == false)
                            {
                                if (v.Contains("image/avif"))
                                {
                                    var file = $"{nfile}.avif";
                                    if (File.Exists(file))
                                    {
                                        context.ContentType = "image/avif";
                                        using (var fU = File.OpenRead(file))
                                        {
                                            context.ContentLength = fU.Length;
                                            fU.CopyTo(context.OutputStream);
                                        }
                                        return true;
                                    }


                                }
                                if (v.Contains("image/webp"))
                                {
                                    var file = $"{nfile}.webp";
                                    if (File.Exists(file))
                                    {

                                        context.ContentType = "image/webp";

                                        using (var fU = File.OpenRead(file))
                                        {
                                            context.ContentLength = fU.Length;
                                            fU.CopyTo(context.OutputStream);
                                        }
                                        return true;
                                    }
                                }

                            }
                            var filePng = $"{nfile}.png";
                            if (File.Exists(filePng))
                            {
                                context.ContentType = "image/png";

                                using (var fU = File.OpenRead(filePng))
                                {
                                    context.ContentLength = fU.Length;
                                    fU.CopyTo(context.OutputStream);
                                }
                                return true;
                            }
                            return false;

                        }

                        context.ContentLength = mime.Length - end - 4;
                        mime.CopyTo(context.OutputStream);
                    }
                    else
                    {

                        context.ContentLength = mime.Length - end - 4;
                        mime.CopyTo(context.OutputStream);
                    }
                    return true;
                }

            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(data);
                mime.Close();
                mime.Dispose();
            }

            return false;
        }
        public static bool CheckCache(this NetContext context, string root, String vs, out String cacheKey)
        {
            var version = vs ?? String.Empty;
            var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();

            var bytes = System.Buffers.ArrayPool<byte>.Shared.Rent(System.Text.Encoding.UTF8.GetByteCount(context.RawUrl) + System.Text.Encoding.UTF8.GetByteCount(version));
            try
            {
                if (context.RawUrl.EndsWith("&umc=src") || context.RawUrl.EndsWith("?umc=src"))
                {
                    var i = System.Text.Encoding.UTF8.GetBytes(context.RawUrl, 0, context.RawUrl.Length - 8, bytes, 0);
                    i += System.Text.Encoding.UTF8.GetBytes(version, 0, version.Length, bytes, i);
                    var md = new Guid(md5.ComputeHash(bytes, 0, i));
                    cacheKey = UMC.Data.Reflection.ConfigPath(String.Format("Cache/{0}/{1}.che", root, md));

                    return false;
                }
                else
                {
                    var i = System.Text.Encoding.UTF8.GetBytes(context.RawUrl, 0, context.RawUrl.Length, bytes, 0);
                    i += System.Text.Encoding.UTF8.GetBytes(version, 0, version.Length, bytes, i);
                    var md = new Guid(md5.ComputeHash(bytes, 0, i));
                    cacheKey = UMC.Data.Reflection.ConfigPath(String.Format("Cache/{0}/{1}.che", root, md));

                    if (System.IO.File.Exists(cacheKey))
                    {
                        using (var stream = File.OpenRead(cacheKey))
                        {
                            return OutputCache(context, stream);
                        }
                    }
                    return false;
                }
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(bytes);

            }
        }

        public static bool TryImageConf(string confKey, out WebMeta confValue)
        {
            confValue = null;
            if (String.IsNullOrEmpty(confKey) == false)
            {

                var match = System.Text.RegularExpressions.Regex.Match(confKey, "^[h|w|c|t|m|b](\\d+)([-|x]\\d+)?([g|p|j|w|a|o]?)$");

                if (match.Success)
                {
                    confValue = new WebMeta();
                    switch (match.Groups[3].Value)
                    {
                        case "g":
                            confValue.Put("Format", "gif");
                            break;
                        case "j":
                            confValue.Put("Format", "jpeg");
                            break;
                        case "w":
                            confValue.Put("Format", "webp");
                            break;
                        case "p":
                            confValue.Put("Format", "png");
                            break;
                        case "a":
                            confValue.Put("Format", "avif");
                            break;
                        case "o":
                            confValue.Put("Format", "Optimal");
                            break;

                    }
                    confValue.Put("Width", match.Groups[1].Value);
                    if (match.Groups[2].Length > 0)
                    {
                        switch (match.Groups[0].Value[0])
                        {
                            case '-':
                                confValue.Put("Width", "-" + match.Groups[1].Value);
                                confValue.Put("Height", "-" + match.Groups[0].Value);
                                break;
                            default:
                                confValue.Put("Height", match.Groups[2].Value.Substring(1));
                                break;
                        }
                    }
                    else
                    {

                        confValue.Put("Height", match.Groups[1].Value);
                    }
                    switch (confKey[0])
                    {
                        case 'w':
                            confValue.Remove("Height");
                            break;
                        case 'h':
                            confValue.Remove("Width");
                            break;
                        case 'c':
                            confValue["Model"] = "0";
                            break;
                        case 't':
                            confValue["Model"] = "1";
                            break;
                        case 'm':
                            confValue["Model"] = "2";
                            break;
                        case 'b':
                            confValue["Model"] = "3";
                            break;
                    }
                    return true;
                }
                else
                {
                    switch (confKey)
                    {
                        case "g":
                            confValue = new WebMeta().Put("Format", "gif");
                            return true;
                        case "j":
                            confValue = new WebMeta().Put("Format", "jpeg");
                            return true;
                        case "w":
                            confValue = new WebMeta().Put("Format", "webp");
                            return true;
                        case "p":
                            confValue = new WebMeta().Put("Format", "png");
                            return true;

                    }
                }
            }
            return false;

        }

        public static System.IO.Stream MimeStream(string file, String contentType, int tag)
        {
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(file)))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));
            }

            System.IO.Stream sWriter = File.Open(file, FileMode.Create, FileAccess.ReadWrite);
            if (String.IsNullOrEmpty(contentType))
            {
                return sWriter;
            }
            var data = System.Buffers.ArrayPool<byte>.Shared.Rent(100);
            try
            {
                var idex = $"ContentType:{contentType}\r\nETag:{tag}\r\n\r\n".WriteBytes(data, 0);
                sWriter.Write(data, 0, idex);
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(data);
            }


            return sWriter;

        }

        public static HttpWebRequest Transfer(this Net.NetContext context, Uri url, CookieContainer cookieContainer = null)
        {

            var webRequest = HttpWebRequest.CreateHttp(url);
            webRequest.AllowAutoRedirect = false;
            webRequest.CookieContainer = cookieContainer;
            webRequest.KeepAlive = true;

            webRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);

            webRequest.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            webRequest.Headers[HttpRequestHeader.Host] = webRequest.Host;


            var Headers = context.Headers;
            //var language = "zh-CN,zh;q=0.9";
            for (var i = 0; i < Headers.Count; i++)
            {
                var k = Headers.GetKey(i);
                var v = Headers.Get(i);
                switch (k.ToLower())
                {
                    //case "accept-language":
                    //    language = v;
                    //    break;
                    //case "accept-encoding":
                    //    webRequest.Headers.Add("Accept-Encoding", "gzip, deflate");
                    //    break;
                    case "content-type":
                        webRequest.ContentType = v;
                        break;
                    case "content-length":
                    case "connection":
                    case "cookie":
                    case "host":
                        break;
                    case "user-agent":
                        webRequest.UserAgent = v;
                        break;
                    default:
                        webRequest.Headers.Add(k, v);
                        break;
                }
            }
            //webRequest.Headers.Add("Accept-Language", language);

            return webRequest;

        }
    }

}


