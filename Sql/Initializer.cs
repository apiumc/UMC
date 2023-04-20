using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;

namespace UMC.Data.Sql
{
    public abstract class Initializer
    {

        class EqualityComparer : IEqualityComparer<Initializer>
        {

            public bool Equals(Initializer x, Initializer y)
            {
                return StringComparer.CurrentCulture.Equals(x.Name, y.Name);

            }

            public int GetHashCode(Initializer obj)
            {
                return obj.GetHashCode();
            }
        }
        static HashSet<Initializer> _Initializers = new HashSet<Initializer>(new EqualityComparer());

        public static HashSet<Initializer> Initializers
        {
            get
            {
                return _Initializers;
            }
        }


        public abstract string Name
        {
            get;
        }
        public abstract string Caption
        {
            get;
        }

        public static ProviderConfiguration Register(params Initializer[] initializers)
        {
            var Setup = Reflection.Configuration("setup") ?? new ProviderConfiguration();
            foreach (var initializer in initializers)
            {
                _Initializers.Add(initializer);
                if (Setup.ContainsKey(initializer.Name) == false)
                {
                    Setup.Add(UMC.Data.Provider.Create(initializer.Name, initializer.GetType().FullName));
                }
            }
            
            var isSetup = false;
            foreach (var init in initializers)
            {
                var setuper = Setup[init.Name];
                if (String.Equals(setuper.Attributes["setup"], "true") == false)
                {
                    isSetup = true;
                    init.Setup(new UMC.Data.CSV.Log($"安装{init.Caption}"));
                    setuper.Attributes["setup"] = "true";
                }
            }
            if (isSetup)
            {
                Reflection.Configuration("setup", Setup);
            }
            
            return Setup;
        }

        public virtual string ProviderName => String.Empty;

        /// <summary>
        /// 安装后初始化
        /// </summary>
        /// <param name="hash"></param>
        public virtual void Setup(CSV.Log log)
        {
        }
        public virtual void Upgrade(CSV.Log log)
        {

        }


