using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UMC.Data.Sql;

namespace UMC.Data{


    public abstract class Record
    {
        static Dictionary<Type, PropertyInfo[]> keyValuePairs = new Dictionary<Type, PropertyInfo[]>();
       
        protected static void AppendValue<T>(Action<string, object> action, string name, T? t) where T : struct
        {
            if (t.HasValue)
            {
                action(name, t.Value);
            }
        }
        protected static void AppendValue<T>(Action<string, object> action, string name, T t) where T : class
        {
            if (t != null)
            {
                action(name, t);
            }
        }
        internal protected virtual void GetValues(Action<String, object> action)
        {
            var type = this.GetType();
            if (keyValuePairs.TryGetValue(type, out var properties) == false)
            {
                properties = type.GetProperties(BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance).Where(r => r.CanRead && r.CanWrite).ToArray();
                keyValuePairs[type] = properties;
            }
            foreach (var pro in properties)
            {
                if (pro.GetIndexParameters().Length == 0)
                {
                    var va = pro.GetValue(this, null); ;
                    if (va != null)
                    {
                        action(pro.Name, va);
                        // hash[pro.Name] = va;
                    }
                }
            }
            // return hash;

        }


        internal protected virtual void SetValue(String name, object obv)
        {
            var type = this.GetType();
            if (keyValuePairs.TryGetValue(type, out var properties) == false)
            {
                properties = type.GetProperties(BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance).Where(r => r.CanRead && r.CanWrite).ToArray();
                keyValuePairs[type] = properties;
            }
            var pro = properties.FirstOrDefault(r => r.Name == name);
            if (pro != null)
            {
                CBO.SetValue(this, pro, obv);
            }

        }

        internal protected virtual RecordColumn[] GetColumns()
        {
            var type = this.GetType();
            if (keyValuePairs.TryGetValue(type, out var properties) == false)
            {
                properties = type.GetProperties(BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance).Where(r => r.CanRead && r.CanWrite).ToArray();
                keyValuePairs[type] = properties;
            }

            var ls = new RecordColumn[properties.Length];


            for (byte i = 0; i < properties.Length; i++)
            {
                var column = new RecordColumn(properties[i].Name, properties[i].PropertyType);
                ls[i] = column;
            }


            return ls.ToArray();
        }
    }


}