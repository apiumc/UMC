//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Net;
//using System.Text;
//using System.Threading;
//using UMC.Data;
//using UMC.Net;
//using UMC.Security;

//namespace UMC.Web
//{
//    /// <summary>
//    /// 网络请求
//    /// </summary>
//    public class WebFactory : IWebFactory
//    {
//        public class XHRer : IJSON
//        {
//            public XHRer(String ex)
//            {
//                this.expression = ex;
//            }
//            string expression;
//            #region IJSONConvert Members

//            void IJSON.Write(System.IO.TextWriter writer)
//            {
//                writer.Write(expression);
//            }

//            void IJSON.Read(string key, object value)
//            {
//            }

//            #endregion
//        }
//        class XHRFlow : WebFlow
//        {
//            private Uri uri;
//            private String appSecret;
//            public XHRFlow(Uri uri, string secret)
//            {
//                this.uri = uri;
//                this.appSecret = secret;

//            }
//            public XHRFlow(Uri uri, string secret, params string[] cmds)
//            {

//                this.uri = uri;
//                this.appSecret = secret;
//                this.cmds.AddRange(cmds);
//            }
//            private System.Text.RegularExpressions.Regex regex;
//            public XHRFlow(Uri uri, string secret, System.Text.RegularExpressions.Regex regex)
//            {

//                this.appSecret = secret;
//                this.uri = uri;
//                this.regex = regex;

//            }
//            private List<String> cmds = new List<string>();
//            public override WebActivity GetFirstActivity()
//            {
//                var cmd = this.Context.Request.Command;
//                if (this.cmds.Count > 0)
//                {
//                    if (String.Equals("*", this.cmds[0]))
//                    {
//                        if (this.cmds.Exists(g => g == cmd))
//                        {
//                            return WebActivity.Empty;
//                        }
//                    }
//                    else if (this.cmds.Exists(g => g == cmd) == false)
//                    {
//                        return WebActivity.Empty;
//                    }
//                }
//                else if (regex != null && regex.IsMatch(cmd) == false)
//                {
//                    return WebActivity.Empty;
//                }
//                StringBuilder sb = new StringBuilder();
//                WebRequest req = this.Context.Request;
//                if (this.uri.AbsoluteUri.EndsWith("/*"))
//                {
//                    sb.Append(this.uri.AbsoluteUri.Substring(0, this.uri.AbsoluteUri.Length - 1));
//                }
//                else
//                {
//                    sb.Append(this.uri.AbsoluteUri.TrimEnd('/'));
//                    sb.Append("/");
//                    sb.Append(Data.Utility.GetRoot(req.Url));
//                    sb.Append("/");
//                }
//                sb.Append(Utility.Guid(this.Context.Token.Device.Value));
//                sb.Append("/");
//                if (req.Headers.ContainsKey(EventType.Dialog))
//                {
//                    WebMeta meta = req.Headers.GetMeta(EventType.Dialog);
//                    if (meta != null)
//                    {
//                        var em = meta.GetDictionary().GetEnumerator();
//                        var isOne = true;
//                        while (em.MoveNext())
//                        {
//                            if (isOne)
//                            {
//                                sb.Append("?");
//                                isOne = false;
//                            }
//                            else
//                            {
//                                sb.Append("&");
//                            }
//                            sb.Append(Uri.EscapeDataString(em.Key.ToString()));
//                            sb.Append("=");
//                            sb.Append(Uri.EscapeDataString(em.Value.ToString()));


//                        }
//                    }
//                    else
//                    {
//                        String dg = req.Headers.Get(EventType.Dialog);
//                        sb.Append("?");
//                        sb.Append(Uri.UnescapeDataString(dg));


//                    }
//                }
//                else
//                {
//                    sb.Append(req.Model);
//                    sb.Append("/");
//                    sb.Append(req.Command);
//                    sb.Append("/");
//                    WebMeta meta = req.SendValues;// ();
//                    if (meta != null)
//                    {
//                        var em = meta.GetDictionary().GetEnumerator();
//                        var isOne = true;
//                        while (em.MoveNext())
//                        {
//                            if (isOne)
//                            {
//                                sb.Append("?");
//                                isOne = false;
//                            }
//                            else
//                            {
//                                sb.Append("&");
//                            }
//                            sb.Append(Uri.EscapeDataString(em.Key.ToString()));
//                            sb.Append("=");
//                            sb.Append(Uri.EscapeDataString(em.Value.ToString()));


