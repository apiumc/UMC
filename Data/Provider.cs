using System;
using System.Xml;
using System.Collections.Specialized;
namespace UMC.Data
{
    /// <summary>
    /// 数据配置
    /// </summary>
    public sealed class Provider : IJSON
    {
        // Fields
        private NameValueCollection _ProviderAttributes = new NameValueCollection();
        private string _ProviderName;
        private string _ProviderType;
        private Provider()
        {
        }
        public static Provider Create(string name, string type)
        {
            var provder = new Provider();
            provder._ProviderName = name;
            provder._ProviderType = type;
            return provder;
        }
        public string this[string key]
        {
            get
            {
                return this._ProviderAttributes[key];
            }
        }
        // Methods
        internal Provider(XmlAttributeCollection Attributes)
        {
            foreach (XmlAttribute attribute in Attributes)
            {
                switch (attribute.Name)
                {
                    case "name":
                        this._ProviderName = attribute.Value;
                        break;
                    case "type":
                        this._ProviderType = attribute.Value;
                        break;
                    default:
                        this._ProviderAttributes.Add(attribute.Name, attribute.Value);
                        break;
                }
            }
        }
        internal Provider(System.Xml.XPath.XPathNavigator nav)
        {

            var ns = nav.SelectChildren(System.Xml.XPath.XPathNodeType.Element);
            if (nav.MoveToFirstAttribute())
            {
                Add(nav);
                while (nav.MoveToNextAttribute())
                {
                    Add(nav);
                }

            }
            while (ns.MoveNext())
            {
                this._ProviderAttributes.Add(ns.Current.Name, ns.Current.Value);
            }
        }
        void Add(System.Xml.XPath.XPathNavigator nav)
        {
            switch (nav.Name)
            {
                case "name":
                    this._ProviderName = nav.Value;
                    break;
                case "type":
                    this._ProviderType = nav.Value;
                    break;
                default:
                    this._ProviderAttributes.Add(nav.Name, nav.Value);
                    break;
            }
        }
        internal Provider(XmlNode node)
        : this(node.Attributes)
        {
            if (String.IsNullOrEmpty(this._ProviderName))
            {
                this._ProviderName = node.Name;
            }
            foreach (XmlNode cnode in node.ChildNodes)
            {
                if (cnode.NodeType == XmlNodeType.CDATA || cnode.NodeType == XmlNodeType.Text)
                {
                    this._ProviderAttributes.Add("nodeValue", cnode.InnerText);
                }
                else if (cnode.NodeType == XmlNodeType.Element)
                {
                    this._ProviderAttributes.Add(cnode.Name, cnode.InnerText);
                }
            }
        }

        /// <summary>
        /// 属性
        /// </summary>
        public NameValueCollection Attributes
        {
            get
            {
                return this._ProviderAttributes;
            }
        }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get
            {
                return this._ProviderName;
            }
        }
        /// <summary>
        /// 提交的类型
        /// </summary>
        public string Type
        {
            get
            {
                return this._ProviderType;
            }
        }

        #region IJSONConvert Members

        void IJSON.Write(System.IO.TextWriter writer)
        {
            writer.Write('{');
            writer.Write("\"name\":");
            JSON.Serialize(this._ProviderName, writer);
            writer.Write(',');
            writer.Write("\"type\":");
            JSON.Serialize(this._ProviderType, writer);
            for (var i = 0; i < this._ProviderAttributes.Count; i++)
            {
                var name = this._ProviderAttributes.GetKey(i);
                if (!String.IsNullOrEmpty(name) && name != "name" && name != "type")
                {
                    writer.Write(',');
                    JSON.Serialize(name, writer);
                    writer.Write(':');
                    JSON.Serialize(this._ProviderAttributes.Get(i), writer);
                }
            }
            writer.Write('}');
        }

        void IJSON.Read(string key, object value)
        {
            switch (key)
            {
                case "name":
                    this._ProviderName = (value ?? String.Empty).ToString();
                    break;
                case "type":
                    this._ProviderType = (value ?? String.Empty).ToString();
                    break;
                default:
                    this._ProviderAttributes.Add(key, (value ?? String.Empty).ToString());
                    break;
            }
        }

        #endregion
    }



}