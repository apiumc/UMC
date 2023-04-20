using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UMC.Data;
namespace UMC.Data
{
    /// <summary>
    /// 信息配置集合体
    /// </summary>
    public class ProviderConfiguration : IJSON, IJSONType
    {
        public ProviderConfiguration()
        {

            this._Providers = new List<Provider>();
        }
        /// <summary>
        /// Provider提供的个数
        /// </summary>
        public int Count
        {
            get
            {
                return this._Providers.Count;
            }
        }



        // Fields
        private string _ProviderType;
        private List<Provider> _Providers;
        public Provider this[string key]
        {
            get
            {
                return this._Providers.FirstOrDefault(r => r.Name == key);
            }
        }

        /// <summary>
        /// 获取配置
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns></returns>
        public Provider this[int index]
        {
            get
            {
                return this._Providers[index] as Provider;
            }
        }
        private static System.Collections.Concurrent.ConcurrentDictionary<String, ProviderConfiguration> _cache = new System.Collections.Concurrent.ConcurrentDictionary<string, ProviderConfiguration>();

        public static System.Collections.Generic.IDictionary<String, ProviderConfiguration> Cache
        {
            get
            {
                return _cache;
            }
        }

        public static ProviderConfiguration GetProvider(string filename)
        {
            ProviderConfiguration providers = null;

            if (System.IO.File.Exists(filename))
            {
                providers = GetProvider(System.IO.File.OpenRead(filename));
                
            }
            return providers;
        }
        /// <summary>
        /// 从xml文件流中得到提供信息
        /// </summary>
        /// <param name="reader">可读流</param>
        public static ProviderConfiguration GetProvider(System.IO.TextReader reader)
        {
            ProviderConfiguration providers = new ProviderConfiguration();
            System.Xml.XmlDocument doc = new XmlDocument();
            doc.Load(reader);
            providers._ProviderType = doc.DocumentElement.Attributes["providerType"] == null ? "" : doc.DocumentElement.Attributes["providerType"].Value;
            foreach (XmlNode node2 in doc.DocumentElement.ChildNodes)
            {
                if (node2.Name == "providers")
                {
                    providers.GetProviders(node2);
                }
            }
            return providers;
        }

