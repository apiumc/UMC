using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Collections;

namespace UMC.Web
{


    /// <summary>
    /// 请求的单据
    /// </summary>
    public class WebClient
    {
        class CommandKey
        {
            public string cmd
            {
                get;
                set;
            }
            public string model
            {
                get;
                set;
            }
            public string value
            {
                get;
                set;
            }

        }

        internal const int OuterDataEvent = 131072;

        /// <summary>
        /// 扩展
        /// </summary>
        internal static readonly WebEvent Prompt = (WebEvent)2048;

        public bool? IsVerify
        {
            get;
            set;
        }
        internal WebRuntime _runtime;
        public void Clear(WebEvent Event)
        {
            if ((this.ClientEvent & Event) == Event)
            {
                this.ClientEvent = this.ClientEvent ^ Event;
            }
            switch (Event)
            {
                case WebEvent.Normal:
                    break;
            }
        }
        internal String Server
        {
            get
            {
                return this._context.Server;
            }
        }
        internal System.IO.Stream InputStream
        {
            get
            {
                return this._context.InputStream;
            }
        }
        public System.Collections.Hashtable OuterHeaders
        {
            get;
            private set;
        }

        public WebEvent ClientEvent
        {
            get;
            set;
        }
        int RedirectTimes = 0;


        public Uri Uri
        {
            get;
            set;
        }
        public string UserHostAddress
        {
            get;
            set;
        }
        public Uri UrlReferrer
        {
            get;
            set;
        }

        public bool IsApp { get; set; }
        public UMC.Security.AccessToken Token { get; set; }
        public bool IsCashier { get; set; }
        public string UserAgent
        {
            get;
            set;
        }

        static bool CheckApp(String UserAgent)
        {

            if (String.IsNullOrEmpty(UserAgent) == false)
            {
                return UserAgent.Contains("UMC Client");
            }
            return false;
        }
        internal string _jsonp;
        Net.NetContext _context;
        public WebClient(Net.NetContext context)
        {
            this._context = context;
            this.Token = context.Token;
            this.Uri = context.Url;
            this.UserHostAddress = context.UserHostAddress;
            this.UrlReferrer = context.UrlReferrer;
            this.UserAgent = context.UserAgent;

            this.IsCashier = context.Token.IsInRole(UMC.Security.AccessToken.UserRole);
            this.IsApp = CheckApp(this.UserAgent);

        }

        //internal int XHRTime = 0;

        Queue<CommandKey> _cmds;// = new Queue<CommandKey>();
        Hashtable _Finish;//= new Hashtable();