//                        }

//                    }
//                    else
//                    {

//                        String dg = req.SendValue;
//                        if (String.IsNullOrEmpty(dg) == false)
//                        {
//                            sb.Append("?");
//                            sb.Append(Uri.EscapeDataString(dg));

//                        }

//                    }

//                }

//                var user = this.Context.Token.Identity();
//                var webRequest = new Uri(sb.ToString()).WebRequest();
//                webRequest.UserAgent = req.UserAgent;


//                if (String.IsNullOrEmpty(this.appSecret) == false)
//                {
//                    var nvs = new System.Collections.Specialized.NameValueCollection();
//                    nvs.Add("umc-request-time", this.Context.runtime.Client.XHRTime.ToString());
//                    nvs.Add("umc-request-user-id", Utility.Guid(user.Id.Value));
//                    nvs.Add("umc-request-user-name", user.Name);
//                    nvs.Add("umc-request-user-alias", user.Alias);
//                    if (user.Roles != null && user.Roles.Length > 0)
//                    {
//                        nvs.Add("umc-request-user-role", String.Join(",", user.Roles));

//                    }
//                    nvs.Add("umc-request-sign", Utility.Sign(nvs, this.appSecret));
//                    for (var i = 0; i < nvs.Count; i++)
//                    {
//                        webRequest.Headers.Add(nvs.GetKey(i), Uri.EscapeDataString(nvs.Get(i)));
//                    }
//                }
//                else
//                {
//                    webRequest.Headers.Add("umc-request-time", this.Context.runtime.Client.XHRTime.ToString());

//                }

//                if (this.Context.Request.UrlReferrer != null)
//                {
//                    webRequest.Referer = this.Context.Request.UrlReferrer.AbsoluteUri;
//                }
//                var ress = webRequest.Get();
//                int StatusCode = (int)ress.StatusCode;

//                String xhr = ress.ReadAsString();
//                if (StatusCode > 300 && StatusCode < 400)
//                {
//                    var url = ress.Headers.Get("Location");
//                    if (String.IsNullOrEmpty(url) == false)
//                    {
//                        this.Context.Response.Redirect(new Uri(webRequest.RequestUri, url));
//                    }
//                }
//                this.Context.Response.Redirect(Data.JSON.Expression(xhr));

//                return WebActivity.Empty;
//            }
//        }
//        public virtual WebFlow GetFlowHandler(string mode)
//        {

//            var cgf = Data.Reflection.Configuration("UMC");

//            if (cgf != null)
//            {
//                var provder = cgf[mode];
//                if (provder != null)
//                {
//                    var url = (provder["src"]);
//                    if (String.IsNullOrEmpty(url) == false)
//                    {
//                        string secret = provder["secret"] as string;
//                        if (String.IsNullOrEmpty(provder.Type) == false)
//                        {
//                            if (provder.Type.StartsWith("/") && provder.Type.EndsWith("/"))
//                            {

//                                return new XHRFlow(new Uri(url), secret, new System.Text.RegularExpressions.Regex(provder.Type.Trim('/')));
//                            }
//                            else if (String.Equals("*", provder.Type) == false)
//                            {
//                                return new XHRFlow(new Uri(url), secret, provder.Type.Split(','));

//                            }
//                            else
//                            {
//                                return new XHRFlow(new Uri(url), secret);
//                            }
//                        }
//                        else
//                        {

//                            return new XHRFlow(new Uri(url), secret);
//                        }
//                    }
//                }
//            }
//            return WebFlow.Empty;
//        }
//        /// <summary>
//        /// 请在此方法中完成url与model的注册,即调用registerModel方法
//        /// </summary>
//        /// <param name="context"></param>
//        public virtual void OnInit(WebContext context)
//        {

//        }
//    }
//}