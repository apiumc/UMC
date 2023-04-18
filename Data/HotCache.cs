using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UMC.Data.Caches;
using UMC.Net;

namespace UMC.Data
{

    public interface IDataSubscribe
    {
        void Subscribe(byte[] buffer, int offset, int count);
    }
    public interface IStringSubscribe
    {
        void Subscribe(string message);
    }
    public sealed class HotCache
    {
        static string BuilderCodeNamespace
        {
            get; set;
        }
        public static void Namespace(string ns)
        {
            BuilderCodeNamespace = ns;
        }


        public static void BuilderCode(Type type)
        {
            var properties = type.GetProperties(BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance).Where(r => r.CanRead && r.CanWrite).ToArray();
            var sbValue = new StringBuilder();
            sbValue.AppendLine("protected override void GetValues(Action<String, object> action){");

            var sbColumns = new StringBuilder();

            sbColumns.AppendLine(@" protected override RecordColumn[] GetColumns(){");
            sbColumns.AppendLine($" var cols = new RecordColumn[{properties.Length}];");

            var sbSetValue = new StringBuilder();


            var ls = new string[properties.Length];

            for (byte i = 0; i < properties.Length; i++)
            {
                ls[i] = properties[i].Name;
            }

            Array.Sort(ls, StringComparer.CurrentCultureIgnoreCase);
            for (byte i = 0; i < ls.Length; i++)
            {
                if (i > 0)
                {
                    sbSetValue.Append(',');
                }

                sbSetValue.Append($"(r,t)=>r.{ls[i]}=Reflection.ParseObject(t, r.{ls[i]})");//\"{ls[i]}\";");

                sbColumns.AppendLine($"cols[{i}]= RecordColumn.Column(\"{ls[i]}\",this.{ls[i]});");
                sbValue.AppendLine($"AppendValue(action,\"{ls[i]}\", this.{ls[i]});");

            }

            sbValue.AppendLine("}");
            sbColumns.AppendLine("return cols;");
            sbColumns.AppendLine("}");

            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UMC.Data;");
            sb.AppendLine($"namespace {type.Namespace}");

            sb.AppendLine("{");
            sb.AppendLine($"public partial class {type.Name}");
            sb.AppendLine("{");
            sb.AppendLine($"readonly static Action<{type.Name},object>[] _SetValues=new Action<{type.Name},object>[]{{{sbSetValue}}};");
            sb.AppendLine($"readonly static string[] _Columns=new string[]{{\"{String.Join("\",\"", ls)}\"}};");

            sb.AppendLine("protected override void SetValue(string name, object obv){");
            sb.AppendLine("var index = Utility.Search(_Columns,name, StringComparer.CurrentCultureIgnoreCase);");
            sb.AppendLine("if (index > -1)_SetValues[index](this, obv);");
            sb.AppendLine("}");
            sb.AppendLine(sbValue.ToString());
            sb.AppendLine(sbColumns.ToString());
            sb.AppendLine("}");
            sb.AppendLine("}");


            Utility.Writer(Reflection.ConfigPath(String.Format("Codes\\{0}.cs", type.Name)), sb.ToString());


        }
        static Dictionary<Type, ICacheSet> dictionary = new Dictionary<Type, ICacheSet>();
        public static Cache Register<T>(params String[] primaryKeys) where T : Record, new()
        {

            var l = typeof(T);
            if (dictionary.ContainsKey(l) == false)
            {

                var hHotCache = new Cache(l.Name, new T(), primaryKeys);
                ICacheSet cacheSet = hHotCache;
                NetSubscribe.Subscribe(cacheSet.NameCode, hHotCache);
                dictionary[l] = hHotCache;
                if (BuilderCodeNamespace == l.Namespace)
                {
                    BuilderCode(l);

                }
                return hHotCache;
            }
            return null;

        }

        public static Object Cache(String type, Hashtable value)
        {
            var m = dictionary.GetEnumerator();
            while (m.MoveNext())
            {
                if (String.Equals(m.Current.Key.FullName, type))
                {
                    return m.Current.Value.Cache(value);

                }
            }
            return null;
        }

        public static ICacheSet[] Caches()
        {
            return dictionary.Values.ToArray();

        }


        public static void LoadFile()
        {

            var m = dictionary.GetEnumerator();
            while (m.MoveNext())
            {

                m.Current.Value.Load();
            }
        }
        public static void Save()
        {

            var m = dictionary.GetEnumerator();
            while (m.MoveNext())
            {
                m.Current.Value.Save();
            }
        }
        public static void Flush()
        {

            var m = dictionary.GetEnumerator();
            while (m.MoveNext())
            {
                m.Current.Value.Flush();
            }
        }


        public static Cache Cache<T>() where T : Record, new()
        {
            var type = typeof(T);
            ICacheSet cache;
            if (dictionary.TryGetValue(type, out cache))
            {
                return (Cache)cache;
            }
            return null;

        }
        public static T[] Find<T>(T t, bool isDesc, int index, int limit, out int nextIndex) where T : Record, new()
        {
            return Cache<T>().Find(t, isDesc, index, limit, out nextIndex);
        }
        public static T[] Find<T>(T t, int index, int limit, out int nextIndex) where T : Record, new()
        {
            return Cache<T>().Find(t, false, index, limit, out nextIndex);
        }
        public static T[] Find<T>(T t) where T : Record, new()
        {
            int nextIndex;
            return Cache<T>().Find(t, 0, 500, out nextIndex);
        }
        public static T[] Find<T>(T t, String field, IEnumerable e) where T : Record, new()
        {
            return Cache<T>().Find(t, field, e);
        }
        public static T Get<T>(T t) where T : Record, new()
        {
            return Cache<T>().Get(t);
        }
        public static Searcher<T> Search<T>() where T : Record, new()
        {
            return new Searcher<T>(Cache<T>());
        }

        public static void Delete<T>(T t) where T : Record, new()
        {
            Cache<T>().Delete(t);
        }
        public static void Put<T>(T t) where T : Record, new()
        {
            Cache<T>().Put(t);
        }



    }
}
