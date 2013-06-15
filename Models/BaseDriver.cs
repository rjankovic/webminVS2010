using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Diagnostics;
using System.Text;
using _min.Interfaces;
using _min.Common;

namespace _min.Models
{
    abstract class BaseDriver
    {
            // empty space always at the beggining of appended command!
            // non-string && non-deployable ValueType  => param, string => copy straight, other => wrong

        protected abstract IDbConnection conn { get; set; }
        // logTable declared in Environment, defined here as string query, int time (miliseconds)
        public DataTable logTable { get; private set; }
        protected bool writeLog;
        protected abstract IDbTransaction currentTransaction { get; set; }
        protected abstract IDbDataAdapter adapter { get; set; }
        protected DbDeployableFactory dbe = new DbDeployableFactory();

        public virtual bool IsInTransaction
        {
            get;
            protected set;
        }

        public bool WriteLog
        {
            get;
            set;
        }

        protected BaseDriver(string connstring, DataTable logTable = null, bool writeLog = false)
        {
            this.logTable = logTable;
            this.writeLog = writeLog;
            if(writeLog && logTable.Rows.Count == 0){       // no rows - reinitialize without loss
                this.logTable = new DataTable();
                this.logTable.Columns.Add("query", typeof(string));
                this.logTable.Columns.Add("time", typeof(int));
            }
        }
        
        protected void log(string query, Stopwatch watch){
                DataRow logInfo = logTable.NewRow();
                logInfo["query"] = query;
                logInfo["time"] = watch.ElapsedMilliseconds;
                logTable.Rows.Add(logInfo);
        }

        /// <summary>
        /// Creates a command by concatenating the provided arguments, preferably IMySqlQueryDeployable, which will translate themselves into SQL. ValueTypes will be passed
        /// as parameters and string will directly copied to the query.
        /// </summary>
        /// <param name="parts">IMySQLQueryDeployable objects, strings or ValueTypes</param>
        /// <returns></returns>
        protected abstract IDbCommand translate(params object[] parts);

        /// <summary>
        /// Composes an SELECT SQL query from the passed parts and returns a DataTable with structure determined by the format of 
        /// result data, but without fetching the data
        /// </summary>
        /// <param name="parts">IMySQLQueryDeployable objects, strings or ValueTypes</param>
        /// <returns></returns>
        public System.Data.DataTable fetchSchema(params object[] parts)
        {
            IDbCommand cmd = translate(parts);
            cmd.Connection = conn;
            if (currentTransaction is IDbTransaction)
            {
                cmd.Transaction = currentTransaction;
            }
            DataSet resultSet = new DataSet();
            adapter.SelectCommand = cmd;
            Stopwatch watch = new Stopwatch();
            if (logTable is DataTable)
            {
                watch.Start();
            }
            try
            {
                if (!IsInTransaction)
                    conn.Open();
                adapter.FillSchema(resultSet, SchemaType.Source);
            }
            finally
            {
                if (!IsInTransaction)
                    conn.Close();
            }
            if (writeLog)
            {
                watch.Stop();
                this.log(cmd.CommandText, watch);
            }
            return resultSet.Tables[0];
        }
        /// <summary>
        /// Composes an SQL query from the passed parts and returns the result in a table - the query MUST be an SELECT statement.
        /// </summary>
        /// <param name="parts">IMySQLQueryDeployable objects, strings or ValueTypes</param>
        /// <returns></returns>        
        public virtual System.Data.DataTable fetchAll(params object[] parts)
        {
            IDbCommand cmd = translate(parts);
            cmd.Connection = conn;
            if (currentTransaction is IDbTransaction) {
                cmd.Transaction = currentTransaction;
            }
            DataSet resultSet = new DataSet();
            adapter.SelectCommand = cmd;
            Stopwatch watch = new Stopwatch();
            if (logTable is DataTable) {
                watch.Start();
            }
            try
            {
                if(!IsInTransaction)
                    conn.Open();
                adapter.Fill(resultSet);
            }
            catch (Exception e)
            {
                if (IsInTransaction)
                    RollbackTransaction();
                throw e;
            }
            finally {
                if(!IsInTransaction)
                conn.Close();
            }
            if (writeLog)
            {
                watch.Stop();
                this.log(cmd.CommandText, watch);
            }

            DataTable resTbl = resultSet.Tables[0];
            return resTbl.Copy();
        }
        /// <summary>
        /// Composes an SQL query from the passed parts and returns the first row of the result in a DataRow - the query MUST be an SELECT statement.
        /// </summary>
        /// <param name="parts">IMySQLQueryDeployable objects, strings or ValueTypes</param>
        /// <returns></returns>        
        public System.Data.DataRow fetch(params object[] parts)
        {
            DataTable table = this.fetchAll(parts);
            if (table.Rows.Count > 0)
                return table.Rows[0];
            else
                return null;
        }

