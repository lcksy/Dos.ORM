﻿/*************************************************************************
 * 
 * Hxj.Data
 * 
 * 2010-2-10
 * 
 * steven hu   
 *  
 * Support: http://www.cnblogs.com/huxj
 *   
 * 
 * Change History:
 * 
 * 
**************************************************************************/

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Dos.ORM.Common;
using Dos;
using Dos.ORM;
using Dos.ORM.Common;

namespace Dos.ORM
{
    #region DataType

    /// <summary>
    /// Type of a database.
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// SQL Server 2000
        /// </summary>
        SqlServer = 0,
        /// <summary>
        /// MsAccess
        /// </summary>
        MsAccess = 1,
        /// <summary>
        /// SQL Server 2005
        /// </summary>
        SqlServer9 = 2,
        /// <summary>
        /// Oracle
        /// </summary>
        Oracle = 3,
        /// <summary>
        /// Sqlite
        /// </summary>
        Sqlite3 = 4,
        /// <summary>
        /// MySql
        /// </summary>
        MySql = 5
    }

    #endregion

    /// <summary>
    /// DbSession
    /// </summary>
    public sealed class DbSession
    {

        /// <summary>
        /// 版本号
        /// </summary>
        //public const string Version = "1.9.8.6";


        /// <summary>
        /// 
        /// </summary>
        private Database db;

        /// <summary>
        /// 
        /// </summary>
        private CommandCreator cmdCreator;


        #region Cache

        /// <summary>
        /// 开启缓存
        /// </summary>
        public void TurnOnCache()
        {
            if (null != db.DbProvider.CacheConfig)
            {
                db.DbProvider.CacheConfig.Enable = true;
            }
        }


        /// <summary>
        /// 关闭缓存
        /// </summary>
        public void TurnOffCache()
        {
            if (null != db.DbProvider.CacheConfig)
            {
                db.DbProvider.CacheConfig.Enable = false;
            }
        }

        #endregion

        #region batch

        /// <summary>
        /// 开始批处理，默认10条sql组合
        /// </summary>
        public DbBatch BeginBatchConnection()
        {
            return BeginBatchConnection(10);
        }

        /// <summary>
        /// 开始批处理
        /// </summary>
        /// <param name="batchSize">sql组合条数</param>
        public DbBatch BeginBatchConnection(int batchSize)
        {
            return new DbBatch(cmdCreator, new BatchCommander(db, batchSize));
        }

        /// <summary>
        /// 开始批处理
        /// </summary>
        /// <param name="batchSize">sql组合条数</param>
        /// <param name="tran">事务</param>
        public DbBatch BeginBatchConnection(int batchSize, DbTransaction tran)
        {
            return new DbBatch(cmdCreator, new BatchCommander(db, batchSize, tran));
        }

        /// <summary>
        /// 开始批处理
        /// </summary>
        /// <param name="batchSize">sql组合条数</param>
        /// <param name="il">事务</param>
        public DbBatch BeginBatchConnection(int batchSize, IsolationLevel il)
        {
            return new DbBatch(cmdCreator, new BatchCommander(db, batchSize, il));
        }


        #endregion

        #region Default


        /// <summary>
        /// Get the default gateway, which mapping to the default Database.
        /// </summary>
        public static DbSession Default = new DbSession(Database.Default);

        /// <summary>
        /// Sets the default DbSession.
        /// </summary>
        /// <param name="dt">The dt.</param>
        /// <param name="connStr">The conn STR.</param>
        public static void SetDefault(DatabaseType dt, string connStr)
        {
            DbProvider provider = CreateDbProvider(dt, connStr);

            Default = new DbSession(new Database(provider));
        }

        /// <summary>
        /// Creates the db provider.
        /// </summary>
        /// <param name="dt">The dt.</param>
        /// <param name="connStr">The conn STR.</param>
        /// <returns>The db provider.</returns>
        private static DbProvider CreateDbProvider(DatabaseType dt, string connStr)
        {
            DbProvider provider = null;
            if (dt == DatabaseType.SqlServer9)
            {
                provider = ProviderFactory.CreateDbProvider(null, typeof(Dos.ORM.SqlServer9.SqlServer9Provider).FullName, connStr);
            }
            else if (dt == DatabaseType.SqlServer)
            {
                provider = ProviderFactory.CreateDbProvider(null, typeof(Dos.ORM.SqlServer.SqlServerProvider).FullName, connStr);
            }
            else if (dt == DatabaseType.Oracle)
            {
                provider = ProviderFactory.CreateDbProvider(null, typeof(Dos.ORM.Oracle.OracleProvider).FullName, connStr);
            }
            else if (dt == DatabaseType.MySql)
            {
                provider = ProviderFactory.CreateDbProvider("Dos.ORM.MySql", "Dos.ORM.MySql.MySqlProvider", connStr);
            }
            else if (dt == DatabaseType.Sqlite3)
            {
                provider = ProviderFactory.CreateDbProvider("Dos.ORM.Sqlite", "Dos.ORM.Sqlite.SqliteProvider", connStr);
            }
            else if (dt == DatabaseType.MsAccess)
            {
                provider = ProviderFactory.CreateDbProvider(null, typeof(Dos.ORM.MsAccess.MsAccessProvider).FullName, connStr);
            }
            return provider;
        }

        /// <summary>
        /// Sets the default DbSession.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="className">Name of the class.</param>
        /// <param name="connStr">The conn STR.</param>
        public static void SetDefault(string assemblyName, string className, string connStr)
        {
            DbProvider provider = ProviderFactory.CreateDbProvider(assemblyName, className, connStr);
            if (provider == null)
            {
                throw new NotSupportedException(string.Format("Cannot construct DbProvider by specified parameters: {0}, {1}, {2}",
                    assemblyName, className, connStr));
            }

            Default = new DbSession(new Database(provider));
        }

        /// <summary>
        /// Sets the default DbSession.
        /// </summary>
        /// <param name="connStrName">Name of the conn STR.</param>
        public static void SetDefault(string connStrName)
        {
            DbProvider provider = ProviderFactory.CreateDbProvider(connStrName);
            provider.ConnectionStringsName = connStrName;
            if (provider == null)
            {
                throw new NotSupportedException(string.Format("Cannot construct DbProvider by specified ConnectionStringName: {0}", connStrName));
            }

            Default = new DbSession(new Database(provider));
        }

        #endregion

        #region 构造函数



        private void initDbSesion()
        {
            cmdCreator = new CommandCreator(db);

            object cacheConfig = System.Configuration.ConfigurationManager.GetSection("DosCacheConfig");

            if (null != cacheConfig)
            {
                db.DbProvider.CacheConfig = (CacheConfiguration)cacheConfig;

                Dictionary<string, CacheInfo> entitiesCache = new Dictionary<string, CacheInfo>();

                //获取缓存配制
                foreach (string key in db.DbProvider.CacheConfig.Entities.AllKeys)
                {
                    if (key.IndexOf('.') > 0)
                    {
                        string[] splittedKey = key.Split('.');
                        if (splittedKey[0].Trim() == db.DbProvider.ConnectionStringsName)
                        {
                            int expireSeconds = 0;
                            CacheInfo cacheInfo = new CacheInfo();
                            if (int.TryParse(db.DbProvider.CacheConfig.Entities[key].Value, out expireSeconds))
                            {
                                cacheInfo.TimeOut = expireSeconds;
                            }
                            else
                            {
                                string tempFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, db.DbProvider.CacheConfig.Entities[key].Value);
                                if (File.Exists(tempFilePath))
                                {
                                    cacheInfo.FilePath = tempFilePath;
                                }

                            }

                            if (!cacheInfo.IsNullOrEmpty())
                            {
                                string entityName = string.Concat(db.DbProvider.ConnectionStringsName, splittedKey[1].Trim());
                                if (entitiesCache.ContainsKey(entityName))
                                    entitiesCache.Remove(entityName);

                                entitiesCache.Add(entityName, cacheInfo);
                            }
                        }
                    }
                }

                db.DbProvider.EntitiesCache = entitiesCache;
            }


        }


        /// <summary>
        /// 构造函数    使用默认  DbSession.Default
        /// </summary>
        public DbSession()
        {
            db = Database.Default;

            initDbSesion();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connStrName">config文件中connectionStrings节点的name</param>
        public DbSession(string connStrName)
        {
            this.db = new Database(ProviderFactory.CreateDbProvider(connStrName));
            this.db.DbProvider.ConnectionStringsName = connStrName;
            initDbSesion();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="db">已知的Database</param>
        public DbSession(Database db)
        {
            this.db = db;

            initDbSesion();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dt">数据库类别</param>
        /// <param name="connStr">连接字符串</param>
        public DbSession(DatabaseType dt, string connStr)
        {
            DbProvider provider = CreateDbProvider(dt, connStr);

            this.db = new Database(provider);

            initDbSesion();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="assemblyName">程序集</param>
        /// <param name="className">类名</param>
        /// <param name="connStr">连接字符串</param>
        public DbSession(string assemblyName, string className, string connStr)
        {
            DbProvider provider = ProviderFactory.CreateDbProvider(assemblyName, className, connStr);
            if (provider == null)
            {
                throw new NotSupportedException(string.Format("Cannot construct DbProvider by specified parameters: {0}, {1}, {2}",
                    assemblyName, className, connStr));
            }

            this.db = new Database(provider);

            cmdCreator = new CommandCreator(db);
        }

        #endregion

        #region 查询


        /// <summary>
        /// 查询
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public FromSection<TEntity> From<TEntity>()
            where TEntity : Entity
        {
            return new FromSection<TEntity>(db);
        }


        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public FromSection From(string tableName)
        {
            return new FromSection(db, tableName);
        }
        /// <summary>
        /// Sum
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="field"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public object Sum<TEntity>(Field field, WhereClip where)
            where TEntity : Entity
        {
            return From<TEntity>().Select(field.Sum()).Where(where).ToScalar();
        }

        /// <summary>
        /// Max
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="field"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public object Max<TEntity>(Field field, WhereClip where)
            where TEntity : Entity
        {
            return From<TEntity>().Select(field.Max()).Where(where).ToScalar();
        }

        /// <summary>
        /// Min
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="field"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public object Min<TEntity>(Field field, WhereClip where)
            where TEntity : Entity
        {
            return From<TEntity>().Select(field.Min()).Where(where).ToScalar();
        }

        /// <summary>
        /// Avg
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="field"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public object Avg<TEntity>(Field field, WhereClip where)
            where TEntity : Entity
        {
            return From<TEntity>().Select(field.Avg()).Where(where).ToScalar();
        }




        /// <summary>
        /// Sum
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="field"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public TResult Sum<TEntity, TResult>(Field field, WhereClip where)
            where TEntity : Entity
        {
            return From<TEntity>().Select(field.Sum()).Where(where).ToScalar<TResult>();
        }

        /// <summary>
        /// Max
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="field"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public TResult Max<TEntity, TResult>(Field field, WhereClip where)
            where TEntity : Entity
        {
            return From<TEntity>().Select(field.Max()).Where(where).ToScalar<TResult>();
        }

        /// <summary>
        /// Min
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="field"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public TResult Min<TEntity, TResult>(Field field, WhereClip where)
            where TEntity : Entity
        {
            return From<TEntity>().Select(field.Min()).Where(where).ToScalar<TResult>();
        }

        /// <summary>
        /// Avg
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="field"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public TResult Avg<TEntity, TResult>(Field field, WhereClip where)
            where TEntity : Entity
        {
            return From<TEntity>().Select(field.Avg()).Where(where).ToScalar<TResult>();
        }

        /// <summary>
        /// 判断是否存在记录
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="where"></param>
        /// <returns></returns>
        public bool Exists<TEntity>(WhereClip where)
            where TEntity : Entity
        {
            using (IDataReader dataReader = From<TEntity>().Where(where).Top(1).Select(EntityCache.GetFirstField<TEntity>()).ToDataReader())
            {
                if (dataReader.Read())
                {
                    return true;
                }

                dataReader.Close();
            }

            return false;

        }


        /// <summary>
        /// Count
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="field"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public int Count<TEntity>(Field field, WhereClip where)
            where TEntity : Entity
        {
            return From<TEntity>().Select(field.Count()).Where(where).ToScalar<int>();
        }

        /// <summary>
        /// Count
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="where"></param>
        /// <returns></returns>
        public int Count<TEntity>(WhereClip where)
            where TEntity : Entity
        {
            return From<TEntity>().Select(Field.All.Count()).Where(where).ToScalar<int>();
        }

        #endregion

        #region Database

        /// <summary>
        /// Registers the SQL logger.
        /// </summary>
        /// <param name="handler">The handler.</param>
        public void RegisterSqlLogger(LogHandler handler)
        {
            db.OnLog += handler;
        }

        /// <summary>
        /// Unregisters the SQL logger.
        /// </summary>
        /// <param name="handler">The handler.</param>
        public void UnregisterSqlLogger(LogHandler handler)
        {
            db.OnLog -= handler;
        }

        /// <summary>
        /// Gets the db.
        /// </summary>
        /// <value>The db.</value>
        public Database Db
        {
            get
            {
                return this.db;
            }
        }

        /// <summary>
        /// Begins the transaction.
        /// </summary>
        /// <returns>The begined transaction.</returns>
        public DbTrans BeginTransaction()
        {
            return new DbTrans(db.BeginTransaction(), this);
        }

        /// <summary>
        /// Begins the transaction.
        /// </summary>
        /// <param name="il">The il.</param>
        /// <returns>The begined transaction.</returns>
        public DbTrans BeginTransaction(System.Data.IsolationLevel il)
        {
            return new DbTrans(db.BeginTransaction(il), this);
        }

        /// <summary>
        /// Closes the transaction.
        /// </summary>
        /// <param name="tran">The tran.</param>
        public void CloseTransaction(DbTransaction tran)
        {
            db.CloseConnection(tran);
        }

        /// <summary>
        /// Builds the name of the db param.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The name of the db param</returns>
        public string BuildDbParamName(string name)
        {
            Check.Require(name, "name", Check.NotNullOrEmpty);

            return db.DbProvider.BuildParameterName(name);
        }


        #endregion

        #region 更新操作

        /// <summary>
        /// 更新全部字段  
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        public int UpdateAll<TEntity>(TEntity entity)
            where TEntity : Entity
        {
            WhereClip where = DataUtils.GetPrimaryKeyWhere(entity);

            Check.Require(!WhereClip.IsNullOrEmpty(where), "entity must have the primarykey!");

            return UpdateAll<TEntity>(entity, where);
        }

        /// <summary>
        /// 更新全部字段  
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        public void UpdateAll<TEntity>(params TEntity[] entities)
            where TEntity : Entity
        {
            if (null == entities || entities.Length == 0)
                return;

            using (DbTrans trans = BeginTransaction())
            {
                UpdateAll<TEntity>(trans, entities);

                trans.Commit();
            }
        }

        /// <summary>
        /// 更新全部字段
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="tran"></param>
        /// <param name="entities"></param>
        public void UpdateAll<TEntity>(DbTransaction tran, params TEntity[] entities)
            where TEntity : Entity
        {

            if (null == entities || entities.Length == 0)
                return;

            foreach (TEntity entity in entities)
            {
                if (entity == null)
                    break;

                UpdateAll<TEntity>(entity, tran);
            }


        }


        /// <summary>
        /// 更新全部字段
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public int UpdateAll<TEntity>(TEntity entity, WhereClip where)
            where TEntity : Entity
        {
            if (entity == null)
                return 0;

            return ExecuteNonQuery(cmdCreator.CreateUpdateCommand<TEntity>(entity.GetFields(), entity.GetValues(), where));
        }

        /// <summary>
        /// 更新全部字段
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="tran"></param>
        /// <param name="entity"></param>
        public int UpdateAll<TEntity>(TEntity entity, DbTransaction tran)
            where TEntity : Entity
        {
            if (entity == null)
                return 0;

            WhereClip where = DataUtils.GetPrimaryKeyWhere(entity);

            Check.Require(!WhereClip.IsNullOrEmpty(where), "entity must have the primarykey!");

            return UpdateAll<TEntity>(entity, where, tran);
        }

        /// <summary>
        /// 更新全部字段
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="tran"></param>
        /// <param name="where"></param>
        /// <param name="entity"></param>
        public int UpdateAll<TEntity>(TEntity entity, WhereClip where, DbTransaction tran)
            where TEntity : Entity
        {
            if (entity == null)
                return 0;

            return ExecuteNonQuery(cmdCreator.CreateUpdateCommand<TEntity>(entity.GetFields(), entity.GetValues(), where), tran);
        }


        /// <summary>
        /// 更新  
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        public int Update<TEntity>(TEntity entity)
            where TEntity : Entity
        {
            if (entity.GetModifyFields().Count == 0)
                return 0;

            WhereClip where = DataUtils.GetPrimaryKeyWhere(entity);

            Check.Require(!WhereClip.IsNullOrEmpty(where), "entity must have the primarykey!");

            return Update<TEntity>(entity, where);
        }
        /// <summary>
        /// 更新  
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        public int Update<TEntity>(params TEntity[] entities)
            where TEntity : Entity
        {
            if (null == entities || entities.Length == 0)
                return 0;
            int count = 0;
            using (DbTrans trans = BeginTransaction())
            {
                count = Update<TEntity>(trans, entities);
                trans.Commit();
            }
            return count;
        }
        /// <summary>
        /// 更新  
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        public int Update<TEntity>(IEnumerable<TEntity> entities)
            where TEntity : Entity
        {
            return Update(entities.ToArray());
        }
        /// <summary>
        /// 更新
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public int Update<TEntity>(TEntity entity, WhereClip where)
            where TEntity : Entity
        {
            if (entity.GetModifyFields().Count == 0)
                return 0;
            return ExecuteNonQuery(cmdCreator.CreateUpdateCommand<TEntity>(entity, where));
        }
        /// <summary>
        /// 
        /// </summary>
        public int Update<TEntity>(TEntity entity, Expression<Func<TEntity, bool>> lambdaWhere)
            where TEntity : Entity
        {
            return Update<TEntity>(entity, ExpressionToClip<TEntity>.ToWhereClip(lambdaWhere));
        }
        /// <summary>
        /// 
        /// </summary>
        public int Update<TEntity>(TEntity entity, Where where)
            where TEntity : Entity
        {
            return Update<TEntity>(entity, where.ToWhereClip());
        }
        /// <summary>
        /// 更新
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="tran"></param>
        /// <param name="entity"></param>
        public int Update<TEntity>(TEntity entity, DbTransaction tran)
            where TEntity : Entity
        {
            if (entity.GetModifyFields().Count == 0)
                return 0;

            WhereClip where = DataUtils.GetPrimaryKeyWhere(entity);

            Check.Require(!WhereClip.IsNullOrEmpty(where), "entity must have the primarykey!");

            return Update<TEntity>(entity, where, tran);
        }
        /// <summary>
        /// 更新
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="tran"></param>
        /// <param name="entities"></param>
        public int Update<TEntity>(DbTransaction tran, params TEntity[] entities)
            where TEntity : Entity
        {

            if (null == entities || entities.Length == 0)
                return 0;
            int count = 0;
            foreach (TEntity entity in entities)
            {
                if (entity.GetModifyFields().Count == 0)
                    break;

                count += Update<TEntity>(entity, DataUtils.GetPrimaryKeyWhere(entity), tran);
            }
            return count;

        }
        /// <summary>
        /// 更新
        /// </summary>
        public int Update<TEntity>(TEntity entity, WhereClip where, DbTransaction tran)
            where TEntity : Entity
        {
            if (entity.GetModifyFields().Count == 0)
                return 0;
            return ExecuteNonQuery(cmdCreator.CreateUpdateCommand<TEntity>(entity, where), tran);
        }
        /// <summary>
        /// 
        /// </summary>
        public int Update<TEntity>(TEntity entity, Expression<Func<TEntity, bool>> lambdaWhere, DbTransaction tran)
            where TEntity : Entity
        {
            return Update<TEntity>(entity, ExpressionToClip<TEntity>.ToWhereClip(lambdaWhere), tran);
        }
        /// <summary>
        /// 
        /// </summary>
        public int Update<TEntity>(TEntity entity, Where where, DbTransaction tran)
            where TEntity : Entity
        {
            return Update<TEntity>(entity, where.ToWhereClip(), tran);
        }
        /// <summary>
        /// 更新单个值
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public int Update<TEntity>(Field field, object value, WhereClip where)
            where TEntity : Entity
        {
            if (Field.IsNullOrEmpty(field))
                return 0;

            return ExecuteNonQuery(cmdCreator.CreateUpdateCommand<TEntity>(new Field[] { field }, new object[] { value }, where));
        }
        /// <summary>
        /// 
        /// </summary>
        public int Update<TEntity>(Field field, object value, Expression<Func<TEntity, bool>> lambdaWhere)
            where TEntity : Entity
        {
            return Update<TEntity>(field, value, ExpressionToClip<TEntity>.ToWhereClip(lambdaWhere));
        }
        /// <summary>
        /// 
        /// </summary>
        public int Update<TEntity>(Field field, object value, Where where)
            where TEntity : Entity
        {
            return Update<TEntity>(field, value, where.ToWhereClip());
        }
        /// <summary>
        /// 更新单个值
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="where"></param>
        /// <param name="tran"></param>
        /// <returns></returns>
        public int Update<TEntity>(Field field, object value, WhereClip where, DbTransaction tran)
            where TEntity : Entity
        {
            if (Field.IsNullOrEmpty(field))
                return 0;

            return ExecuteNonQuery(cmdCreator.CreateUpdateCommand<TEntity>(new Field[] { field }, new object[] { value }, where), tran);
        }
        /// <summary>
        /// 
        /// </summary>
        public int Update<TEntity>(Field field, object value, Expression<Func<TEntity, bool>> lambdaWhere, DbTransaction tran)
            where TEntity : Entity
        {
            return Update<TEntity>(field, value, ExpressionToClip<TEntity>.ToWhereClip(lambdaWhere), tran);
        }
        /// <summary>
        /// 
        /// </summary>
        public int Update<TEntity>(Field field, object value, Where where, DbTransaction tran)
            where TEntity : Entity
        {
            return Update<TEntity>(field, value, where.ToWhereClip(), tran);
        }

        /// <summary>
        /// 更新多个值
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="fieldValue"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public int Update<TEntity>(Dictionary<Field, object> fieldValue, WhereClip where)
              where TEntity : Entity
        {
            if (null == fieldValue || fieldValue.Count == 0)
                return 0;
            Field[] fields = new Field[fieldValue.Count];
            object[] values = new object[fieldValue.Count];
            int i = 0;
            foreach (KeyValuePair<Field, object> kv in fieldValue)
            {
                fields[i] = kv.Key;
                values[i] = kv.Value;

                i++;
            }
            return ExecuteNonQuery(cmdCreator.CreateUpdateCommand<TEntity>(fields, values, where));
        }
        /// <summary>
        /// 
        /// </summary>
        public int Update<TEntity>(Dictionary<Field, object> fieldValue, Expression<Func<TEntity, bool>> lambdaWhere)
            where TEntity : Entity
        {
            return Update<TEntity>(fieldValue, ExpressionToClip<TEntity>.ToWhereClip(lambdaWhere));
        }
        /// <summary>
        /// 
        /// </summary>
        public int Update<TEntity>(Dictionary<Field, object> fieldValue, Where where)
            where TEntity : Entity
        {
            return Update<TEntity>(fieldValue, where.ToWhereClip());
        }
        /// <summary>
        /// 更新多个值
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="fieldValue"></param>
        /// <param name="where"></param>
        /// <param name="tran"></param>
        /// <returns></returns>
        public int Update<TEntity>(Dictionary<Field, object> fieldValue, WhereClip where, DbTransaction tran)
              where TEntity : Entity
        {
            if (null == fieldValue || fieldValue.Count == 0)
                return 0;

            Field[] fields = new Field[fieldValue.Count];
            object[] values = new object[fieldValue.Count];

            int i = 0;

            foreach (KeyValuePair<Field, object> kv in fieldValue)
            {
                fields[i] = kv.Key;
                values[i] = kv.Value;

                i++;
            }

            return ExecuteNonQuery(cmdCreator.CreateUpdateCommand<TEntity>(fields, values, where), tran);
        }
        /// <summary>
        /// 
        /// </summary>
        public int Update<TEntity>(Dictionary<Field, object> fieldValue, Expression<Func<TEntity, bool>> lambdaWhere, DbTransaction tran)
            where TEntity : Entity
        {
            return Update<TEntity>(fieldValue, ExpressionToClip<TEntity>.ToWhereClip(lambdaWhere), tran);
        }
        /// <summary>
        /// 
        /// </summary>
        public int Update<TEntity>(Dictionary<Field, object> fieldValue, Where where, DbTransaction tran)
            where TEntity : Entity
        {
            return Update<TEntity>(fieldValue, where.ToWhereClip(), tran);
        }
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TEntity"></typeparam>
        ///// <param name="entity"></param>
        ///// <param name="where"></param>
        ///// <returns></returns>
        //private DbCommand createUpdateCommand<TEntity>(TEntity entity, WhereClip where)
        //    where TEntity : Entity
        //{

        //    List<ModifyField> mfields = entity.GetModifyFields();

        //    if (null == mfields || mfields.Count == 0)
        //        return null;

        //    Field[] fields = new Field[mfields.Count];
        //    object[] values = new object[mfields.Count];

        //    int i = 0;

        //    foreach (ModifyField mf in mfields)
        //    {
        //        fields[i] = mf.Field;
        //        values[i] = mf.NewValue;
        //        i++;
        //    }

        //    return createUpdateCommand<TEntity>(fields, values, where);

        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TEntity"></typeparam>
        ///// <param name="fields"></param>
        ///// <param name="values"></param>
        ///// <param name="where"></param>
        ///// <returns></returns>
        //private DbCommand createUpdateCommand<TEntity>(Field[] fields, object[] values, WhereClip where)
        //    where TEntity : Entity
        //{
        //    Check.Require(!EntityCache.IsReadOnly<TEntity>(), string.Concat("Entity(", EntityCache.GetTableName<TEntity>(), ") is readonly!"));

        //    if (null == fields || fields.Length == 0 || null == values || values.Length == 0)
        //        return null;

        //    Check.Require(fields.Length == values.Length, "fields.Length must be equal values.Length");

        //    int length = fields.Length;

        //    if (WhereClip.IsNullOrEmpty(where))
        //        where = WhereClip.All;

        //    StringBuilder sql = new StringBuilder();
        //    sql.Append("UPDATE ");
        //    sql.Append(db.DbProvider.LeftToken.ToString());
        //    sql.Append(EntityCache.GetTableName<TEntity>());
        //    sql.Append(db.DbProvider.RightToken.ToString());
        //    sql.Append(" SET ");

        //    Field identityField = EntityCache.GetIdentityField<TEntity>();

        //    List<Parameter> list = new List<Parameter>();
        //    StringBuilder colums = new StringBuilder();
        //    for (int i = 0; i < length; i++)
        //    {
        //        if (null != identityField)
        //        {
        //            //标识列  排除
        //            if (fields[i].PropertyName.Equals(identityField.PropertyName))
        //                continue;
        //        }

        //        string pname = DataUtils.MakeUniqueKey(string.Empty);

        //        colums.Append(",");
        //        colums.Append(fields[i].FieldName);
        //        colums.Append("=");
        //        colums.Append(pname);

        //        Parameter p = new Parameter(pname, values[i], fields[i].ParameterDbType, fields[i].ParameterSize);
        //        list.Add(p);
        //    }
        //    sql.Append(colums.ToString().Substring(1));
        //    sql.Append(where.WhereString);
        //    list.AddRange(where.Parameters);

        //    DbCommand cmd = db.GetSqlStringCommand(sql.ToString());

        //    db.AddCommandParameter(cmd, list.ToArray());
        //    return cmd;
        //}

        /// <summary>
        /// 更新
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="fields"></param>
        /// <param name="values"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public int Update<TEntity>(Field[] fields, object[] values, WhereClip where)
            where TEntity : Entity
        {

            if (null == fields || fields.Length == 0)
                return 0;
            return ExecuteNonQuery(cmdCreator.CreateUpdateCommand<TEntity>(fields, values, where));
        }
        /// <summary>
        /// 
        /// </summary>
        public int Update<TEntity>(Field[] fields, object[] values, Expression<Func<TEntity, bool>> lambdaWhere)
            where TEntity : Entity
        {
            return Update<TEntity>(fields, values, ExpressionToClip<TEntity>.ToWhereClip(lambdaWhere));
        }
        /// <summary>
        /// 
        /// </summary>
        public int Update<TEntity>(Field[] fields, object[] values, Where where)
            where TEntity : Entity
        {
            return Update<TEntity>(fields, values, where.ToWhereClip());
        }
        /// <summary>
        /// 更新
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="fields"></param>
        /// <param name="values"></param>
        /// <param name="where"></param>
        /// <param name="tran"></param>
        /// <returns></returns>
        public int Update<TEntity>(Field[] fields, object[] values, WhereClip where, DbTransaction tran)
            where TEntity : Entity
        {
            if (null == fields || fields.Length == 0)
                return 0;

            return ExecuteNonQuery(cmdCreator.CreateUpdateCommand<TEntity>(fields, values, where), tran);
        }
        /// <summary>
        /// 
        /// </summary>
        public int Update<TEntity>(Field[] fields, object[] values, Expression<Func<TEntity, bool>> lambdaWhere, DbTransaction tran)
            where TEntity : Entity
        {
            return Update<TEntity>(fields, values, ExpressionToClip<TEntity>.ToWhereClip(lambdaWhere), tran);
        }
        /// <summary>
        /// 
        /// </summary>
        public int Update<TEntity>(Field[] fields, object[] values, Where where, DbTransaction tran)
            where TEntity : Entity
        {
            return Update<TEntity>(fields, values, where.ToWhereClip(), tran);
        }
        #endregion

        #region 删除操作


        /// <summary>
        ///  删除
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public int Delete<TEntity>(TEntity entity)
            where TEntity : Entity
        {
            Check.Require(!EntityCache.IsReadOnly<TEntity>(), string.Concat("Entity(", EntityCache.GetTableName<TEntity>(), ") is readonly!"));

            WhereClip where = DataUtils.GetPrimaryKeyWhere(entity);

            Check.Require(!WhereClip.IsNullOrEmpty(where), "entity must have the primarykey!");

            return Delete<TEntity>(where);
        }

        ///// <summary>
        /////  删除
        ///// </summary>
        ///// <typeparam name="TEntity"></typeparam>
        ///// <param name="entity"></param>
        ///// <param name="where"></param>
        ///// <returns></returns>
        //[Obsolete("请使用Delete<TEntity>(WhereClip where)方法!")]
        //public int Delete<TEntity>(TEntity entity, WhereClip where)
        //    where TEntity : Entity
        //{
        //    Check.Require(!EntityCache.IsReadOnly<TEntity>(), string.Concat("Entity(", EntityCache.GetTableName<TEntity>(), ") is readonly!"));

        //    return ExecuteNonQuery(createDeleteCommand(entity.GetTableName(), where));
        //}


        /// <summary>
        ///  删除
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="tran"></param>
        /// <returns></returns>
        public int Delete<TEntity>(TEntity entity, DbTransaction tran)
            where TEntity : Entity
        {
            Check.Require(!EntityCache.IsReadOnly<TEntity>(), string.Concat("Entity(", EntityCache.GetTableName<TEntity>(), ") is readonly!"));

            return Delete<TEntity>(DataUtils.GetPrimaryKeyWhere(entity), tran);
        }



        ///// <summary>
        /////  删除
        ///// </summary>
        ///// <typeparam name="TEntity"></typeparam>
        ///// <param name="where"></param>
        ///// <param name="tran"></param>
        ///// <param name="entity"></param>
        ///// <returns></returns>
        //[Obsolete("请使用Delete<TEntity>(WhereClip where, DbTransaction tran)方法!")]
        //public int Delete<TEntity>(TEntity entity, WhereClip where, DbTransaction tran)
        //    where TEntity : Entity
        //{
        //    Check.Require(!EntityCache.IsReadOnly<TEntity>(), string.Concat("Entity(", EntityCache.GetTableName<TEntity>(), ") is readonly!"));

        //    return ExecuteNonQuery(createDeleteCommand(entity.GetTableName(), where), tran);
        //}

        /// <summary>
        ///  删除
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="pkValues"></param>
        /// <returns></returns>
        public int Delete<TEntity>(params object[] pkValues)
            where TEntity : Entity
        {
            return DeleteByPrimaryKey<TEntity>(pkValues);
        }
        /// <summary>
        /// 
        /// </summary>
        public int Delete<TEntity>(IEnumerable<TEntity> entities)
            where TEntity : Entity
        {
            var eCount = entities.Count();
            switch (eCount)
            {
                case 0:
                    return 0;
                case 1:
                    return Delete(entities.First());
                default:
                    //TODO 修改成In条件，性能更高。 
                    var listKey = new List<object>();
                    var where = new Where();
                    var f = entities.First().GetPrimaryKeyFields().First();
                    foreach (var entity in entities)
                    {
                        listKey.Add(DataUtils.GetPropertyValue(entity, f.Name));
                    }
                    where.And(f.In(listKey));
                    return Delete<TEntity>(where);
            }
            //var count = 0;
            //using (DbTrans trans = BeginTransaction())
            //{
            //    foreach (var entity in entities)
            //    {
            //        count += Delete<TEntity>(entity, trans);
            //    }
            //    trans.Commit();
            //}
            //return count;
        }
        /// <summary>
        ///  删除
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="pkValues"></param>
        /// <returns></returns>
        //public int Delete<TEntity>(params string[] pkValues)
        //    where TEntity : Entity
        //{
        //    return DeleteByPrimaryKey<TEntity>(pkValues);
        //}

        /// <summary>
        ///  删除
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="pkValues"></param>
        /// <returns></returns>
        internal int DeleteByPrimaryKey<TEntity>(params object[] pkValues)
            where TEntity : Entity
        {
            Check.Require(!EntityCache.IsReadOnly<TEntity>(), string.Concat("Entity(", EntityCache.GetTableName<TEntity>(), ") is readonly!"));


            return ExecuteNonQuery(cmdCreator.CreateDeleteCommand(EntityCache.GetTableName<TEntity>(), DataUtils.GetPrimaryKeyWhere<TEntity>(pkValues)));
        }


        /// <summary>
        ///  删除
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="pkValues"></param>
        /// <param name="tran"></param>
        /// <returns></returns>
        public int Delete<TEntity>(DbTransaction tran, params object[] pkValues)
            where TEntity : Entity
        {
            return DeleteByPrimaryKey<TEntity>(tran, pkValues);
        }




        /// <summary>
        ///  删除
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="pkValues"></param>
        /// <param name="tran"></param>
        /// <returns></returns>
        public int Delete<TEntity>(DbTransaction tran, params string[] pkValues)
            where TEntity : Entity
        {
            return DeleteByPrimaryKey<TEntity>(tran, pkValues);
        }


        /// <summary>
        ///  删除
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="pkValues"></param>
        /// <param name="tran"></param>
        /// <returns></returns>
        internal int DeleteByPrimaryKey<TEntity>(DbTransaction tran, params object[] pkValues)
            where TEntity : Entity
        {
            Check.Require(!EntityCache.IsReadOnly<TEntity>(), string.Concat("Entity(", EntityCache.GetTableName<TEntity>(), ") is readonly!"));


            return ExecuteNonQuery(cmdCreator.CreateDeleteCommand(EntityCache.GetTableName<TEntity>(), DataUtils.GetPrimaryKeyWhere<TEntity>(pkValues)), tran);
        }





        /// <summary>
        ///  删除
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="where"></param>
        /// <param name="tran"></param>
        /// <returns></returns>
        public int Delete<TEntity>(WhereClip where, DbTransaction tran)
            where TEntity : Entity
        {
            Check.Require(!EntityCache.IsReadOnly<TEntity>(), string.Concat("Entity(", EntityCache.GetTableName<TEntity>(), ") is readonly!"));

            return ExecuteNonQuery(cmdCreator.CreateDeleteCommand(EntityCache.GetTableName<TEntity>(), where), tran);
        }

        /// <summary>
        ///  删除
        /// </summary>
        public int Delete<TEntity>(Expression<Func<TEntity, bool>> lambdaWhere)
            where TEntity : Entity
        {
            return Delete<TEntity>(ExpressionToClip<TEntity>.ToWhereClip(lambdaWhere));
        }

        /// <summary>
        ///  删除
        /// </summary>
        public int Delete<TEntity>(WhereClip where)
            where TEntity : Entity
        {
            Check.Require(!EntityCache.IsReadOnly<TEntity>(), string.Concat("Entity(", EntityCache.GetTableName<TEntity>(), ") is readonly!"));

            return ExecuteNonQuery(cmdCreator.CreateDeleteCommand(EntityCache.GetTableName<TEntity>(), where));
        }
        /// <summary>
        /// 删除
        /// </summary>
        public int Delete<TEntity>(Where where)
            where TEntity : Entity
        {
            return Delete<TEntity>(where.ToWhereClip());
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="tableName"></param>
        ///// <param name="where"></param>
        ///// <returns></returns>
        //private DbCommand createDeleteCommand(string tableName, WhereClip where)
        //{
        //    if (WhereClip.IsNullOrEmpty(where))
        //        where = WhereClip.All;

        //    StringBuilder sql = new StringBuilder();
        //    sql.Append("DELETE FROM ");
        //    sql.Append(db.DbProvider.LeftToken.ToString());
        //    sql.Append(tableName);
        //    sql.Append(db.DbProvider.RightToken.ToString());
        //    sql.Append(where.WhereString);
        //    DbCommand cmd = db.GetSqlStringCommand(sql.ToString());
        //    db.AddCommandParameter(cmd, where.Parameters.ToArray());

        //    return cmd;
        //}

        #endregion

        #region 添加操作
        /// <summary>
        /// 添加
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <returns></returns>
        public int Insert<TEntity>(params TEntity[] entities)
            where TEntity : Entity
        {
            if (null == entities || entities.Length == 0)
                return 0;
            int count = 0;
            using (DbTrans trans = this.BeginTransaction())
            {
                count = Insert<TEntity>(trans, entities);
                trans.Commit();
            }
            return count;
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <returns></returns>
        public int Insert<TEntity>(IEnumerable<TEntity> entities)
            where TEntity : Entity
        {
            return Insert(entities.ToArray());
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public int Insert<TEntity>(TEntity entity)
            where TEntity : Entity
        {
            return insertExecute<TEntity>(cmdCreator.CreateInsertCommand<TEntity>(entity));
        }



        /// <summary>
        /// 添加
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="tran"></param>
        /// <returns></returns>
        public int Insert<TEntity>(TEntity entity, DbTransaction tran)
            where TEntity : Entity
        {
            return insertExecute<TEntity>(cmdCreator.CreateInsertCommand<TEntity>(entity), tran);
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="tran"></param>
        /// <returns></returns>
        public int Insert<TEntity>(DbTransaction tran, params TEntity[] entities)
            where TEntity : Entity
        {
            if (null == entities || entities.Length == 0)
                return 0;
            int count = 0;
            foreach (TEntity entity in entities)
            {
                count += insertExecute<TEntity>(cmdCreator.CreateInsertCommand<TEntity>(entity), tran);
            }
            return count;
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="fields"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public int Insert<TEntity>(Field[] fields, object[] values)
            where TEntity : Entity
        {
            return insertExecute<TEntity>(cmdCreator.CreateInsertCommand<TEntity>(fields, values));
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="fields"></param>
        /// <param name="values"></param>
        /// <param name="tran"></param>
        /// <returns></returns>
        public int Insert<TEntity>(Field[] fields, object[] values, DbTransaction tran)
            where TEntity : Entity
        {
            return insertExecute<TEntity>(cmdCreator.CreateInsertCommand<TEntity>(fields, values), tran);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="cmd"></param>
        /// <returns></returns>
        private int insertExecute<TEntity>(DbCommand cmd)
             where TEntity : Entity
        {
            int returnValue = 0;

            if (null == cmd)
                return returnValue;

            //using (DbTrans dbTrans = BeginTransaction())
            //{
            //    returnValue = insertExecute<TEntity>(cmd, dbTrans);
            //    dbTrans.Commit();
            //}
            returnValue = insertExecute<TEntity>(cmd, null);
            return returnValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="cmd"></param>
        /// <param name="tran"></param>
        /// <returns></returns>
        private int insertExecute<TEntity>(DbCommand cmd, DbTransaction tran)
             where TEntity : Entity
        {
            if (null == cmd)
                return 0;

            Field identity = EntityCache.GetIdentityField<TEntity>();
            if (Field.IsNullOrEmpty(identity))
            {
                return tran == null ? ExecuteNonQuery(cmd) : ExecuteNonQuery(cmd, tran);
            }
            else
            {
                object scalarValue = null;
                if (Db.DbProvider is Dos.ORM.MsAccess.MsAccessProvider)
                {
                    if (tran == null)
                    {
                        ExecuteNonQuery(cmd);
                        scalarValue =
                            ExecuteScalar(
                                db.GetSqlStringCommand(string.Format("select max({0}) from {1}", identity.FieldName,
                                    identity.TableName))); //Max<TEntity, int>(identity, WhereClip.All) + 1;
                    }
                    else
                    {
                        ExecuteNonQuery(cmd, tran);
                        scalarValue = ExecuteScalar(db.GetSqlStringCommand(string.Format("select max({0}) from {1}", identity.FieldName, identity.TableName)), tran); //Max<TEntity, int>(identity, WhereClip.All) + 1;
                    }

                }
                else if (Db.DbProvider is Dos.ORM.Oracle.OracleProvider)
                {
                    if (tran == null)
                    {
                        ExecuteNonQuery(cmd);
                        scalarValue =
                            ExecuteScalar(
                                db.GetSqlStringCommand(string.Format(db.DbProvider.RowAutoID,
                                    EntityCache.GetSequence<TEntity>())));
                    }
                    else
                    {
                        ExecuteNonQuery(cmd, tran);
                        scalarValue = ExecuteScalar(db.GetSqlStringCommand(string.Format(db.DbProvider.RowAutoID, EntityCache.GetSequence<TEntity>())), tran);
                    }
                }
                else
                {
                    if (Db.DbProvider.SupportBatch)
                    {
                        if (tran == null)
                        {
                            cmd.CommandText = string.Concat(cmd.CommandText, ";", db.DbProvider.RowAutoID);
                            scalarValue = ExecuteScalar(cmd);
                        }
                        else
                        {
                            cmd.CommandText = string.Concat(cmd.CommandText, ";", db.DbProvider.RowAutoID);
                            scalarValue = ExecuteScalar(cmd, tran);
                        }
                    }
                    else
                    {
                        if (tran == null)
                        {
                            ExecuteNonQuery(cmd);
                            scalarValue = ExecuteScalar(db.GetSqlStringCommand(Db.DbProvider.RowAutoID));
                        }
                        else
                        {
                            ExecuteNonQuery(cmd, tran);
                            scalarValue = ExecuteScalar(db.GetSqlStringCommand(Db.DbProvider.RowAutoID), tran);
                        }
                    }
                }

                if (null == scalarValue || Convert.IsDBNull(scalarValue))
                    return 0;
                return Convert.ToInt32(scalarValue);
            }
        }
        #endregion

        #region Save操作
        /// <summary>
        /// Save。将实体批量提交至数据库，每个实体需要手动标记EntityState状态。
        /// </summary>
        public int Save<TEntity>(IEnumerable<TEntity> entities)
            where TEntity : Entity
        {
            int count = 0;
            using (DbTrans trans = this.BeginTransaction())
            {
                foreach (var entity in entities)
                {
                    EntityState es = entity.GetEntityState();
                    if (es == EntityState.Added)
                    {
                        count = Insert<TEntity>(trans, entity);
                    }
                    else if (es == EntityState.Deleted)
                    {
                        count = Delete<TEntity>(trans, entity);
                    }
                    else if (es == EntityState.Modified)
                    {
                        count = Update<TEntity>(trans, entity);
                    }
                }
                trans.Commit();
            }
            return count;
        }
        /// <summary>
        ///保存实体。需要手动标记EntityState状态。
        /// </summary>
        public int Save<TEntity>(TEntity entity)
            where TEntity : Entity
        {
            int count = 0;
            EntityState es = entity.GetEntityState();
            if (es == EntityState.Added)
            {
                count = Insert<TEntity>(entity);
            }
            else if (es == EntityState.Deleted)
            {
                count = Delete<TEntity>(entity);
            }
            else if (es == EntityState.Modified)
            {
                count = Update<TEntity>(entity);
            }
            return count;
        }
        #endregion

        #region 执行command


        /// <summary>
        /// 执行ExecuteNonQuery
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(DbCommand cmd)
        {
            //int returnValue = 0;
            //using (DbTransaction tran = db.BeginTransaction())
            //{
            //    try
            //    {
            //        returnValue = ExecuteNonQuery(cmd, tran);
            //        tran.Commit();

            //    }
            //    catch
            //    {
            //        tran.Rollback();
            //        throw;
            //    }
            //}

            //return returnValue;
            if (null == cmd)
                return 0;

            return db.ExecuteNonQuery(cmd);
        }

        /// <summary>
        /// 执行ExecuteNonQuery
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="tran"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(DbCommand cmd, DbTransaction tran)
        {
            if (null == cmd)
                return 0;
            return db.ExecuteNonQuery(cmd, tran);
        }

        /// <summary>
        /// 执行ExecuteScalar
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="tran"></param>
        /// <returns></returns>
        public object ExecuteScalar(DbCommand cmd, DbTransaction tran)
        {
            if (null == cmd)
                return null;

            return db.ExecuteScalar(cmd, tran);
        }

        /// <summary>
        /// 执行ExecuteScalar
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public object ExecuteScalar(DbCommand cmd)
        {
            //object returnValue = null;
            //using (DbTransaction tran = db.BeginTransaction())
            //{
            //    try
            //    {
            //        returnValue = ExecuteScalar(cmd, tran);
            //        tran.Commit();
            //    }
            //    catch
            //    {
            //        tran.Rollback();
            //        throw;
            //    }
            //}

            //return returnValue;
            if (null == cmd)
                return null;

            return db.ExecuteScalar(cmd);
        }

        /// <summary>
        /// 执行ExecuteReader
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public IDataReader ExecuteReader(DbCommand cmd)
        {
            if (null == cmd)
                return null;
            return db.ExecuteReader(cmd);
        }

        /// <summary>
        /// 执行ExecuteReader
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="tran"></param>
        /// <returns></returns>
        public IDataReader ExecuteReader(DbCommand cmd, DbTransaction tran)
        {
            if (null == cmd)
                return null;
            return db.ExecuteReader(cmd, tran);
        }

        /// <summary>
        /// 执行ExecuteDataSet
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(DbCommand cmd)
        {
            if (null == cmd)
                return null;
            return db.ExecuteDataSet(cmd);
        }

        /// <summary>
        /// 执行ExecuteDataSet
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="tran"></param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(DbCommand cmd, DbTransaction tran)
        {
            if (null == cmd)
                return null;
            return db.ExecuteDataSet(cmd, tran);
        }

        #endregion

        #region 属性


        /// <summary>
        /// 左边  
        /// <example>例如:sqlserver   的    [</example>
        /// </summary>
        public string LeftToken
        {
            get
            {
                return db.DbProvider.LeftToken.ToString();
            }
        }


        /// <summary>
        /// 右边
        /// <example>例如:sqlserver   的    ]</example>
        /// </summary>
        public string RightToken
        {
            get
            {
                return db.DbProvider.RightToken.ToString();
            }
        }

        /// <summary>
        /// 参数前缀
        /// <example>例如:sqlserver 的     @</example>
        /// </summary>
        public string ParamPrefix
        {
            get
            {
                return db.DbProvider.ParamPrefix.ToString();
            }
        }

        #endregion

        #region 存储过程

        /// <summary>
        /// 存储过程查询
        /// </summary>
        /// <param name="procName"></param>
        /// <returns></returns>
        public ProcSection FromProc(string procName)
        {
            return new ProcSection(this, procName);
        }

        #endregion

        #region sql语句

        /// <summary>
        /// sql查询
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public SqlSection FromSql(string sql)
        {
            return new SqlSection(this, sql);
        }

        #endregion
    }
}