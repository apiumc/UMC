using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Specialized;

namespace UMC.Web
{



    /// <summary>
    /// 表格对话框 
    /// </summary>  
    public abstract class UIGridDialog : UIDialog
    {
        public class Header
        {
            System.Collections.Hashtable headers = new System.Collections.Hashtable();
            List<System.Collections.Hashtable> fields = new List<System.Collections.Hashtable>();
            public Header(int pageSize)
            {
                headers["type"] = "grid";
                headers["pageSize"] = pageSize;
            }
            /// <summary>
            /// 如果valueField为Empty，则type将是editor
            /// </summary>
            /// <param name="valueField">对话框返回字段</param>
            /// <param name="pageSize">分页长度</param>
            public Header(string valueField, int pageSize)
            {
                headers["type"] = "dialog";

                headers["pageSize"] = pageSize;
                if (String.IsNullOrEmpty(valueField))
                {
                    headers["type"] = "grid";
                }
                else
                {
                    headers["ValueField"] = valueField;
                }
            }
            protected System.Collections.Hashtable GetField(string fieldName)
            {
                var field = fields.Find(f => (String)f["Name"] == fieldName);
                if (field == null)
                {
                    field = new System.Collections.Hashtable();
                    field["Name"] = fieldName;
                    field["type"] = "string";
                    fields.Add(field);
                }
                return field;
            }

            public void AddField(string field, string name)
            {
                var f = GetField(field);
                f["config"] = new WebMeta().Put("text", name);
            }
            public Header PutField(string fieldName, string config)
            {
                AddField(fieldName, config);
                return this;
            }
            public System.Collections.Hashtable GetHeader()
            {
                headers["fields"] = fields;
                return headers;
            }
        }
        protected UIGridDialog()
        {
            this.IsReturnValue = true;
        }

        /// <summary>
        /// 是否有返回值
        /// </summary>
        public bool IsReturnValue
        {
            get;
            set;
        }
        protected abstract Hashtable GetHeader();
        protected abstract Hashtable GetData(IDictionary paramsKey);

        public bool AutoSearch
        {
            get;
            set;
        }
        /// <summary>
        /// 类型
        /// </summary>
        protected override string DialogType
        {
            get { return "Grid"; }
        }
        /// <summary>
        /// 搜索
        /// </summary>
        public bool IsSearch
        {
            get;
            set;
        }
        /// <summary>
        /// Keywork搜索提示
        /// </summary>
        public string Keyword
        {
            get;
            set;
        }

        public void Menu(string text, string model, string cmd, string value)
        {
            this.Menu(new UIClick(value) { Text = text }.Send(model, cmd));
        }
        public void Menu(params UIClick[] menus)
        {
            this.Config.Put("menu", menus);
        }
        public void Menu(string text, string model, string cmd, WebMeta param)
        {
            this.Menu(new UIClick(param) { Text = text }.Send(model, cmd));
        }

        protected override void Initialization(WebContext context)
        {
            var request = context.Request;
            var response = context.Response;

            WebMeta meta = request.SendValues;
            if (meta != null && meta.Count > 0)
            {
                response.Redirect(this.GetData(meta.GetDictionary()));
            }

            var p = GetHeader();

            p["title"] = this.Title;
            if (IsAsyncData)
            {
                this.Config.Set("Data", this.GetData(new Hashtable()));
            }
            else if (this.IsSearch)
            {

                p["search"] = this.Keyword ?? "搜索";
            }

            if (this.IsReturnValue)
            {
                p["type"] = "dialog";
            }
            else
            {
                p["type"] = "grid";
            }
            if (this.AutoSearch)
            {
                p["auto"] = "true";
            }
            this.Config.Set("Header", p);

            base.Initialization(context);
        }
    }

}