        void QueueTo()
        {
            var cmd = _cmds.Dequeue(); //_cmds.Peek();
            if ((Convert.ToInt32(this.ClientEvent) & OuterDataEvent) == OuterDataEvent)
            {
                var data = this.OuterHeaders["Data"];
                _Finish[$"{cmd.model}.{cmd.cmd}.{cmd.value}"] = data;
                if (data is System.Uri)
                {
                    _Finish[$"{cmd.model}.{cmd.cmd}.{cmd.value}"] = (data as System.Uri).AbsoluteUri;
                }
                else
                {
                    var sb = new StringBuilder();
                    var writer = new System.IO.StringWriter(sb);
                    UMC.Data.JSON.Serialize(data, writer);
                    writer.Flush();

                    _Finish[$"{cmd.model}.{cmd.cmd}.{cmd.value}"] = UMC.Data.JSON.Expression(sb.ToString());
                }
            }
            else
            {

                var sb = new StringBuilder();
                var writer = new System.IO.StringWriter(sb);

                writer.Write('{');
                writer.Write("\"ClientEvent\":{0}", Convert.ToInt32(this.ClientEvent));
                if (this.OuterHeaders != null && this.OuterHeaders.Count > 0)
                {
                    writer.Write(",\"Headers\":");
                    UMC.Data.JSON.Serialize(this.OuterHeaders, writer);
                }
                if (RedirectTimes > 0 && _Redirect != null)
                {
                    writer.Write(",\"Redirect\":");
                    UMC.Data.JSON.Serialize(this._Redirect, writer);
                }
                writer.Write('}');

                writer.Flush();

                _Finish[$"{cmd.model}.{cmd.cmd}.{cmd.value}"] = UMC.Data.JSON.Expression(sb.ToString());

            }
            if (_cmds.Count == 0)
            {
                _context.ContentType = "text/javascript;charset=utf-8";
                var writer = _context.Output;
                if (String.IsNullOrEmpty(_jsonp) == false)
                {
                    writer.Write(_jsonp);
                    writer.Write("(");
                }
                UMC.Data.JSON.Serialize(_Finish, writer);
                if (String.IsNullOrEmpty(_jsonp) == false)
                {
                    writer.Write(")");
                }
                writer.Flush();
            }
            else
            {
                Command(_cmds.Peek());
            }
        }
        void Command(CommandKey c)
        {

            this.OuterHeaders = new Hashtable();

            this.ClientEvent = WebEvent.None;
            _Output = QueueTo;
            if (Verify(c.model, c.cmd))
            {
                if (String.IsNullOrEmpty(c.value))
                {
                    this.Redirect(c.model, c.cmd, String.Empty);
                }
                else if (c.value.IndexOf("=") > -1)
                {
                    var QueryString = System.Web.HttpUtility.ParseQueryString(c.value) ?? new NameValueCollection();
                    this.Redirect(c.model, c.cmd, QueryString);
                }
                else
                {
                    this.Redirect(c.model, c.cmd, c.value);
                }
            }
            else
            {
                QueueTo();
            }

        }
        public void Command(string json)
        {
            _Finish = new Hashtable();
            var cmds = UMC.Data.JSON.Deserialize<CommandKey[]>(json);

            _cmds = new Queue<CommandKey>();

            foreach (var c3 in cmds)
            {
                _cmds.Enqueue(c3);
            };
            if (cmds.Length > 0)
            {
                Command(_cmds.Peek());
            }
            else
            {
                this._WriteTo();
            }




        }
        public void Command(string model, string cmd, string value)
        {

            _Output = this._WriteTo;
            if (Verify(model, cmd))
            {
                Redirect(model, cmd, value);
            }
            else
            {
                _Output();
            }




        }
        void Redirect(string model, string cmd, string value)
        {
            var hash = new Hashtable();
            if (!String.IsNullOrEmpty(value))
            {
                hash[cmd] = value;
            }
            this.Send(model, cmd, hash);

        }

        bool Verify(string model, string cmd)
        {
            if (this.IsVerify.HasValue == false)
            {
                String key = String.Format("{0}.{1}", model, cmd);
                WebAuthType authType = WebAuthType.Check;
                if (WebRuntime.authKeys.ContainsKey(key))
                {
                    authType = WebRuntime.authKeys[key];
                }
                else if (WebRuntime.authKeys.ContainsKey(model))
                {
                    authType = WebRuntime.authKeys[model];
                }
                var user = this.Token.Identity();

                switch (authType)
                {
                    case WebAuthType.All:
                        this.IsVerify = true;
                        return true;
                    case WebAuthType.User:
                        if (user.IsInRole(Security.AccessToken.UserRole))
                        {
                            this.IsVerify = true;
                            return true;

                        }
                        break;
                    case WebAuthType.UserCheck:
                        if (user.IsInRole(Security.AccessToken.AdminRole))
                        {
                            this.IsVerify = true;
                            return true;

                        }
                        else if (user.IsInRole(Security.AccessToken.UserRole))
                        {
                            if (UMC.Data.Reflection.Instance().IsAuthorization(user, String.Format("UMC/{0}/{1}", model, cmd)))
                            {
                                this.IsVerify = true;
                                return true;
                            }
                        }
                        break;
                    case WebAuthType.Check:
                        if (user.IsInRole(Security.AccessToken.AdminRole))
                        {
                            this.IsVerify = true;
                            return true;

                        }
                        else if (UMC.Data.Reflection.Instance().IsAuthorization(user, String.Format("UMC/{0}/{1}", model, cmd)))
                        {
                            this.IsVerify = true;
                            return true;
                        }

                        break;
                    case WebAuthType.Admin:
                        if (user.IsInRole(Security.AccessToken.AdminRole))
                        {
                            this.IsVerify = true;
                            return true;

                        }
                        break;
                    case WebAuthType.Guest:
                        if (user.IsAuthenticated)
                        {
                            this.IsVerify = true;
                            return true;

                        }
                        else
                        {
                            this.OuterHeaders = new Hashtable();
                            this.ClientEvent = WebEvent.Prompt | WebEvent.DataEvent;
                            this.OuterHeaders["Prompt"] = new WebMeta().Put("Title", "提示", "Text", "您没有登录,请登录");

                            this.OuterHeaders["DataEvent"] = new WebMeta().Put("type", "Login");
                            return false;
                        }
                }

                this.OuterHeaders = new Hashtable();
                this.ClientEvent = WebEvent.Prompt;
                if (user.IsInRole(Security.AccessToken.UserRole) == false)
                {
                    this.OuterHeaders["Prompt"] = new WebMeta().Put("Title", "提示", "Text", "您没有登录或权限受限");
                    //this.ClientEvent = WebEvent.Prompt | WebEvent.DataEvent;
                    //this.OuterHeaders["DataEvent"] = new WebMeta().Put("type", "Close");
                }
                else
                {
                    this.OuterHeaders["Prompt"] = new WebMeta().Put("Title", "提示", "Text", "您的权限受限,请与管理员联系");

                }
                return false;

            }
            return true;
        }


