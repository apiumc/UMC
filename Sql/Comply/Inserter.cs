using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace UMC.Data.Sql
{
    class Inserter : IInserter
    {

        DbProvider dbProvider;
        ISqler sqler;
        public Inserter(Sqler sqler)
        {
            this.dbProvider = sqler.DbProvider;
            this.sqler = sqler;
        }

        bool IInserter.Execute<T>(params T[] items)
        {
            var sqlTextHelper = new EntityHelper(this.dbProvider, typeof(T).Name);

            int i = 0;
            this.sqler.Execute(sc =>
            {
                if (i < items.Length)
                {
                    sc.Reset(sqlTextHelper.CreateInsertText(items[i]), sqlTextHelper.Arguments.ToArray());
                    i++;
                    return true;
                }
                else
                {
                    return false;
                }
            }, cmd => cmd.ExecuteNonQuery());

            return false;
        }
        bool IInserter.Execute(System.Data.DataTable table)
        {
            System.Text.StringBuilder sb = new StringBuilder();
            var tablename = "{pfx}" + table.TableName;

            sb.AppendFormat("INSERT INTO {0}(", tablename); ;
            System.Text.StringBuilder sb2 = new StringBuilder();
            for (int i = 0; i < table.Columns.Count; i++)
            {
                if (i != 0)
                {
                    sb.Append(',');
                    sb2.Append(',');
                }
                sb.Append(this.dbProvider.QuotePrefix);
                sb.AppendFormat("{0}", table.Columns[i].ColumnName);
                sb.Append(this.dbProvider.QuoteSuffix);

                sb2.AppendFormat("{{{0}}}", i);
            }
            sb.AppendFormat(")VALUES({0})", sb2);
            var sqlText = sb.ToString();
            int c = -1;
            this.sqler.Execute(sc =>
            {
                c++;
                if (c < table.Rows.Count)
                {
                    sc.Reset(sqlText, table.Rows[c].ItemArray);
                    return true;
                }
                return false;
            }, cmd => cmd.ExecuteNonQuery());

            return true;
        }

        int IInserter.ExecuteSingle(object obj)
        {
            var sqlTextHelper = new EntityHelper(this.dbProvider, obj.GetType().Name);
            string sqlText = sqlTextHelper.CreateInsertText(Reflection.PropertyToDictionary(obj));
            Insert(obj);
            sqlText = sqlText + "\r\n" + this.dbProvider.GetIdentityText(sqlTextHelper.TableName);

            return Convert.ToInt32(this.sqler.ExecuteScalar(sqlText, sqlTextHelper.Arguments.ToArray()));


        }
        bool Insert(object value)
        {
            var sqlTextHelper = new EntityHelper(this.dbProvider, value.GetType().Name);
            string sqlText = sqlTextHelper.CreateInsertText(Reflection.PropertyToDictionary(value));
            return this.sqler.ExecuteNonQuery(sqlText, sqlTextHelper.Arguments.ToArray()) == 1;
        }
        int IInserter.Execute(System.Data.IDataReader reader, string table)
        {

            var tablename = "{pfx}" + table;


            System.Text.StringBuilder sb = new StringBuilder();
            sb.AppendFormat("INSERT INTO {0}(", tablename); ;

            System.Text.StringBuilder sb2 = new StringBuilder();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (i != 0)
                {
                    sb.Append(',');
                    sb2.Append(',');
                }
                sb.Append(this.dbProvider.QuotePrefix);
                sb.AppendFormat("{0}", reader.GetName(i));
                sb.Append(this.dbProvider.QuoteSuffix);
                sb2.AppendFormat("{{{0}}}", i);
            }
            sb.AppendFormat(")VALUES({0})", sb2);
            object[] objs = new object[reader.FieldCount];

            var sqlText = sb.ToString();
            int c = 0;

            this.sqler.Execute(sc =>
            {
                if (reader.Read())
                {
                    reader.GetValues(objs);

                    sc.Reset(sqlText, objs);
                    c++;
                    return true;
                }
                else
                {
                    return false;
                }
            }, cmd => cmd.ExecuteNonQuery());
            return c;
        }


        #region IInserter Members


        int IInserter.Execute(string name, params System.Collections.IDictionary[] values)
        {
            var tablename = name;


            int i = 0, c = values.Length;

            System.Text.StringBuilder sb = new StringBuilder();
            System.Text.StringBuilder sb2 = new StringBuilder();
            this.sqler.Execute(script =>
            {
                if (i < c)
                {
                    sb.Clear();
                    sb2.Clear();
                    sb.AppendFormat("INSERT INTO {0}(", tablename);
                    bool isE = false;
                    var dm = values[i].GetEnumerator();

                    var dms = new List<object>();
                    while (dm.MoveNext())
                    {
                        if (isE)
                        {
                            sb.Append(',');
                            sb2.Append(',');
                        }
                        else
                        {
                            isE = true;
                        }
                        sb.Append(this.dbProvider.QuotePrefix);
                        sb.AppendFormat("{0}", dm.Key);
                        sb.Append(this.dbProvider.QuoteSuffix);
                        sb2.AppendFormat("{{{0}}}", dms.Count);
                        dms.Add(dm.Value);
                    }
                    sb.AppendFormat(")VALUES({0})", sb2);
                    script.Reset(sb.ToString(), dms.ToArray());

                    i++;
                    return true;

                }
                return false;

            }, cmd => cmd.ExecuteNonQuery());
            return i;

        }

        #endregion
    }
}
