using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Specialized;

namespace UMC.Web
{
    /// <summary>
    /// 选择对话框
    /// </summary>
    public class UISelectDialog : UIDialog
    {
        ListItemCollection _nSource = new ListItemCollection();



        /// <summary>
        /// 文本对话框选择配置
        /// </summary>
        public ListItemCollection Options
        {
            get
            {
                return _nSource;
            }
        }

        protected override void Initialization(WebContext context)
        {
            this.Config.Put("DataSource", _nSource);
        }
        /// <summary>
        /// 对话框类型
        /// </summary>
        protected override string DialogType
        {
            get { return "Select"; }
        }


    }
    /// <summary>
    /// 单选框
    /// </summary>
    public class UIRadioDialog : UISelectDialog
    {
        /// <summary>
        /// 对话框类型
        /// </summary>
        protected override string DialogType
        {
            get
            {
                return "RadioGroup";
            }
        }
    }
    /// <summary>
    /// 复选框 
    /// </summary>
    public class UICheckboxDialog : UISelectDialog
    {
        /// <summary>
        /// 对话框类型
        /// </summary>
        protected override string DialogType
        {
            get
            {
                return "CheckboxGroup";
            }
        }
        protected override void Initialization(Web.WebContext context)
        {
            if (String.IsNullOrEmpty(this.DefaultValue) == false)
            {
                this.Config["DefaultValue"] = this.DefaultValue;
            }
            base.Initialization(context);
        }
    }
}
