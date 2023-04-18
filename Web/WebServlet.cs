using System;
using System.Net;
using System.Collections.Specialized;
using System.Collections.Generic;
using UMC.Data;
using UMC.Net;
using System.IO;
using System.Buffers;

namespace UMC.Web
{
    /// <summary>
    /// WebServlet处理
    /// </summary>
    public class WebServlet : UMC.Net.INetHandler
    {
        protected void Temp(UMC.Net.NetContext context)
        {
            var file = context.Url.LocalPath.Substring(context.Url.LocalPath.IndexOf('/', 2) + 1);
            file = UMC.Data.Reflection.ConfigPath(String.Format("Static\\TEMP\\{0}", file));
            switch (context.HttpMethod)
            {
                case "GET":
                    File(context, file);
                    break;
                case "PUT":
                    if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(file)))
                    {
                        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));
                    }
                    if (System.IO.File.Exists(file))
                    {
                        System.IO.File.Delete(file);
                    }

                    System.IO.Stream sWriter = System.IO.File.Open(file, FileMode.Create);
                    context.ReadAsData((b, i, c) =>
                    {
                        if (c == 0 && b.Length == 0)
                        {
                            if (i == -1)
                            {
                                sWriter.Close();
                                System.IO.File.Delete(file);

                            }
                            else
                            {
                                sWriter.Close();
                                sWriter.Dispose();
                            }
                            context.OutputFinish();
                        }
                        else
                        {
                            sWriter.Write(b, i, c);
                        }
                    });
                    break;
            }
        }
        void File(UMC.Net.NetContext context, String name)
        {
            if (System.IO.File.Exists(name))
            {
                TransmitFile(context, name, true);
            }
            else
            {
                context.StatusCode = 404;
            }
        }
        protected virtual void TransmitFile(UMC.Net.NetContext context, String file, bool isCache)
        {
            var lastIndex = file.LastIndexOf('.');

            var extName = "html";
            if (lastIndex > -1)
            {
                extName = file.Substring(lastIndex + 1);
            }
            switch (extName.ToLower())
            {
                case "pdf":
                    context.ContentType = "application/pdf";
                    break;
                case "log":
                    context.ContentType = "text/plain; charset=UTF-8";
                    break;
                case "txt":
                    context.ContentType = "text/plain";
                    break;
                case "htm":
                case "html":
                    context.ContentType = "text/html";
                    break;
                case "json":
                    context.ContentType = "text/json";
                    break;
                case "js":
                    context.ContentType = "text/javascript";
                    break;
                case "css":
                    context.ContentType = "text/css";
                    break;
                case "bmp":
                    context.ContentType = "image/bmp";
                    break;
                case "gif":
                    context.ContentType = "image/gif";
                    break;
                case "webp":
                    context.ContentType = "image/webp";
                    break;
                case "jpeg":
                case "jpg":
                    context.ContentType = "image/jpeg";
                    break;
                case "png":
                    context.ContentType = "image/png";
                    break;
                case "ico":
                    context.ContentType = "image/x-icon";
                    break;
                case "svg":
                    context.ContentType = "image/svg+xml";
                    break;
                case "mp3":
                    context.ContentType = "audio/mpeg";
                    break;
                case "mp4":
                    context.ContentType = "video/mpeg4";
                    break;
                case "xml":
                    context.ContentType = "text/xml";
                    break;
                default:
                    context.ContentType = "application/octet-stream";
                    break;
            }
            var fileInfo = new System.IO.FileInfo(file);
            var Modified = fileInfo.LastWriteTimeUtc.ToString("r");
            context.AddHeader("Last-Modified", Modified);
            if (isCache)
            {
                var Since = context.Headers["If-Modified-Since"];
                if (String.IsNullOrEmpty(Since) == false)
                {

                    if (String.Equals(Modified, Since))
                    {
                        context.StatusCode = 304;
                        return;

                    }
                }
            }
            var range = context.Headers["Range"];

            if (String.IsNullOrEmpty(range) == false)
            {
                var IfSince = context.Headers["If-Range"];
                if (String.IsNullOrEmpty(IfSince) == false)
                {
                    if (String.Equals(IfSince, Modified) == false)
                    {
                        range = String.Empty;
                    }
                }
            }
            using (System.IO.FileStream stream = System.IO.File.OpenRead(file))
            {
                byte[] array = ArrayPool<byte>.Shared.Rent(1024);
                try
                {
                    if (String.IsNullOrEmpty(range) == false && range.StartsWith("bytes=") && context.StatusCode == 200)
                    {
                        var rg = range.Substring(6).Trim().Split(',')[0];

                        int start = 0, len = -1;
                        if (rg.StartsWith("-"))
                        {
                            start = UMC.Data.Utility.IntParse(rg, 0);
                            len = Math.Abs(start);
                        }
                        else if (rg.EndsWith("-"))
                        {
                            start = UMC.Data.Utility.IntParse(rg.Substring(0, rg.Length - 1), 0);
                        }
                        else
                        {
                            var rs = rg.Split('-');
                            start = UMC.Data.Utility.IntParse(rs[0], 0);
                            len = UMC.Data.Utility.IntParse(rs[1], 0) - start + 1;
                        }
                        if (start < 0)
                        {
                            start = (int)stream.Seek(start, SeekOrigin.End);
                        }
                        else if (start < stream.Length)
                        {
                            stream.Seek(start, SeekOrigin.Begin);

                        }
                        var endLen = (int)(stream.Length - start);
                        if (len == -1 || len > endLen)
                        {
                            len = endLen;
                        }
                        if (len > 0)
                        {
                            context.StatusCode = 206;
                            context.AddHeader("Content-Range", $"bytes {start}-{start + len - 1}/{stream.Length}");

                            context.ContentLength = len;
                            int count2 = 0;


                            while (len > 0)
                            {
                                count2 = stream.Read(array, 0, array.Length);

                                if (count2 == 0)
                                {
                                    break;
                                }
                                else if (count2 > len)
                                {
                                    context.OutputStream.Write(array, 0, len);
                                    break;
                                }
                                else
                                {
                                    context.OutputStream.Write(array, 0, count2);
                                }
                                len -= count2;
                            }
                        }
                        else
                        {
                            context.ContentType = " text/plain";
                            context.StatusCode = 416;

                            context.Output.Write($"416 {UMC.Net.HttpStatusDescription.Get(416)}");
                        }

                    }
                    else
                    {
                        context.AddHeader("Accept-Ranges", "bytes");

                        context.ContentLength = stream.Length;
                        int count;
                        while ((count = stream.Read(array, 0, array.Length)) != 0)
                        {
                            context.OutputStream.Write(array, 0, count);
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(array);
                }

            }

        }

        protected void Process(UMC.Net.NetContext context)
        {
            if (String.IsNullOrEmpty(context.UserAgent))
            {
                return;
            }
            context.AddHeader("Access-Control-Allow-Origin", "*");
            context.AddHeader("Access-Control-Allow-Credentials", "true");
            context.AddHeader("Cache-Control", "no-cache");
            context.UseSynchronousIO(() => { });
            var QueryString = new NameValueCollection(context.QueryString);
            context.ReadAsForm(r =>
            {
                QueryString.Add(r);
                var model = QueryString["_model"];
                var cmd = QueryString["_cmd"];
                Process(QueryString, context.Url, context, model, cmd);
            });
        }


        public const string SessionCookieName = "device";

        protected virtual UMC.Security.AccessToken AccessToken(UMC.Net.NetContext context)
        {

            var ns = new NameValueCollection();
            var sign = String.Empty;
            var hs = context.Headers;
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
            if (ns.Count > 0)
            {
                Data.Provider provider = Reflection.Configuration("assembly")["WebResource"] ?? Data.Provider.Create("WebResource", "UMC.Data.WebResource");

                String authSecret = provider["authSecret"];
                if (String.IsNullOrEmpty(authSecret) == false)
                {
                    if (String.Equals(Data.Utility.Sign(ns, authSecret), sign, StringComparison.CurrentCultureIgnoreCase) == false)
                    {
                        return new Security.AccessToken(Guid.Empty).Login(new UMC.Security.Guest(Guid.Empty), 0);
                    }

                }
                var organizes = ns["umc-request-user-organizes"];
                var roles = ns["umc-request-user-role"];
                var id = ns["umc-request-user-id"];
                var name = ns["umc-request-user-name"];
                var alias = ns["umc-request-user-alias"];
                var user = UMC.Security.Identity.Create(Utility.Guid(id) ?? Utility.Guid(name, true).Value, name, alias
                    , String.IsNullOrEmpty(roles) ? new String[0] : roles.Split(',')
                    , String.IsNullOrEmpty(organizes) ? new String[0] : organizes.Split(','));

                var token = new UMC.Security.AccessToken(new Guid(UMC.Data.Utility.MD5("umc.request", id, name, roles, organizes, alias)));
                token.Login(user, 0);
                return token;

            }

            return new Security.AccessToken(Guid.Empty).Login(new UMC.Security.Guest(Guid.Empty), 0);
        }
        void Process(NameValueCollection nvs, Uri Url, Net.NetContext context, string model, string cmd)
        {
            NameValueCollection queryString = new NameValueCollection();

            var paths = new System.Collections.Generic.List<string>(Url.AbsolutePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
            if (paths.Count > 0)
            {
                paths.RemoveAt(0);
            }
            if (paths.Count == 1)
            {
                context.Cookies[SessionCookieName] = paths[0];
            }
            else if (String.IsNullOrEmpty(model))
            {
                if (paths.Count > 1)
                {
                    model = paths[0];
                    cmd = paths[1];
                }
                if (paths.Count > 2)
                {
                    paths.RemoveAt(0);
                    paths.RemoveAt(0);
                    queryString.Add(null, String.Join("/", paths));

                }
            }
            for (var i = 0; i < nvs.Count; i++)
            {
                var key = System.Web.HttpUtility.UrlDecode(nvs.GetKey(i));
                var value = nvs.Get(i);
                if (String.IsNullOrEmpty(key))
                {
                    if (String.IsNullOrEmpty(value) == false)
                    {
                        queryString.Add(null, value);
                    }
                }
                else if (String.Equals(key, "_"))
                {
                    queryString.Add(key, value);
                }
                else if (!key.StartsWith("_"))
                {
                    queryString.Add(key, value);
                }
            }

            if (String.IsNullOrEmpty(model))
            {
                context.StatusCode = 404;
                context.ContentType = "text/plain";
                context.Output.Write("Model is empty");
                context.OutputFinish();
            }
            else if (String.IsNullOrEmpty(cmd))
            {
                if (model.StartsWith("[") && model.EndsWith("]"))
                {

                    context.Token = this.AccessToken(context);

                    var client = new WebClient(context);

                    client._jsonp = queryString.Get("jsonp");
                    queryString.Remove("jsonp");

                    client.Command(model);
                }
                else
                {
                    context.StatusCode = 404;
                    context.ContentType = "text/plain";
                    context.Output.Write("Command is empty");
                    context.OutputFinish();
                }

            }
            else
            {

                context.Token = this.AccessToken(context);

                var client = new WebClient(context);

                client._jsonp = queryString.Get("jsonp");
                queryString.Remove("jsonp");

                client.Command(model, cmd, queryString);
            }

        }

        async void LocalResources(NetContext context, String path, bool check)
        {
            var file = Reflection.ConfigPath($"Static{path}");
            if (check)
            {
                if (System.IO.File.Exists(file))
                {
                    TransmitFile(context, file, true);
                    if (context.AllowSynchronousIO)
                    {
                        context.OutputFinish();
                    }
                    return;
                }
            }
            var url = new Uri($"https://res.apiumc.com{path}?v{UMC.Data.Utility.TimeSpan()}");
            if (context.AllowSynchronousIO == false)
            {
                context.UseSynchronousIO(() => { });
            }
            System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
            try
            {

                var res = await httpClient.GetAsync(url);
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    using (var stream = UMC.Data.Utility.Writer(file, false))
                    {
                        await res.Content.CopyToAsync(stream);
                        stream.Close();
                    }
                    TransmitFile(context, file, true);
                    context.OutputFinish();
                }
                else
                {
                    context.StatusCode = 404;

                    context.OutputFinish();
                }
            }
            catch (Exception ex)
            {

                context.StatusCode = 500;
                context.ContentType = "text/plain";
                context.Output.Write(ex.ToString());
                context.OutputFinish();
            }
        }
        #region IMIMEHandler Members

        public virtual void ProcessRequest(UMC.Net.NetContext context)
        {

            var paths = new List<string>(context.Url.LocalPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
            if (paths.Count == 0)
            {
                paths.Add("index.html");
            }


            switch (paths[0])
            {
                case "TEMP":
                    Temp(context);
                    return;
                case "UMC":
                    var query = context.Url.Query;
                    if (query.Contains("?_model=") || query.Contains("&_model="))
                    {
                        Process(context);
                        return;
                    }
                    else
                    {
                        switch (context.HttpMethod)
                        {
                            case "GET":
                                switch (paths.Count)
                                {
                                    case 2:
                                    case 1:
                                        IndexResource(context);
                                        break;
                                    case 3:
                                        if (context.Url.LocalPath.EndsWith("/"))
                                        {
                                            PageResource(context);
                                        }
                                        else
                                        {

                                            IndexResource(context);
                                        }
                                        break;
                                    default:
                                        if (context.Url.LocalPath.EndsWith("/"))
                                        {

                                            PageResource(context);
                                        }
                                        else
                                        {

                                            Process(context);
                                        }
                                        break;
                                }
                                break;
                            case "OPTIONS":

                                context.AddHeader("Access-Control-Allow-Origin", "*");
                                context.AddHeader("Access-Control-Allow-Credentials", "true");
                                context.AddHeader("Cache-Control", "no-cache");
                                context.AddHeader("Access-Control-Allow-Headers", "Referer-Path");
                                break;
                            default:
                                Process(context);
                                break;
                        }
                        return;
                    }
                case "UMC.UI":
                    LocalResources(context, "/" + context.Url.LocalPath.Substring(5), true);
                    return;
                case "css":
                case "js":
                    LocalResources(context, context.Url.LocalPath, true);
                    return; ;
                default:
                    StaticFile(Reflection.ConfigPath("Static"), context);
                    break;

            }



        }
        protected static String FilePath(String path)
        {
            var sb = new System.Text.StringBuilder();
            char last = char.MinValue;
            foreach (var c in path)
            {
                switch (c)
                {
                    case '/':
                    case '\\':
                        if (last != System.IO.Path.DirectorySeparatorChar)
                        {
                            last = System.IO.Path.DirectorySeparatorChar;
                            sb.Append(System.IO.Path.DirectorySeparatorChar);
                        }
                        break;
                    default:
                        sb.Append(c);
                        last = c;
                        break;
                }
            }
            return sb.ToString();
        }
        protected void StaticFile(string dir, Net.NetContext context)
        {

            var path = context.Url.AbsolutePath;
            var file = FilePath(dir + path);

            try
            {
                if (System.IO.File.Exists(file))
                {
                    TransmitFile(context, file, true);
                    return;
                }
                if (path.IndexOf('.', path.LastIndexOf('/')) == -1)
                {
                    var staticFile = FilePath(file + "/index.html");

                    if (System.IO.File.Exists(staticFile))
                    {
                        TransmitFile(context, staticFile, true);
                        return;

                    }
                }
                var lastIndex = file.LastIndexOf('.');
                var extName = "html";
                if (lastIndex > -1)
                {
                    extName = file.Substring(lastIndex + 1);
                }
                NotFound(context, extName, dir);

            }
            finally
            {

                if (context.AllowSynchronousIO)
                {
                    context.OutputFinish();
                }
            }
        }
        protected void NotFound(Net.NetContext context, string extName, string dir)
        {


            var fok404 = FilePath($"{dir}/404.OK.{extName}");
            if (System.IO.File.Exists(fok404))
            {
                context.StatusCode = 200;
                TransmitFile(context, fok404, false);
                return;
            }
            var f404 = FilePath($"{dir}/404.{extName}");
            context.StatusCode = 404;
            if (System.IO.File.Exists(f404))
            {
                TransmitFile(context, f404, false);
            }
            else
            {
                context.StatusCode = 404;
                switch (extName)
                {
                    case "html":
                        break;
                    default:
                        f404 = FilePath($"{dir}/404.html");
                        if (System.IO.File.Exists(f404))
                        {
                            TransmitFile(context, f404, false);
                        }
                        break;
                }
            }

        }
        protected virtual void PageResource(Net.NetContext context)
        {
            context.ContentType = "text/html";


            using (System.IO.Stream stream = typeof(WebServlet).Assembly
                               .GetManifestResourceStream("UMC.Resources.page.html"))
            {
                context.ContentLength = stream.Length;
                stream.CopyTo(context.OutputStream);

            }
        }
        protected virtual void IndexResource(Net.NetContext context)
        {
            context.ContentType = "text/html";


            using (System.IO.Stream stream = typeof(WebServlet).Assembly
                               .GetManifestResourceStream("UMC.Resources.umc.html"))
            {
                context.ContentLength = stream.Length;
                stream.CopyTo(context.OutputStream);

            }
        }

        #endregion
        internal static List<WebMeta> Mapping()
        {

            List<WebMeta> metas = new List<WebMeta>();
            if (WebRuntime.webFactorys.Count > 0)
            {
                foreach (var wt in WebRuntime.webFactorys)
                {
                    var t = wt.Type;
                    WebMeta meta = new WebMeta();
                    meta.Put("type", t.FullName);
                    meta.Put("name", "." + t.Name);
                    metas.Add(meta);

                    MappingAttribute mapping = (MappingAttribute)t.GetCustomAttributes(typeof(MappingAttribute), false)[0];
                    if (String.IsNullOrEmpty(mapping.Desc) == false)
                    {
                        meta.Put("desc", mapping.Desc);

                    }

                }

            }
            if (WebRuntime.flows.Count > 0)
            {
                var em = WebRuntime.flows.GetEnumerator();
                while (em.MoveNext())
                {
                    var tls = em.Current.Value;
                    foreach (var wt in tls)
                    {
                        var t = wt.Type;
                        WebMeta meta = new WebMeta();
                        meta.Put("type", t.FullName);
                        meta.Put("name", em.Current.Key + ".");
                        meta.Put("auth", WebRuntime.authKeys[em.Current.Key].ToString().ToLower());
                        meta.Put("model", em.Current.Key);
                        metas.Add(meta);

                        var mappings = t.GetCustomAttributes(typeof(MappingAttribute), false);

                        MappingAttribute mapping = (MappingAttribute)mappings[0];
                        if (mappings.Length > 1)
                        {
                            foreach (var m in mappings)
                            {
                                var c = m as MappingAttribute;
                                if (String.Equals(c.Model, em.Current.Key))
                                {
                                    mapping = c;
                                    break;
                                }
                            }
                        }
                        if (String.IsNullOrEmpty(mapping.Desc) == false)
                        {
                            meta.Put("desc", mapping.Desc);

                        }

                    }


                }
            }
            if (WebRuntime.activities.Count > 0)
            {
                var em = WebRuntime.activities.GetEnumerator();
                while (em.MoveNext())
                {
                    var em3 = em.Current.Value.GetEnumerator();
                    while (em3.MoveNext())
                    {
                        var mappings = em3.Current.Value.Type.GetCustomAttributes(typeof(MappingAttribute), false);
                        MappingAttribute mapping = (MappingAttribute)mappings[0];
                        if (mappings.Length > 1)
                        {
                            foreach (var m in mappings)
                            {
                                var c = m as MappingAttribute;
                                if (String.Equals(c.Model, em.Current.Key) && String.Equals(c.Command, em3.Current.Key))
                                {
                                    mapping = c;
                                    break;
                                }
                            }
                        }

                        WebAuthType authType = mapping.Auth;

                        WebMeta meta = new WebMeta();
                        meta.Put("type", em3.Current.Value.Type.FullName);
                        meta.Put("name", em.Current.Key + "." + em3.Current.Key);
                        meta.Put("auth", authType.ToString().ToLower());
                        meta.Put("model", mapping.Model);
                        meta.Put("cmd", mapping.Command);
                        metas.Add(meta);

                        if (String.IsNullOrEmpty(mapping.Desc) == false)
                        {
                            meta.Put("desc", mapping.Desc);

                        }


                    }



                }
            }
            return metas;
        }

    }
}