        /// <summary>
        /// Composes an SQL query from the passed parts and returns the result the first cell of the result - the query MUST be an SELECT statement.
        /// </summary>
        /// <param name="parts">IMySQLQueryDeployable objects, strings or ValueTypes</param>
        /// <returns></returns>
        public object fetchSingle(params object[] parts)
        {
            DataTable table = this.fetchAll(parts);
            if (table.Rows.Count > 0)
                return table.Rows[0][0];
            else
                return null;
        }
        /// <summary>
        /// Composes an SQL query from the passed parts and executes it.
        /// </summary>
        /// <param name="parts">IMySQLQueryDeployable objects, strings or ValueTypes</param>
        /// <returns>Number of affected rows</returns>
        public int query(params object[] parts)
        {
            IDbCommand cmd = this.translate(parts);
            if(currentTransaction != null)
                cmd.Transaction = currentTransaction;
            cmd.Connection = conn;
            int rowsAffected = 0;

            Stopwatch watch = new Stopwatch();
            if (logTable is DataTable)
            {
                watch.Start();
            }
            try
            {
                if (!IsInTransaction)
                    conn.Open();
                rowsAffected = cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                if (IsInTransaction)
                    RollbackTransaction();
                throw e;
            }
            finally {
                if(!IsInTransaction)    // wrong?
                    conn.Close();
            }
            if (writeLog)
            {
                watch.Stop();
                this.log(cmd.CommandText, watch);
            }
            return rowsAffected;
        }

        public void BeginTransaction() {
            if (IsInTransaction) throw new Exception("Already in transaction");
            conn.Open();
            currentTransaction = conn.BeginTransaction();
            IsInTransaction = true;
        }

        public void CommitTransaction()
        {
            if (!IsInTransaction) throw new Exception("Not in transaction");
            currentTransaction.Commit();
            conn.Close();
            IsInTransaction = false;
        }
        public void RollbackTransaction()
        {
            if (!IsInTransaction) throw new Exception("Not in transaction");
            currentTransaction.Rollback();
            conn.Close();
            IsInTransaction = false;
        }

        public abstract int LastId();

        public abstract int NextAIForTable(string tableName);

        /// <summary>
        /// Checks whether this column is unique in the table by checking for unique constraint in the schema
        /// => artificial constraints (not set in database) cannot be created
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        protected virtual bool CheckUniqueness(string tableName, string columnName)
        {
            DataTable schema = fetchSchema("SELECT", dbe.Col(columnName), " FROM", dbe.Table(tableName));
            return schema.Columns[0].Unique;
        }

        /// <summary>
        /// does the new value in the column keep the column unique?
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <param name="newValue"></param>
        /// <param name="updatedItemPK"></param>
        /// <returns></returns>
        public virtual bool CheckUniqueness(string tableName, string columnName, object newValue, DataRow updatedItemPK = null)
        {
            if (updatedItemPK == null)   // INSERT - there can be row with this value
                return Int64.Parse(fetchSingle("SELECT COUNT(*) FROM", dbe.Table(tableName),
                    " WHERE", dbe.Col(columnName), " =", dbe.InObj(newValue)).ToString()) == 0;       // boolean return
            else    // UPDATE - no row except this row
                return Int64.Parse(fetchSingle("SELECT COUNT(*) FROM", dbe.Table(tableName),
                " WHERE NOT ", dbe.Condition(updatedItemPK), " AND ", dbe.Col(columnName), " =", dbe.InObj(newValue)).ToString()) == 0;   // boolean return
        }

        public virtual bool CheckUniqueness(string tableName, string columnName, object newValue, string idColumnName, int id)
        {
            return Int64.Parse(fetchSingle("SELECT COUNT(*) FROM", dbe.Table(tableName),
            " WHERE NOT ", dbe.Col(idColumnName), "=", dbe.InObj(id), " AND ", dbe.Col(columnName), " =", dbe.InObj(newValue)).ToString()) == 0;     // boolean return
        }

        public abstract void TestConnection();

    }
}