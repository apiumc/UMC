using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Specialized;

namespace UMC.Web
{
 

    /// <summary>
    /// 对话框
    /// </summary>
    public abstract class UIDialog
    {

        class UIFromValue : UIFormDialog
        {
            protected override string DialogType
            {
                get { throw new NotImplementedException(); }
            }
            /// </summary>
            public WebMeta InputValues
            {

                get;
                set;
            }
        }
        class UIDialogValue : UIDialog
        {
            public string InputValue
            {
                get;
                set;
            }
            protected override string DialogType
            {
                get { throw new NotImplementedException(); }
            }
        }
        class Dialog : UIDialog
        {
            public Dialog(string type)
            {
                this._DType = type;
            }
            String _DType;
            protected override string DialogType
            {
                get { return _DType; }
            }
        }
        class GridDialog : UIGridDialog
        {
            Header header;
            public GridDialog(Header header, object data)
            {
                this.IsAsyncData = true;
                this.header = header;
                this.data = data;
            }
            protected override Hashtable GetHeader()
            {
                return header.GetHeader();
            }
            object data;
            protected override Hashtable GetData(IDictionary paramsKey)
            {
                var hash = new Hashtable();
                hash["data"] = data;
                return hash;
            }
        }
        public static UIGridDialog Create(UIGridDialog.Header header, System.Data.DataTable data, bool isReturn)
        {
            return new GridDialog(header, data) { IsReturnValue = isReturn };
        }
        public static UIGridDialog Create(UIGridDialog.Header header, Array data, bool isReturn)
        {
            return new GridDialog(header, data) { IsReturnValue = isReturn };
        }
        public static UIGridDialog Create(UIGridDialog.Header header, System.Data.DataTable data)
        {
            return new GridDialog(header, data);
        }
        public static UIGridDialog Create(UIGridDialog.Header header, Array data)
        {
            return new GridDialog(header, data);
        }
        public static UIDialog CreateDialog(string type)
        {
            return new Dialog(type);
        }
        public static UIDialog CreateImage(string title, Uri uri, string tip)
        {
            var p = new Dialog("Image");
            p.Title = title;
            p.Config["Url"] = uri.AbsoluteUri;
            if (String.IsNullOrEmpty(tip) == false)
            {
                p.Config["Text"] = tip;
            }
            return p;
        }
        protected bool IsAsyncData
        {
            get;
            set;
        }

        /// <summary>
        /// 对话框Id
        /// </summary>
        protected string AsyncId
        {
            get;
            private set;
        }

        /// <summary>
        /// 创建不用返回值客户端直接返回对话框
        /// </summary>
        /// <param name="value">对话框要返回的值，只能是String，或POSMeta类型</param>
        /// <returns></returns>
        public static UIDialog ReturnValue(string value)
        {
            var v = new UIDialogValue();

            v.InputValue = value as string;
            return v;

        }
        /// <summary>
        /// 创建不用返回值客户端直接返回对话框
        /// </summary>
        /// <param name="value">对话框要返回的值，只能是String，或POSMeta类型</param>
        /// <returns></returns>
        public static UIFormDialog ReturnValue(WebMeta value)
        {
            var v = new UIFromValue();

            v.InputValues = value as WebMeta;

            return v;

        }
        //const string Dialog = "Dialog";
        /// <summary>
        /// 对话框类型
        /// </summary>
        protected abstract string DialogType
        {
            get;
        }
        /// <summary>
        /// 默认值
        /// </summary>
        public string DefaultValue
        {
            get;
            set;
        }
        public String RefreshEvent
        {
            get
            {
                return config["RefreshEvent"];
            }
            set
            {
                config["RefreshEvent"] = value;
            }
        }
        public String CloseEvent
        {
            get
            {
                return config["CloseEvent"];
            }
            set
            {
                config["CloseEvent"] = value;
            }
        }
        WebMeta config = new WebMeta();
        /// <summary>
        /// 对话框参数配置
        /// </summary>
        public WebMeta Config
        {
            get
            {
                return config;
            }
        }
        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title
        {
            get;
            set;
        }


        /// <summary>
        /// 获取异步对话框的值
        /// </summary>
        /// <param name="asyncId">异步值Id</param>
        /// <param name="dialog">对话框</param>
        public static string AsyncDialog(WebContext context, string asyncId, UIDialog dialog)
        {
            return GetAsyncValue(context, asyncId, true, aid => dialog, false) as string;
        }
        /// <summary>
        /// 获取异步对话框的值
        /// </summary>
        /// <param name="asyncId">异步值Id</param>
        /// <param name="callback">对话框回调方法</param>
        public static string AsyncDialog(WebContext context, string asyncId, Func<String, UIDialog> callback )
        {
            return GetAsyncValue(context, asyncId, true, callback, false) as string;
        }

