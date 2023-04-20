using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UMC.Data;

namespace UMC.Web.UI
{
    public class UIComment : UICell
    {
        public class Image
        {
            public string max
            {
                get; set;
            }
            public string src
            {
                get; set;
            }
        }
        public class Icon
        {
            public string icon
            {
                get; set;
            }
            public string src
            {
                get; set;
            }
            public int? color
            {
                get; set;
            }
            public UIClick click { get; set; }

            public string badge
            {
                get; set;
            }
        }
        public class Reply : UMC.Data.IJSON
        {
            public string title
            {
                get; set;
            }
            public string content
            {
                get; set;
            }
            public UIStyle style
            {
                get; set;

            }
            public WebMeta data
            {
                get; set;
            }

            void IJSON.Read(string key, object value)
            {

            }

            void IJSON.Write(System.IO.TextWriter writer)
            {
                UMC.Data.JSON.Serialize(new WebMeta().Put("format", new WebMeta().Put("content", this.content, "title", this.title)).Put("value", this.data).Put("style", this.style), writer);
            }
        }
        WebMeta data;
        public override object Data => data;
        public UIComment(string src)
        {
            this.data = new WebMeta().Put("src", src);
            this.Type = "Comment";
        }
        public UIComment Name(string name, string value)
        {

            this.data.Put(name, value);
            return this;
        }
        public UIComment ImageClick(UIClick click)
        {
            this.data.Put("image-click", click);
            return this;
        }
        public UIComment Desc(string desc)
        {

            this.data.Put("desc", desc);
            return this;
        }
        public UIComment Name(string name)
        {
            this.data.Put("name", name);
            return this;
        }
        public UIComment Bottom(string time, params UIEventText[] right)
        {
            this.data.Put("bottom", new WebMeta().Put("time", time).Put("right", right));
            return this;
        }
        public UIComment Content(string content)
        {
            this.data.Put("content", content);
            return this;
        }
        public UIComment Images(params Image[] images)
        {
            this.data.Put("image", images);
            return this;

        }
        public string Id
        {
            get; set;
        }
        public UIComment Icons(UIEventText more, UIEventText[] right, params Icon[] icons)
        {
            this.data.Put("icons", new WebMeta().Put("value", icons).Put("more", more).Put("right", right));
            return this;

        }
        public UIComment Icons(UIEventText more, params Icon[] icons)
        {
            this.data.Put("icons", new WebMeta().Put("value", icons).Put("more", more));
            return this;
        }
        public UIComment Icons(params Icon[] icons)
        {
            this.data.Put("icons", new WebMeta().Put("value", icons));
            return this;
        }
        public UIComment Replys(params Reply[] replys)
        {
            this.data.Put("replys", new WebMeta().Put("value", replys));
            return this;

        }
        public UIComment Replys(UIEventText more, params Reply[] replys)
        {
            this.data.Put("replys", new WebMeta().Put("value", replys).Put("more", more));
            return this;

        }
        public UIComment Tag(UIEventText tag)
        {
            this.data.Put("tag", tag);
            return this;
        }
    }
}