        private IDictionary<Record, string[]> dictionary = new Dictionary<Record, string[]>();
        private List<Record> _copy = new List<Record>();
        /// <summary>
        /// 主键
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        protected void Setup(Record key) //where T : Record
        {
            dictionary.Add(key, null);
        }
        protected void Setup(Record key, Record text)//where T : Record
        {
            dictionary.Add(key, GetValues(text).Keys.ToArray());

        }
        protected void Setup(Record key, params string[] fields)//where T : Record
        {
            dictionary.Add(key, fields);

        }
        protected void Copy(Record type)
        {
            _copy.Add(type);

        }
        /// <summary>
        /// 还原
        /// </summary>
        public void Restore(CSV.Log log, DbProvider provider, string file)
        {
            SQLiteDbProvider sqlite = new SQLiteDbProvider();
            sqlite.Provider = Provider.Create(provider.Provider.Name, sqlite.GetType().FullName);
            sqlite.Provider.Attributes["db"] = file;
            var em = dictionary.GetEnumerator();
            var Delimiter = provider.Delimiter;
            DbFactory dsource = new DbFactory(sqlite);
            DbFactory target = new DbFactory(provider);
            var targetSqler = target.Sqler(false);
            var sourceSqler = dsource.Sqler();

            while (em.MoveNext())
            {
                var value = em.Current.Value;

                var tabName = em.Current.Key.GetType().Name;
                log.Info("还原表", tabName);
                var fields = new List<String>(); ;
                var insertSql = new StringBuilder();
                insertSql.Append("INSERT INTO ");
                insertSql.Append(GetName(provider, tabName));
                insertSql.Append("(");
                foreach (var f in em.Current.Key.GetType().GetProperties())
                {
                    insertSql.AppendFormat("{0}{1}{2},", provider.QuotePrefix, f.Name, provider.QuoteSuffix);
                    fields.Add(f.Name);
                }
                if (value != null)
                {
                    if (value.GetType().IsArray)
                    {
                        var fs = (string[])value;
                        foreach (var f in fs)
                        {
                            insertSql.AppendFormat("{0}{1}{2},", provider.QuotePrefix, f, provider.QuoteSuffix);
                            fields.Add(f);
                        }
                    }
                }
                insertSql.Remove(insertSql.Length - 1, 1);
                insertSql.Append(")VALUES(");
                for (var i = 0; i < fields.Count; i++)
                {
                    insertSql.Append("{");
                    insertSql.Append(i);
                    insertSql.Append("},");
                }
                insertSql.Remove(insertSql.Length - 1, 1);
                insertSql.Append(")");
                targetSqler.ExecuteNonQuery("DELETE FROM " + GetName(provider, tabName));
                sourceSqler.Execute(String.Format("SELECT *FROM {0}", tabName), dr =>
                {
                    targetSqler.Execute(s =>
                    {
                        var valueStrSQL = insertSql.ToString();
                        while (dr.Read())
                        {
                            var values = new List<Object>();

                            for (int d = 0; d < fields.Count; d++)
                            {
                                values.Add(dr[fields[d]]);
                            }
                            s.Reset(valueStrSQL, values.ToArray());
                            return true;
                        }
                        return false;
                    }, cmd =>
                    {
                        cmd.ExecuteNonQuery();
                    });


                });


            }
            dsource.Close();
            target.Close();
        }
        /// <summary>
        /// 备份
        /// </summary>
        public void BackUp(CSV.Log log, DbProvider provider, string file)
        {
            DbFactory dsource = new DbFactory(provider);
            SQLiteDbProvider sqlite = new SQLiteDbProvider();
            sqlite.Provider = Provider.Create(provider.Provider.Name, sqlite.GetType().FullName);
            sqlite.Provider.Attributes["db"] = file;
            var em = dictionary.GetEnumerator();
            var Delimiter = provider.Delimiter;
            DbFactory target = new DbFactory(sqlite);
            var targetSqler = target.Sqler();
            var sourceSqler = dsource.Sqler();

            while (em.MoveNext())
            {
                CreateTable(target.Sqler(), sqlite, log, em.Current.Key, em.Current.Value);
                var tabName = em.Current.Key.GetType().Name;

                var fields = new List<String>(); ;
                var insertSql = new StringBuilder();
                insertSql.Append("INSERT INTO ");
                insertSql.Append(tabName);
                insertSql.Append("(");
                foreach (var f in em.Current.Key.GetColumns())
                {
                    insertSql.AppendFormat("{0}{1}{2},", sqlite.QuotePrefix, f.Name, sqlite.QuoteSuffix);
                    fields.Add(f.Name);
                }
                var value = em.Current.Value;
                if (value != null)
                {
                    foreach (var f in value)
                    {
                        insertSql.AppendFormat("{0}{1}{2},", sqlite.QuotePrefix, f, sqlite.QuoteSuffix);
                        fields.Add(f);
                    }
                }
                insertSql.Remove(insertSql.Length - 1, 1);
                insertSql.Append(")VALUES(");
                for (var i = 0; i < fields.Count; i++)
                {
                    insertSql.Append("{");
                    insertSql.Append(i);
                    insertSql.Append("},");
                }
                insertSql.Remove(insertSql.Length - 1, 1);
                insertSql.Append(")");

                sourceSqler.Execute(String.Format("SELECT *FROM {0}", tabName), dr =>
                {
                    System.Data.Common.DbTransaction transaction = null;
                    targetSqler.Execute(s =>
                    {
                        var valueStrSQL = insertSql.ToString();
                        while (dr.Read())
                        {
                            var values = new List<Object>();

                            for (int d = 0; d < fields.Count; d++)
                            {
                                values.Add(dr[fields[d]]);
                            }
                            s.Reset(valueStrSQL, values.ToArray());
                            return true;
                        }
                        if (transaction != null)
                        {
                            transaction.Commit();


                        }
                        return false;
                    }, cmd =>
                    {
                        if (transaction == null)
                        {
                            transaction = cmd.Connection.BeginTransaction();
                        }
                        cmd.ExecuteNonQuery();
                    });


                });


            }
            target.Close();
            dsource.Close();
        }

