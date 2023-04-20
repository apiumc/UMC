using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;

namespace UMC.Web
{
    /// <summary>
    /// 请求的单据
    /// </summary>
    public class WebRequest
    {
        public bool IsCashier => client.IsCashier;
        bool? _IsMaster;
        public bool IsMaster
        {
            get
            {
                if (_IsMaster.HasValue == false)
                {
                    _IsMaster = client.Token.IsInRole(Security.AccessToken.AdminRole);
                }
                return _IsMaster.Value;
            }

        }
        public string UserAgent => client.UserAgent;

        bool? _IsWeiXin;
        public bool IsWeiXin
        {
            get
            {
                if (_IsWeiXin.HasValue == false)
                {
                    if (String.IsNullOrEmpty(this.UserAgent) == false)
                    {
                        this._IsWeiXin = this.UserAgent.IndexOf("MicroMessenger") > 10;
                    }
                }
                return _IsWeiXin ?? false;
            }
        }
         

        /// <summary>
        /// 是否是消费者App
        /// </summary>
        public bool IsApp => client.IsApp; 
        //public System.IO.Stream InputStream=>   client.InputStream; 
        
        private WebClient client;


        internal protected virtual void OnInit(WebClient client, String model, String cmd, System.Collections.IDictionary header)
        {

            this.Model = model;
            this.Command = cmd;

            this._Headers = new WebMeta(header);
            this.Arguments = new WebMeta();

            this.client = client;
        }


        /// <summary>
        /// 模式下的指令
        /// </summary>
        public string Command
        {
            get;
            private set;
        }
        /// <summary>
        /// 提交的值
        /// </summary>
        public string SendValue
        {
            get
            {
                return this._Headers.Get(this.Command);
            }
        }
        /// <summary>
        /// 提交的值
        /// </summary>
        public WebMeta SendValues
        {
            get
            {
                return this._Headers.GetMeta(this.Model);
            }
        }

         
        /// <summary>
        /// 参数
        /// </summary>
        public WebMeta Arguments
        {
            get;
            private set;
        }
        WebMeta _Headers;
        /// <summary>
        /// 头信息
        /// </summary>
        public WebMeta Headers => _Headers;
        /// <summary>
        /// 模块
        /// </summary>
        public string Model
        {
            get;
            private set;
        }

        /// <summary>
        /// 客户端IP
        /// </summary>
        public string UserHostAddress => client.UserHostAddress;
        /// 请求的引用
        /// </summary>
        public Uri UrlReferrer => client.UrlReferrer;

        public Uri Url => client.Uri;
        public string Server => client.Server;
    }
}