        public static string AsyncDialog(WebContext context, string asyncId, Func<String, UIDialog> callback , bool IsDialogValue)
        {
            return GetAsyncValue(context, asyncId, true, callback, IsDialogValue) as string;
        }
        internal const string KEY_DIALOG_ID = "KEY_DIALOG_ID";
        protected static object GetAsyncValue(WebContext context, string asyncId, bool singleValue, Func<String, UIDialog> callback, bool IsDialog)
        {
            var request = context.Request;
            var response = context.Response;
            if (singleValue)
            {
                var rValue = request.Arguments.Get(asyncId);
                if (String.IsNullOrEmpty(rValue))
                {
                    var value = String.Empty;
                    var mvs = request.SendValues;
                    if (mvs != null && mvs.ContainsKey(asyncId))
                    {
                        value = mvs[asyncId];
                        mvs.Remove(asyncId);
                    }
                    else if (String.IsNullOrEmpty(request.SendValue) == false)
                    {
                        value = request.SendValue;
                        request.Headers.Remove(request.Command);
                    }

                    if (String.IsNullOrEmpty(value) == false)
                    {
                        request.Arguments[asyncId] = value;
                        return value;
                    }
                }
                var dialog = callback(asyncId);
                dialog.AsyncId = asyncId;
                if (dialog is UIDialogValue)
                {
                    var dv = dialog as UIDialogValue;
                    request.Arguments[(asyncId)] = dv.InputValue;
                    return dv.InputValue;
                }

                response.RedirectDialog(dialog);
                return rValue;
            }
            else
            {
                var rValue = request.Arguments.GetMeta(asyncId);
                if (rValue == null)
                {
                    if (request.SendValues != null)
                    {
                        var mvs = request.SendValues;
                        var sVal = mvs.Get(asyncId);
                        if (String.IsNullOrEmpty(sVal) == false)
                        {
                            if (sVal.StartsWith("{") && sVal.EndsWith("}"))
                            {
                                rValue = UMC.Data.JSON.Deserialize<WebMeta>(sVal);
                                if (rValue != null)
                                {
                                    request.Arguments[asyncId] = sVal;
                                    mvs.Remove(asyncId);
                                    return rValue;
                                }
                            }
                        }
                        if (String.Equals(mvs[KEY_DIALOG_ID], asyncId) && mvs.Count > 1)
                        {
                            rValue = mvs;
                            mvs.Remove(KEY_DIALOG_ID);
                            request.Arguments[asyncId] = UMC.Data.JSON.Serialize(mvs);
                            request.Headers.Remove(request.Model);
                            return rValue;
                        }

                    }
                    var dialog = callback(asyncId);
                    dialog.AsyncId = asyncId;
                    if (dialog is UIFromValue)
                    {
                        var dfv = dialog as UIFromValue;
                        request.Arguments[asyncId] = UMC.Data.JSON.Serialize(dfv.InputValues);

                        return dfv.InputValues;
                    }
                    response.RedirectDialog(dialog);

                }
                return rValue;
            }
        }
        public string SubmitText
        {
            get; set;
        }
        /// <summary>
        /// 初始化
        /// </summary>
        protected virtual void Initialization(WebContext context) { }

        /// <summary>
        /// 转化异步参数
        /// </summary>
        /// <returns></returns>
        internal WebMeta ToAsyncArgs(WebContext context)
        {
            Initialization(context);
            this.InitSubmit(context.Request);
            if (!String.IsNullOrEmpty(this.DefaultValue))
            {
                this.config["DefaultValue"] = DefaultValue;
            }
            if (!String.IsNullOrEmpty(Title))
            {
                this.config["Title"] = Title;
            }
            this.config["Type"] = DialogType;
            return this.config;
        }

        protected virtual void InitSubmit(WebRequest request)
        {
            var p = new WebMeta();
            p.Set("send", request.Arguments);
            p["model"] = request.Model;
            p["cmd"] = request.Command;
            if (String.IsNullOrEmpty(SubmitText) == false)
            {
                p["text"] = SubmitText;
            }
            this.config.Put("Name", this.AsyncId);
            this.config.Put("Submit", p);
        }
        /// <summary>
        /// 用异步对话框
        /// </summary>
        public static string AsyncDialog(WebContext context, string valueKey, string title)
        {
            return AsyncDialog(context, valueKey, anyc => new UITextDialog { Title = title });
        }


    }
}
