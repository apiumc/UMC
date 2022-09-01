using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using UMC.Data.Sql;
using UMC.Security;
namespace UMC.Data
{
    /// <summary>
    ///实体数据库访问提供器
    /// </summary>
    public sealed class Database : IDisposable
    {
        bool IsTran = false;

        /// <summary>
        /// 数据库差异配置器
        /// </summary>
        public DbProvider DbProvider
        {
            get
            {
                return DbCommonFactory.Provider;
            }
        }

        DbFactory DbCommonFactory;
        System.Data.Common.DbTransaction tran;
        /// <summary>
        /// 如果是采用事务初始化，则提交事务
        /// </summary>
        public void Commit()
        {
            if (IsTran)
            {
                if (this.DbCommonFactory.TranCmd.Transaction != null)
                {
                    this.tran.Commit();
                }
                this.DbCommonFactory.TranCmd.Connection.Close();
                this.tran.Dispose();
                this.IsTran = false;
            }
        }

        /// <summary>
        /// 打开数据库接连
        /// </summary>
        public void Open()
        {
            this.DbCommonFactory.Open();
        }
        /// <summary>
        /// 关闭数据库接连
        /// </summary>
        public void Close()
        {
            this.DbCommonFactory.Close();
        }
        /// <summary>
        /// 如果是采用事务初始化，则回退事务
        /// </summary>
        public void Rollback()
        {
            if (IsTran)
            {
                if (this.DbCommonFactory.TranCmd.Transaction != null)
                {
                    this.tran.Rollback();
                }
                this.DbCommonFactory.TranCmd.Connection.Close();
                this.tran.Dispose();

                this.IsTran = false;
            }
        }
        private Database(DbFactory DbFactory)
        {
            this.DbCommonFactory = DbFactory;
        }
        /// <summary> 
        /// 创建默认DbProvider实体访实例,默认配置节点是defaultDbProvider
        /// </summary>
        /// <returns></returns>
        public static Database Instance()
        {
            return Instance("defaultDbProvider"); ;
        }
        /// <summary>
        /// 创建DataBase
        /// </summary>
        /// <param name="Factor"></param>
        /// <returns></returns>
        public static Database Instance(DbFactory Factor)
        {
            return new Database(Factor);
        }
        public Database For(Guid appKey)
        {
            if (this.DbCommonFactory.Provider != null && this.DbCommonFactory.Provider.Provider != null)
            {
                var provider = this.DbCommonFactory.Provider.Provider;
                var provider2 = Provider.Create(provider.Name, provider.Type);
                provider2.Attributes.Add(provider.Attributes);
                provider2.Attributes["prefix"] = UMC.Data.Utility.Parse36Encode(UMC.Data.Utility.IntParse(appKey));

                return Instance(new DbFactory(Reflection.CreateObject(provider2) as DbProvider));
            }
            return this;
        }
        /// <summary>
        /// 创建默认数据库实体访实例
        /// </summary>
        /// <param name="providerName">配置节点名</param>
        /// <returns></returns>
        public static Database Instance(string providerName)
        {

            var node = Reflection.GetDataProvider("database", providerName);//, Reflection.Instance().AppKey());
            if (node == null)
            {
                throw new Data.Sql.DbException(new Exception(String.Format("未配置“{0}”", providerName)), null);
            }
            var provider = Reflection.CreateObject(node) as DbProvider;
            return new Database(new DbFactory(provider));
        }


        /// <summary>
        /// 使用事务，如果已经使用了事务，则返回false，如果没有使用事务，则取用事务并返回true
        /// </summary>
        /// <returns></returns>
        public bool BeginTransaction()
        {
            return this.BeginTransaction(System.Data.IsolationLevel.Unspecified);
        }
        /// <summary>
        /// 使用事务，如果已经使用了事务，则返回false，如果没有使用事务，则取用事务并返回true
        /// </summary>
        /// <returns></returns>
        public bool BeginTransaction(System.Data.IsolationLevel lev)
        {
            if (IsTran == false)
            {
                this.tran = this.DbCommonFactory.UseTran(lev);
                this.IsTran = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 创建的Sql语句的查询器
        /// </summary>
        /// <returns></returns>
        public ISqler Sqler()
        {
            return DbCommonFactory.Sqler();
        }
        /// <summary>
        /// 创建的Sql语句的查询器
        /// </summary>
        /// <param name="TimeOut">超时时间</param>
        /// <returns></returns>
        public ISqler Sqler(int TimeOut)
        {
            return DbCommonFactory.Sqler(TimeOut);
        }
        /// <summary>
        /// 创建的Sql语句的查询器
        /// </summary>
        /// <param name="pfx">是否自动添加表前缀</param>
        /// <returns></returns>
        public ISqler Sqler(bool pfx)
        {
            return DbCommonFactory.Sqler(pfx);
        }

        /// <summary>
        /// 创建实体综合管理适配器
        /// </summary>
        /// <returns></returns>
        public IObjectEntity<T> ObjectEntity<T>() where T : class
        {
            return DbCommonFactory.ObjectEntity<T>();// (TimeOut);
        }

        /// <summary>
        /// 创建实体综合管理适配器
        /// </summary>
        /// <returns></returns>
        public IObjectEntity<T> ObjectEntity<T>(string tabName) where T : class
        {
            return DbCommonFactory.ObjectEntity<T>(tabName);
        }

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            ((IDisposable)DbCommonFactory).Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}