        public void WriteTo(System.Xml.XmlDocument doc)
        {
            var provider = doc.CreateElement("providers");
            doc.DocumentElement.AppendChild(provider);
            if (!String.IsNullOrEmpty(this._ProviderType))
            {
                var providerType = doc.CreateAttribute("providerType");
                providerType.Value = this._ProviderType;
                provider.Attributes.Append(providerType);
            }
            var em = this._Providers.GetEnumerator();

            while (em.MoveNext())
            {
                Provider pro = em.Current;

                var add = doc.CreateElement("add");
                var name = doc.CreateAttribute("name");
                name.Value = pro.Name;
                add.Attributes.Append(name);
                var type = doc.CreateAttribute("type");
                type.Value = pro.Type;
                add.Attributes.Append(type);

                for (int a = 0; a < pro.Attributes.Count; a++)
                {
                    string str = pro.Attributes[a];
                    if (!String.IsNullOrEmpty(str))
                    {
                        if (str.IndexOf('\n') > -1)
                        {
                            var node = doc.CreateElement(pro.Attributes.GetKey(a));
                            node.AppendChild(doc.CreateCDataSection(str));
                            add.AppendChild(node);
                        }
                        else
                        {
                            var att = doc.CreateAttribute(pro.Attributes.GetKey(a));
                            att.Value = str;
                            add.Attributes.Append(att);
                        }
                    }
                }
                provider.AppendChild(add);

            }

        }
        public void WriteTo(System.IO.Stream outStream)
        {
            var writer = new System.IO.StreamWriter(outStream);
            WriteTo(writer);
        }
        public void WriteTo(string filename)
        {
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(filename)))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filename));
            }
            var file = System.IO.File.Open(filename, System.IO.FileMode.Create);
            try
            {
                WriteTo(file);
            }
            finally
            {
                file.Close();
            }

        }
        public void WriteTo(System.IO.TextWriter writer)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.ConformanceLevel = ConformanceLevel.Auto;
            settings.IndentChars = "\t";
            settings.OmitXmlDeclaration = false;
            WriteTo(System.Xml.XmlWriter.Create(writer, settings));
        }
        public void WriteTo(System.Xml.XmlWriter writer)
        {
            System.Xml.XmlDocument doc = new XmlDocument();
            doc.LoadXml("<umc/>");
            WriteTo(doc);
            doc.Save(writer);
        }
        /// <summary>
        /// 从xml文件流中得到提供信息
        /// </summary>
        /// <param name="stream">可读流</param>
        public static ProviderConfiguration GetProvider(System.IO.Stream stream)
        {
            System.IO.StreamReader reader = new System.IO.StreamReader(stream);
            try
            {
                return GetProvider(reader);
            }
            finally
            {
                stream.Close();
                reader.Close();
            }
        }
        internal static ProviderConfiguration GetProvider(System.Xml.XmlNode node)
        {
            ProviderConfiguration providers = new ProviderConfiguration();
            providers.GetProviders(node);
            return providers;
        }

        void GetProviders(XmlNode node)
        {

            var attr = node.Attributes["providerType"];
            if (attr != null)
            {
                this._ProviderType = attr.Value;
            }
            foreach (XmlNode node2 in node.ChildNodes)
            {
                switch (node2.Name)
                {
                    case "add":
                        this.Add(new Provider(node2));
                        break;
                    case "remove":
                        this.Remove(node2.Attributes["name"].Value);
                        break;
                    case "clear":
                        this.Clear();
                        break;
                }
            }
        }
        public static ProviderConfiguration GetProvider(System.Xml.XPath.XPathNavigator nav)
        {
            ProviderConfiguration providers = new ProviderConfiguration();
            providers.GetProviders(nav);
            return providers;
        }
        void GetProviders(System.Xml.XPath.XPathNavigator nav1)
        {
            var navProviders = nav1.Select("providers");

            if (navProviders.Current.MoveToAttribute("providerType", String.Empty))
            {
                this._ProviderType = navProviders.Current.Value;
            }

            while (navProviders.MoveNext())
            {
                var navs = navProviders.Current.SelectChildren(System.Xml.XPath.XPathNodeType.Element);

                while (navs.MoveNext())
                {
                    var cur = navs.Current.Clone();

                    string name = cur.GetAttribute("name", string.Empty);
                    switch (cur.Name.ToLower())
                    {
                        case "add":
                            this.Add(new Provider(cur));
                            break;
                        case "remove":
                            this.Remove(name);
                            break;
                        case "clear":
                            this.Clear();
                            break;
                    }
                }
            }
        }

        void IJSON.Write(TextWriter writer)
        {

            writer.Write('{');
            writer.Write("\"ProviderType\":");
            JSON.Serialize(this._ProviderType, writer);
            writer.Write(',');
            writer.Write("\"Providers\":[");
            var em = this._Providers.GetEnumerator();
            bool IsNext = false;
            while (em.MoveNext())
            {
                if (IsNext)
                {
                    writer.Write(",");
                }
                else
                {
                    IsNext = true;
                }
                JSON.Serialize(em.Current, writer);
            }


            writer.Write("]}");
        }

        void IJSON.Read(string key, object value)
        {
            switch (key)
            {
                case "ProviderType":
                    this._ProviderType = (value ?? String.Empty).ToString();
                    break;
                case "Providers":
                    Provider[] providers = (Provider[])value;
                    foreach (var p in providers)
                    {
                        if (p != null)
                            this.Add(p);
                    }
                    break;
            }
        }

        /// <summary>
        /// 提供者属性，如果是从文件中加裁，则些属性为加裁的文件名
        /// </summary>
        public string ProviderType
        {
            get
            {
                return this._ProviderType;
            }
            set
            {
                this._ProviderType = value;
            }
        }
        public bool ContainsKey(string name)
        {
            return this._Providers.Exists(p => p.Name == name);
        }
        public void Clear()
        {
            this._Providers.Clear();
        }
        public void Add(Provider provider)
        {

            var index = this._Providers.FindIndex(p => p.Name == provider.Name);
            if (index > -1)
            {
                this._Providers[index] = provider;

            }
            else
            {
                this._Providers.Add(provider);
            }
        }
        public void Remove(String name)
        {
            var index = this._Providers.FindIndex(p => p.Name == name);
            if (index > -1)
            {
                this._Providers.RemoveAt(index);

            }
        }

        Func<object> IJSONType.GetInstance(string prototypeName)
        {
            switch (prototypeName)
            {
                default:
                case "ProviderType":
                    return () => String.Empty;
                case "Providers":
                    return () => Provider.Create("", "");

            }
        }

        /// <summary>
        /// 所有提供者节点信息
        /// </summary>
        public Provider[] Providers
        {
            get
            {
                return this._Providers.ToArray();
            }
        }
    }


}