        /// <summary>
        /// 当前处理共享健值
        /// </summary>
        internal System.Collections.Hashtable Items = new System.Collections.Hashtable();

        public void Command(string model, string cmd, NameValueCollection queryString)
        {

            _Output = this._WriteTo;
            if (Verify(model, cmd))
            {
                Redirect(model, cmd, queryString);
            }
            else
            {
                _Output();
            }




        }
        void Redirect(string model, string cmd, NameValueCollection QueryString)
        {

            switch (QueryString.Count)
            {
                case 0:
                    this.Redirect(model, cmd, String.Empty);
                    break;
                case 1:
                    var skey = QueryString.GetKey(0);
                    var svalue = QueryString.Get(0);
                    if (String.IsNullOrEmpty(skey) || String.Equals(skey, "_"))
                    {
                        this.Redirect(model, cmd, svalue);

                    }
                    else if (String.IsNullOrEmpty(svalue))
                    {

                        this.Redirect(model, cmd, skey);
                    }
                    else
                    {

                        goto default;
                    }
                    break;
                default:
                    var sendValue = new System.Collections.Hashtable();

                    var header = new System.Collections.Hashtable();
                    for (var i = 0; i < QueryString.Count; i++)
                    {
                        var key = QueryString.GetKey(i);
                        var value = QueryString.Get(i);
                        if (String.IsNullOrEmpty(key) || String.Equals(key, "_"))
                        {
                            header[cmd] = value;
                        }
                        else
                        {
                            sendValue[key] = value;
                        }
                    }
                    header[model] = sendValue;
                    this.Send(model, cmd, header);
                    break;
            }

        }