        public void Drop(CSV.Log log, DbProvider provider)
        {

            var factory = new DbFactory(provider);
            var sqler = factory.Sqler(1, false);

            var em = dictionary.GetEnumerator();
            var Delimiter = provider.Delimiter;

            while (em.MoveNext())
            {
                var tabName = em.Current.Key.GetType().Name;
                log.Info("删除表", tabName);
                tabName = GetName(provider, tabName);
                var sb = new StringBuilder();
                sb.Append("DROP TABLE ");
                sb.Append(tabName);
                try
                {
                    sqler.ExecuteNonQuery(sb.ToString());
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                }


            }
        }
        static string GetName(DbProvider provider, string tabName)
        {
            var Delimiter = provider.Delimiter;

            if (String.IsNullOrEmpty(Delimiter))
            {
                tabName = String.Format("{0}{3}{2}", provider.QuotePrefix, provider.Prefixion, provider.QuoteSuffix, provider.Builder.Column(tabName));
            }
            else
            {
                if (String.IsNullOrEmpty(provider.Prefixion))
                {
                    tabName = String.Format("{0}{1}{2}", provider.QuotePrefix, tabName, provider.QuoteSuffix);
                }
                else
                {
                    switch (Delimiter)
                    {
                        case ".":
                            tabName = String.Format("{0}{1}{2}.{0}{3}{2}", provider.QuotePrefix, provider.Prefixion, provider.QuoteSuffix, provider.Builder.Column(tabName));
                            break;
                        default:
                            tabName = String.Format("{0}{1}{4}{3}{2}", provider.QuotePrefix, provider.Prefixion, provider.QuoteSuffix, provider.Builder.Column(tabName), Delimiter);
                            break;
                    }
                }

            }
            return tabName;
        }
        public void Upgrade(CSV.Log log, DbProvider provider)
        {

            var builder = provider.Builder;
            if (builder == null)
            {
                log.Debug("此数据库节点配置器未有管理器");
                return;
            }
            var factory = new DbFactory(provider);
            var sqler = factory.Sqler(1, false);

            var em = dictionary.GetEnumerator();

            while (em.MoveNext())
            {
                CheckTable(sqler, provider, log, em.Current.Key, em.Current.Value);
            }

        }

