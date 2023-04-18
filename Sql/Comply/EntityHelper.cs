using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace UMC.Data.Sql
{


    class EntityHelper
    {
        public List<object> Arguments
        {
            get;
            private set;
        }

        /// <summary>
        /// 类型的表属性
        /// </summary>
        public String TableName
        {
            get;
            set;
        }
        
        DbProvider Provider;
        
        public EntityHelper(DbProvider dbProvider, String tableName)
        {
            this.Provider = dbProvider;
            this.Arguments = new List<object>();
            this.TableName = tableName;
            
        }


        String ObjectName(String name)
        {
            if (this.Provider.Builder != null)
            {
                return this.Provider.Builder.Column(name);
            }

            return name;
        }
        public string CreateInsertText<T>(T entity) where T : Record
        {  StringBuilder sb = new StringBuilder();
            sb.AppendFormat("INSERT INTO {0}(", this.TableName);
            StringBuilder sb2 = new StringBuilder();
            sb2.Append("VALUES(");
            bool IsMush = false;
            this.Arguments.Clear();
            
            entity.GetValues((field, value) =>
            {

                if (IsMush)
                {
                    sb.Append(",");
                    sb2.Append(",");
                }
                else
                {
                    IsMush = true;
                }

                sb.Append(Provider.QuotePrefix);
                sb.Append(ObjectName(field as string));
                sb.Append(Provider.QuoteSuffix);

                sb2.Append('{');
                sb2.Append(this.Arguments.Count);
                sb2.Append('}');

                this.Arguments.Add(value);

            });

            sb.Append(")");
            sb2.Append(")");
            return sb.ToString() + sb2.ToString();

        }
        public string CreateInsertText(System.Collections.IDictionary fieldValues)
        {

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("INSERT INTO {0}(", this.TableName);
            StringBuilder sb2 = new StringBuilder();
            sb2.Append("VALUES(");
            bool IsMush = false;
            this.Arguments.Clear();
            var values = fieldValues.GetEnumerator();

            while (values.MoveNext())
            {
                //var field = values.Key as string;
                var value = values.Value;
                if (value == null)
                {
                    continue;
                }

                if (IsMush)
                {
                    sb.Append(",");
                    sb2.Append(",");
                }
                else
                {
                    IsMush = true;
                }

                sb.Append(Provider.QuotePrefix);
                sb.Append(ObjectName(values.Key as string));
                sb.Append(Provider.QuoteSuffix);

                sb2.Append('{');
                sb2.Append(this.Arguments.Count);
                sb2.Append('}');

                this.Arguments.Add(value);

            }

            sb.Append(")");
            sb2.Append(")");
            return sb.ToString() + sb2.ToString();

        }
        /// <summary>
        /// 创建实体ＳＱＬ删除脚本
        /// </summary>
        /// <returns></returns>
        public string CreateDeleteText()
        {
            return String.Format("DELETE FROM {0} ", TableName);

        }
        /// <summary>
        /// 创建实体查询SQL脚本
        /// </summary>
        /// <returns></returns>
        public string CreateSelectText<T>(T entity) where T : Record, new()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            bool IsMush = false; 

            entity.GetValues((field, values) =>
            {
                if (IsMush)
                {
                    sb.Append(",");
                }
                else
                {
                    IsMush = true;
                }
                sb.Append(Provider.QuotePrefix);
                sb.Append(ObjectName(field));
                sb.Append(Provider.QuoteSuffix);
            });

            sb.AppendFormat(" FROM {0} ", TableName);

            return sb.ToString();
        }
        /// <summary>
        /// 创建实体更新脚本
        /// </summary>
        /// <returns></returns>
        public string CreateUpdateText<T>(T entity, string format) where T : Record, new()
        {
            if (String.IsNullOrEmpty(format))
            {
                format = "{1}";
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(" UPDATE {0} SET  ", TableName);

            bool IsMush = false;
            
            entity.GetValues((key, value) =>
            {
                if (IsMush)
                {
                    sb.Append(",");
                }
                else
                {
                    IsMush = true;
                }
                var Field = Provider.QuotePrefix + ObjectName(key) + Provider.QuoteSuffix;
                var Value = "{" + this.Arguments.Count + "}";
                sb.Append(Field);
                sb.Append('=');

                sb.AppendFormat(format, Field, Value);


                this.Arguments.Add(value);


            });
            return sb.ToString();
        }

        /// <summary>
        /// 创建实体更新脚本
        /// </summary>
        /// <returns></returns>
        public string AppendUpdateText<T>(StringBuilder sb, T entity, string format) where T : Record, new()
        {
            if (String.IsNullOrEmpty(format))
            {
                format = "{1}";
            }
            entity.GetValues((key, value) =>
            {
                sb.Append(",");

                var Field = Provider.QuotePrefix + ObjectName(key) + Provider.QuoteSuffix;
                var Value = "{" + this.Arguments.Count + "}";
                sb.Append(Field);
                sb.Append('=');

                sb.AppendFormat(format, Field, Value);


                this.Arguments.Add(value);


            });
            return sb.ToString();
        }



        public string CreateUpdateText(string format, System.Collections.IDictionary fieldValues)
        {
            if (String.IsNullOrEmpty(format))
            {
                format = "{1}";
            }

            this.Arguments.Clear();
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(" UPDATE {0} SET  ", TableName);

            var f = fieldValues.GetEnumerator();
            bool IsMush = false;
            while (f.MoveNext())
            {
                if (IsMush)
                {
                    sb.Append(",");
                }
                else
                {
                    IsMush = true;
                }
                var Field = Provider.QuotePrefix + ObjectName(f.Key as string) + Provider.QuoteSuffix;
                var Value = "{" + this.Arguments.Count + "}";

                sb.Append(Field);
                sb.Append('=');

                sb.AppendFormat(format, Field, Value);

                this.Arguments.Add(f.Value);
            }

            return sb.ToString();
        }


    }
}