        void OutputHeader(UMC.Web.WebMeta header)
        {
            if (this.OuterHeaders == null)
            {
                this.OuterHeaders = new Hashtable(); ;
            }

            var dic = header.GetDictionary();

            var em = dic.GetEnumerator();
            while (em.MoveNext())
            {
                var key = em.Key.ToString();
                if (String.Equals(key, "DataEvent"))
                {
                    var value = em.Value;
                    if (this.OuterHeaders.ContainsKey(key))
                    {
                        var ats = new System.Collections.ArrayList();
                        var ts = this.OuterHeaders[key];

                        if (ts is Array)
                        {
                            ats.AddRange((Array)ts);
                        }
                        else
                        {

                            ats.Add(ts);

                        }
                        if (value is Array)
                        {

                            ats.AddRange((Array)value);
                        }
                        else
                        {
                            ats.Add(value);
                        }
                        this.OuterHeaders[key] = ats.ToArray();
                    }
                    else
                    {
                        this.OuterHeaders[em.Key] = em.Value;
                    }
                }
                else
                {
                    this.OuterHeaders[em.Key] = em.Value;
                }
            }

        }
        WebMeta _Redirect;
        public void Atfer(WebContext context)
        {

            var request = context.Request;
            var response = context.Response;
            var webEvent = response.ClientEvent;
            var redirect = response.ClientRedirect;
            if ((Convert.ToInt32(webEvent) & OuterDataEvent) == OuterDataEvent)
            {
                var data = response.Headers.GetDictionary()["Data"];
                if ((data is WebActivity))
                {
                    response.ClientEvent = WebEvent.None;
                    response.Headers.Remove("Data");
                    if (_context.AllowSynchronousIO == false)
                    {
                        _context.UseSynchronousIO(() => { });
                    }
                }
                else
                {
                    this.ClientEvent = webEvent;
                    this.OutputHeader(response.Headers);
                    this._Output();
                }
                return;
            }

            if (webEvent != WebEvent.None)
            {
                this.ClientEvent |= webEvent;
                OutputHeader(response.Headers);
            }

            if (redirect != null)
            {
                this.RedirectTimes++;
                if (this.RedirectTimes > 10)
                {
                    throw new Exception(String.Format("{0}.{1},请求重定向超过最大次数", redirect.Model, redirect.Command));
                }
                if (String.IsNullOrEmpty(redirect.Value))
                {
                    this.Redirect(redirect.Model, redirect.Command, String.Empty);
                }
                else
                {
                    if (redirect.Value.IndexOf("&") > -1)
                    {
                        var nquery = System.Web.HttpUtility.ParseQueryString(redirect.Value);
                        this.Redirect(redirect.Model, redirect.Command, nquery);
                    }
                    else if (redirect.Value.StartsWith("{"))
                    {
                        var p = UMC.Data.JSON.Deserialize(redirect.Value) as Hashtable;
                        var pos = new NameValueCollection();
                        var em = p.GetEnumerator();
                        while (em.MoveNext())
                        {
                            pos[em.Key.ToString()] = em.Value.ToString();
                        }
                        this.Redirect(redirect.Model, redirect.Command, pos);
                    }
                    else
                    {
                        this.Redirect(redirect.Model, redirect.Command, redirect.Value);
                    }
                }
                return;
            }

            context.Completed();
            if (context.runtime.CurrentActivity == null)
            {
                _Redirect = new WebMeta().Put("model", context.Request.Model, "cmd", context.Request.Command);
                var sv = request.SendValues ?? new WebMeta();

                if (String.IsNullOrEmpty(request.SendValue) == false)
                {
                    if (sv.Count > 0)
                    {
                        sv.Put("_", request.SendValue);
                        _Redirect.Put("send", sv);
                    }
                    else
                    {
                        _Redirect.Put("send", request.SendValue);
                    }

                }
                else if (sv.Count > 0)
                {

                    _Redirect.Put("send", sv);
                }

            }
            _Output();
        }
        void Send(String model, String cmd, System.Collections.IDictionary value)
        {
            var context = WebRuntime.ProcessRequest(model, cmd, value, this);

            Atfer(context);

        }



        public Net.NetContext Context
        {
            get
            {
                return _context;
            }
        }
        Action _Output;
        void _WriteTo()
        {
            if ((Convert.ToInt32(this.ClientEvent) & OuterDataEvent) == OuterDataEvent)
            {
                var data = this.OuterHeaders["Data"];

                if (data is System.Uri)
                {
                    _context.Redirect((data as System.Uri).AbsoluteUri);

                }
                else
                {
                    _context.ContentType = "text/javascript;charset=utf-8";
                    var writer = _context.Output;
                    if (String.IsNullOrEmpty(_jsonp) == false)
                    {
                        writer.Write(_jsonp);
                        writer.Write("(");
                    }
                    UMC.Data.JSON.Serialize(data, writer);
                    if (String.IsNullOrEmpty(_jsonp) == false)
                    {
                        writer.Write(")");
                    }
                    writer.Flush();
                }
                _context.OutputFinish();
            }
            else
            {
                _context.ContentType = "text/javascript;charset=utf-8";
                var writer = _context.Output;
                if (String.IsNullOrEmpty(_jsonp) == false)
                {
                    writer.Write(_jsonp);
                    writer.Write("(");
                }

                writer.Write('{');
                writer.Write("\"ClientEvent\":{0}", Convert.ToInt32(this.ClientEvent));
                if (this.OuterHeaders != null && this.OuterHeaders.Count > 0)
                {
                    writer.Write(",\"Headers\":");
                    UMC.Data.JSON.Serialize(this.OuterHeaders, writer);
                }
                if (RedirectTimes > 0 && _Redirect != null)
                {
                    writer.Write(",\"Redirect\":");
                    UMC.Data.JSON.Serialize(this._Redirect, writer);
                }
                writer.Write('}');
                if (String.IsNullOrEmpty(_jsonp) == false)
                {
                    writer.Write(")");
                }
                writer.Flush();
                _context.OutputFinish();
            }
        }

    }

}