        public void Copy(CSV.Log log, DbProvider provider)
        {
            var sqler = new DbFactory(provider).Sqler(0, false);
            foreach (var type in _copy)
            {
                var tabName = provider.Builder.Column(type.GetType().Name);
                log.Info("复制表", tabName);
                var sName = tabName;
                var Delimiter = provider.Delimiter;
                if (String.IsNullOrEmpty(Delimiter))
                {
                    log.Info("当前配置不支持复制表数据", tabName);
                    return;
                }
                else
                {
                    switch (Delimiter)
                    {
                        case ".":
                            tabName = String.Format("{0}{1}{2}.{0}{3}{2}", provider.QuotePrefix, provider.Prefixion, provider.QuoteSuffix, tabName);
                            sName = String.Format("{0}{1}{2}.{0}{3}{2}", provider.QuotePrefix, provider.Prefixion, provider.QuoteSuffix, sName);
                            break;
                        default:
                            tabName = String.Format("{0}{1}{4}{3}{2}", provider.QuotePrefix, provider.Prefixion, provider.QuoteSuffix, tabName, Delimiter);
                            sName = String.Format("{0}{1}{4}{3}{2}", provider.QuotePrefix, provider.Prefixion, provider.QuoteSuffix, sName, Delimiter);
                            break;
                    }
                }

                var sb = new StringBuilder();
                var ps = type.GetColumns();
                foreach (var property in ps)
                {
                    sb.AppendFormat("{0}{3}{1}", provider.QuotePrefix, provider.Prefixion, provider.QuoteSuffix, provider.Builder.Column(property.Name));
                    sb.Append(",");
                }
                sb.Remove(sb.Length - 1, 1);
                try
                {
                    sqler.ExecuteNonQuery(String.Format("INSERT INTO {0}({1})SELECT {1} FROM {2}", tabName, sb, sName));
                }
                catch (Exception ex)
                {
                    log.Error("复制出错", ex.Message);
                }
            }
        }
        void CheckTable(ISqler sqler, DbProvider provider, CSV.Log log, Record key, string[] textFields)
        {


            var tabName = key.GetType().Name;
            log.Info("检测表", tabName);

            var builder = provider.Builder;


            tabName = GetName(provider, tabName);

            var checkName = tabName;
            if (String.IsNullOrEmpty(provider.QuotePrefix) == false)
            {
                checkName = checkName.Replace(provider.QuotePrefix, "");
            }
            if (String.IsNullOrEmpty(provider.QuoteSuffix) == false)
            {
                checkName = checkName.Replace(provider.QuoteSuffix, "");
            }
            if (builder.Check(checkName, sqler) == false)
            {
                CreateTable(sqler, provider, log, key, textFields);
            }
            else
            {
                var ps = key.GetColumns();
                foreach (var property in ps)
                {
                    var filed = String.Format("{0}{2}{1} ", provider.QuotePrefix, provider.QuoteSuffix, builder.Column(property.Name));
                    var cfiled = builder.Column(filed);

                    var checkField = cfiled;
                    if (String.IsNullOrEmpty(provider.QuotePrefix) == false)
                    {
                        checkField = checkField.Replace(provider.QuotePrefix, "");
                    }
                    if (String.IsNullOrEmpty(provider.QuoteSuffix) == false)
                    {
                        checkField = checkField.Replace(provider.QuoteSuffix, "");
                    }
                    if (builder.Check(checkName.Trim(), checkField.Trim(), sqler) == false)
                    {
                        var sb = new StringBuilder();
                        var type = property.Type;
                        switch (type.FullName)
                        {
                            case "System.SByte":
                            case "System.Byte":
                            case "System.Int16":
                            case "System.UInt16":
                            case "System.Int32":
                            case "System.UInt32":
                                sb.Append(builder.AddColumn(tabName, cfiled, builder.Integer()));
                                break;
                            case "System.Double":
                            case "System.Single":
                                sb.Append(builder.AddColumn(tabName, cfiled, builder.Float()));
                                break;
                            case "System.Int64":
                            case "System.UInt64":
                            case "System.Decimal":
                                sb.Append(builder.AddColumn(tabName, cfiled, builder.Number()));
                                break;
                            case "System.Boolean":
                                sb.Append(builder.AddColumn(tabName, cfiled, builder.Boolean()));
                                break;
                            case "System.DateTime":
                                sb.Append(builder.AddColumn(tabName, cfiled, builder.Date()));
                                break;
                            case "System.Guid":
                                sb.Append(builder.AddColumn(tabName, cfiled, builder.Guid()));
                                break;
                            default:
                                if (type.IsEnum)
                                {
                                    sb.Append(builder.AddColumn(tabName, cfiled, builder.Integer()));
                                }
                                else if (type == typeof(byte[]))
                                {
                                    sb.Append(builder.AddColumn(tabName, cfiled, builder.Binary()));
                                }
                                else if (textFields.Contains(property.Name))
                                {

                                    sb.Append(builder.AddColumn(tabName, cfiled, builder.Text()));
                                }
                                else
                                {
                                    sb.Append(builder.AddColumn(tabName, cfiled, builder.String()));
                                }
                                break;
                        }
                        if (sb.Length > 0)
                        {
                            log.Info("追加表字段", tabName + '.' + cfiled);
                            ExecuteNonQuery(log, sqler, sb.ToString());
                        }
                    }
                }
            }

        }
        void ExecuteNonQuery(CSV.Log log, ISqler sqler, string sb)
        {
            try
            {
                sqler.ExecuteNonQuery(sb.ToString());
            }
            catch (Exception ex)
            {
                UMC.Data.Utility.Debug("Setup", sb.ToCharArray());
                log.Error(ex.Message);
            }


        }
        static Dictionary<String, Object> GetValues(Record record)
        {
            var vs = new Dictionary<String, Object>();
            record.GetValues((t, v) => vs.Add(t, v));
            return vs;
        }
        public static void Create(Database database, Record key, Record value)
        {
            CreateTable(database.Sqler(), database.DbProvider, new CSV.Log(key.GetType().Name), key, GetValues(value).Keys.ToArray());
        }
        static void CreateTable(ISqler sqler, DbProvider provider, CSV.Log log, Record key, string[] textFeilds)
        {


            var tabName = key.GetType().Name;
            log.Info("创建表", tabName);

            var keys = GetValues(key);

            var Delimiter = provider.Delimiter;
            var builder = provider.Builder;

            if (String.IsNullOrEmpty(Delimiter))
            {
                tabName = String.Format("{0}{3}{2}", provider.QuotePrefix, provider.Prefixion, provider.QuoteSuffix, tabName);
            }
            else
            {
                if (String.IsNullOrEmpty(provider.Prefixion))
                {
                    tabName = String.Format("{0}{1}{2}", provider.QuotePrefix, tabName, provider.QuoteSuffix);
                }
                else
                {
                    switch (Delimiter)
                    {
                        case ".":
                            tabName = String.Format("{0}{1}{2}.{0}{3}{2}", provider.QuotePrefix, provider.Prefixion, provider.QuoteSuffix, tabName);
                            break;
                        default:
                            tabName = String.Format("{0}{1}{4}{3}{2}", provider.QuotePrefix, provider.Prefixion, provider.QuoteSuffix, tabName, Delimiter);
                            break;
                    }
                }

            }
            var sb = new StringBuilder();
            sb.Append("CREATE TABLE ");
            sb.Append(tabName);
            sb.Append("(");
            var ps = key.GetColumns();
            foreach (var property in ps)
            {
                var filed = String.Format("{0}{2}{1} ", provider.QuotePrefix, provider.QuoteSuffix, property.Name);

                sb.Append(builder.Column(filed));
                var type = property.Type;

                switch (type.FullName)
                {
                    case "System.SByte":
                    case "System.Byte":
                    case "System.Int16":
                    case "System.UInt16":
                    case "System.Int32":
                    case "System.UInt32":
                        sb.Append(builder.Integer());
                        break;
                    case "System.Double":
                    case "System.Single":
                        sb.Append(builder.Float());
                        break;
                    case "System.Int64":
                    case "System.UInt64":
                    case "System.Decimal":
                        sb.Append(builder.Number());
                        break;
                    case "System.Boolean":
                        sb.Append(builder.Boolean());
                        break;
                    case "System.DateTime":
                        sb.Append(builder.Date());
                        break;
                    case "System.Guid":
                        sb.Append(builder.Guid());
                        break;
                    default:

                        if (property.Type.IsEnum)
                        {
                            sb.Append(builder.Integer());
                        }
                        else if (type == typeof(byte[]))
                        {
                            sb.Append(builder.Binary());
                        }
                        else if (textFeilds.Contains(property.Name))
                        {

                            sb.Append(builder.Text());
                        }
                        else
                        {
                            sb.Append(builder.String());
                        }
                        break;
                }
                if (keys.ContainsKey(property.Name))
                {
                    sb.Append(" NOT NULL");
                }
                sb.Append(",");
            }
            sb.Remove(sb.Length - 1, 1);

            sb.Append(")");
            try
            {
                sqler.ExecuteNonQuery(sb.ToString());
            }
            catch (Exception ex)
            {
                UMC.Data.Utility.Debug("Setup", sb.ToString());
                log.Error(ex.Message);
            }
            if (keys.Count > 0)
            {
                var ids = new List<String>();
                var m = keys.GetEnumerator();
                while (m.MoveNext())
                {
                    var filed = String.Format("{0}{1}{2}", provider.QuotePrefix, m.Current.Key, provider.QuoteSuffix);
                    ids.Add(filed);
                }
                var sql = builder.PrimaryKey(tabName, ids.ToArray());
                if (String.IsNullOrEmpty(sql) == false)
                {
                    try
                    {
                        sqler.ExecuteNonQuery(sql);
                    }
                    catch (Exception ex)
                    {
                        log.Error("创建主键" + String.Join(",", ids.ToArray()), ex.Message);
                    }
                }
            }


        }
        public void Setup(CSV.Log log, DbProvider provider)
        {
            var builder = provider.Builder;
            if (builder == null)
            {
                log.Debug("此数据库节点配置器未有管理器");
                log.End("操作结束");
                return;
            }
            var factory = new DbFactory(provider);
            var sqler = factory.Sqler(1, false);

            var em = dictionary.GetEnumerator();
            var Delimiter = provider.Delimiter;
            log.Info("数据前缀", provider.Prefixion);
            if (String.Equals(Delimiter, ".") && String.IsNullOrEmpty(provider.Prefixion) == false)
            {
                var prefixion = String.Format("{0}{1}{2}", provider.QuotePrefix, provider.Prefixion, provider.QuoteSuffix);
                var schemaSQL = builder.Schema(prefixion);
                if (String.IsNullOrEmpty(schemaSQL) == false)
                {
                    try
                    {
                        sqler.ExecuteNonQuery(schemaSQL);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message);
                    }
                }
            }

            while (em.MoveNext())
            {
                CreateTable(sqler, provider, log, em.Current.Key, em.Current.Value);
            }

        }
    }

}
