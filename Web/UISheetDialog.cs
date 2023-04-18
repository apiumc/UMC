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
    public class UISheetDialog : UIDialog
    {
        List<UMC.Data.IJSON> _nSource = new List<UMC.Data.IJSON>();

        public int Count
        {
            get
            {
                return _nSource.Count;
            }
        }
        public UISheetDialog Put(UIClick click)
        {
            _nSource.Add(click);
            return this;
        }
        public UISheetDialog Cells(int cells)
        {
            this.Config.Put("Cells", cells);
            return this;
        }
        public UISheetDialog Put(string text)
        {
            _nSource.Add(new ListItem(text));
            return this;
        }
        public UISheetDialog Put(string text, string value)
        {
            _nSource.Add(new ListItem(text, value));
            return this;
        }
        public UISheetDialog Put(ListItem item)
        {
            _nSource.Add(item);
            return this;
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
}
