using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace UMC.Data.Sql
{
    class Grouper<T> : IGrouper<T> where T : Record, new()
    {
        class GroupKey
        {
            public string Field
            {
                get;
                set;
            }
            public string Formula
            {
                get;
                set;
            }
            public string Name
            {
                get;
                set;
            }
        }
        string[] fields;

        public Grouper(Sqler sqler, EntityHelper helper, Conditions<T> query, params string[] fields)
        {
            this.sqler = sqler;
            this.query = query;
            this.helper = helper;
            this.fields = fields;
            this.seq = new GSequencer<T>(this);
        }
        GSequencer<T> seq;
        Sqler sqler;
        EntityHelper helper;
        Conditions<T> query;

        List<GroupKey> GroupKeys = new List<GroupKey>();
        object[] Format(StringBuilder sb)
        {
            var provider = this.sqler.DbProvider;
            sb.Append("SELECT ");
            var IsB = false;
            foreach (var field in this.fields)
            {
                if (IsB)
                {
                    sb.Append(',');
                }
                else
                {
                    IsB = true;
                }
                sb.AppendFormat("{0}{1}{2}", provider.QuotePrefix, field, provider.QuoteSuffix);
            }
            int ctime = 1;
            for (var i = 0; i < this.GroupKeys.Count; i++)
            {
                var group = this.GroupKeys[i];
                if (IsB)
                {
                    sb.Append(',');
                }
                else
                {
                    IsB = true;
                }
                if (String.IsNullOrEmpty(group.Name))
                {
                    switch (group.Field)
                    {
                        case "*":
                            sb.AppendFormat("{0}({1}) AS G{2}", group.Formula, group.Field, ctime);
                            ctime++;
                            break;
                        default:
                            sb.AppendFormat("{0}({2}) AS G{4}", group.Formula, provider.QuotePrefix, group.Field, provider.QuoteSuffix, ctime);
                            ctime++;
                            break;
                    }
                }
                else
                {
                    switch (group.Field)
                    {
                        case "*":
                            sb.AppendFormat("{0}(*) AS {1}{4}{3}", group.Formula, provider.QuotePrefix, group.Field, provider.QuoteSuffix, group.Name);
                            break;
                        default:
                            sb.AppendFormat("{0}({1}{2}{3}) AS {1}{4}{3}", group.Formula, provider.QuotePrefix, group.Field, provider.QuoteSuffix, group.Name);
                            break;
                    }
                }

            }
            sb.AppendFormat(" FROM {0} ", this.helper.TableName);

            var li = this.query.FormatSqlText(sb, new List<object>(), this.sqler.DbProvider);
            if (this.fields.Length > 0)
            {
                sb.Append(" GROUP BY ");
                for (var i = 0; i < this.fields.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(',');
                    }
                    sb.AppendFormat("{0}{1}{2}", provider.QuotePrefix, this.fields[i], provider.QuoteSuffix);
                }
            }
            return li.ToArray();
        }

        #region IGrouper<T> Members

        IGrouper<T> IGrouper<T>.Count()
        {
            Group("*", "COUNT");
            return this;
            ;
        }

        IGrouper<T> IGrouper<T>.Sum(string field)
        {
            Group(field, "SUM");
            return this;
        }

        IGrouper<T> IGrouper<T>.Avg(string field)
        {
            Group(field, "AVG");
            return this;
        }

        IGrouper<T> IGrouper<T>.Max(string field)
        {
            Group(field, "MAX");
            return this;
        }

        IGrouper<T> IGrouper<T>.Min(string field)
        {
            Group(field, "MIN");
            return this;
        }

        void Group(T field, string Formula)
        {
            field.GetValues((t, v) =>
            {
                this.GroupKeys.Add(new GroupKey { Field = t, Name = t, Formula = Formula });

            });

        }
        void Group(string field, string Formula, String asName)
        {
            this.GroupKeys.Add(new GroupKey { Field = field, Formula = Formula, Name = asName });

        }
        void Group(string field, string Formula)
        {
            this.GroupKeys.Add(new GroupKey { Field = field, Formula = Formula });

        }
        IGrouper<T> IGrouper<T>.Sum(T field)
        {
            Group(field, "SUM");
            return this;
        }

        IGrouper<T> IGrouper<T>.Avg(T field)
        {
            Group(field, "AVG");
            return this;
        }

        IGrouper<T> IGrouper<T>.Max(T field)
        {
            Group(field, "MAX");
            return this;

        }

        IGrouper<T> IGrouper<T>.Min(T field)
        {
            Group(field, "MIN");
            return this;
        }

        System.Data.DataTable IGrouper<T>.Query()
        {
            var sb = new StringBuilder();
            var agrs = this.Format(sb);
            this.seq.FormatSqlText(sb);

            ISqler sqer = this.sqler;
            this.script = Script.Create(sb.ToString(), agrs);
            return sqer.ExecuteTable(this.script.Text, this.script.Arguments);
        }
        IGroupOrder<T> IGrouper<T>.Order
        {
            get { return this.seq; }
        }

        #endregion
        Script script;

        #region IScript Members

        Script IScript.SQL
        {
            get { return this.script; }
        }

        #endregion

        #region IGrouper<T> Members


        T IGrouper<T>.Single()
        {
            var sb = new StringBuilder();
            var agrs = this.Format(sb);
            this.seq.FormatSqlText(sb);

            ISqler sqer = this.sqler;
            this.script = Script.Create(sb.ToString(), agrs);
            return sqer.ExecuteSingle<T>(this.script.Text, this.script.Arguments);
        }

        void IGrouper<T>.Query(DataReader<T> reader)
        {
            var sb = new StringBuilder();
            var agrs = this.Format(sb);
            this.seq.FormatSqlText(sb);

            ISqler sqer = this.sqler;
            this.script = Script.Create(sb.ToString(), agrs);
            sqer.Execute<T>(this.script.Text, reader, this.script.Arguments);

        }



        IGrouper<T> IGrouper<T>.Count(T field)
        {
            field.GetValues((t, v) =>
             {
                 this.GroupKeys.Add(new GroupKey { Field = "*", Name = t, Formula = "COUNT" });
             });
            return this;
        }

        IGrouper<T> IGrouper<T>.Count(string asName)
        {
            Group("*", "COUNT", asName);
            return this;
        }

        IGrouper<T> IGrouper<T>.Sum(string field, string asName)
        {
            Group(field, "SUM", asName);
            return this;
        }

        IGrouper<T> IGrouper<T>.Avg(string field, string asName)
        {
            Group(field, "AVG", asName);
            return this;
        }

        IGrouper<T> IGrouper<T>.Max(string field, string asName)
        {
            Group(field, "MAX", asName);
            return this;
        }

        IGrouper<T> IGrouper<T>.Min(string field, string asName)
        {
            Group(field, "MIN", asName);
            return this;
        }

        #endregion
    }
}
