using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UMC.Data;

namespace UMC.Web
{

    public abstract class UICell : Data.IJSON
    {
        private class UICeller : UICell
        {
            WebMeta data;
            public override object Data => data;
            public UICeller(WebMeta data)
            {
                this.data = data;
            }
        }

        public static UICell UI(String text, String value, UIClick click)
        {
            return UICell.Create("Cell", new WebMeta().Put("value", value, "text", text).Put("click", click));

        }
        public static UICell UI(String text, UIClick click)
        {
            return UICell.Create("Cell", new WebMeta().Put("text", text).Put("click", click));
        }
        public static UICell UI(String text, String value)
        {
            return UICell.Create("Cell", new WebMeta().Put("value", value, "text", text));

        }
        public static UICell UI(string name, String text, String value, UIClick click)
        {
            return UICell.Create("TextNameValue", new WebMeta().Put("value", value, "text", text, "name", name).Put("click", click));

        }
        public static UICell UI(string name, String text, String value)
        {
            return UICell.Create("TextNameValue", new WebMeta().Put("value", value, "text", text, "name", name));

        }
        public static UICell UI(char icon, String text, String value)
        {
            return UICell.Create("UI", new WebMeta().Put("value", value, "text", text).Put("Icon", icon));

        }
        public static UICell UI(char icon, String text, String value, UIClick click)
        {
            return UICell.Create("UI", new WebMeta().Put("value", value, "text", text).Put("Icon", icon).Put("click", click));

        }

        public static UICell Create(String type, WebMeta data)
        {
            var celler = new UICeller(data);
            celler.Type = type;
            return celler;
        }

        void IJSON.Write(TextWriter writer)
        {
            if (String.IsNullOrEmpty(this.Type))
            {
                throw new ArgumentException("Cell Type is empty");
            }
            writer.Write("{\"_CellName\":");
            UMC.Data.JSON.Serialize(this.Type, writer);
            writer.Write(",");
            UMC.Data.JSON.Serialize("value", writer);
            writer.Write(":");
            UMC.Data.JSON.Serialize(this.Data, writer);
            if (this._format.Count > 0)
            {

                writer.Write(",");
                UMC.Data.JSON.Serialize("format", writer);
                writer.Write(":");
                UMC.Data.JSON.Serialize(_format, writer);
            }
            if (this._style.Count > 0)
            {

                writer.Write(",");
                UMC.Data.JSON.Serialize("style", writer);
                writer.Write(":");
                UMC.Data.JSON.Serialize(_style, writer);
            }

            writer.Write("}");
        }

        void IJSON.Read(string key, object value)
        {

        }

        String _type;
        public string Type
        {
            get
            {
                return _type;

            }
            protected set
            {
                _type = value;
            }
        }
        WebMeta _format = new WebMeta();
        public WebMeta Formats
        {
            get
            {//String.Format
                return _format;

            }
        }
        public UICell Format(string key, string format)
        {
            _format.Put(key, format);
            return this;
        }

        UIStyle _style = new UIStyle();


        public UIStyle Style
        {
            get
            {
                return _style;

            }
        }
        public abstract object Data
        {
            get;
        }
    }
}
