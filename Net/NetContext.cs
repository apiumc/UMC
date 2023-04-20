using System;
using System.Collections.Specialized;

namespace UMC.Net
{
    public delegate void NetReadData(byte[] buffer, int offset, int size);
    public abstract class NetContext
    {
        public abstract int StatusCode
        {
            get;
            set;
        }
        public abstract string Server
        {
            get;
        }
        public abstract string ContentType
        {
            get;
            set;
        }
        public abstract long? ContentLength
        {
            get;
            set;
        }
        public abstract string UserHostAddress
        {
            get;
        }
        public abstract string RawUrl
        {
            get;
        }
        public abstract bool IsWebSocket
        {
            get;
        }
        public abstract string UserAgent
        {
            get;
        }
        public abstract void AddHeader(string name, string value);
        public abstract void AppendCookie(string name, string value);
        public abstract void AppendCookie(string name, string value, string path);
        public abstract NameValueCollection Headers
        {
            get;
        }

        public abstract NameValueCollection QueryString
        {
            get;
        }
        public abstract NameValueCollection Cookies
        {
            get;
        }
        public virtual UMC.Security.AccessToken Token
        {
            get;
            set;
        }

        public abstract void RewriteUrl(String pathAndQuery);
        public abstract void ReadAsForm(Action<NameValueCollection> action);
        public abstract void ReadAsData(Net.NetReadData readData);

        public abstract bool AllowSynchronousIO
        {
            get;
        }
        public virtual void UseSynchronousIO(Action action)
        {

        }
        public virtual void OutputFinish()
        {

        }
        public virtual void Error(Exception ex)
        {

        }
        public abstract System.IO.TextWriter Output
        {
            get;
        }

        public abstract System.IO.Stream OutputStream
        {
            get;
        }
        public abstract Uri UrlReferrer
        {
            get;
        }
        public abstract Uri Url
        {
            get;
        }
        public abstract string HttpMethod
        {
            get;
        }
         
        public abstract void Redirect(string url);

        public Object Tag
        {
            get; set;
        }
        public Guid? AppKey
        {
            get; set;
        }


    }


}
