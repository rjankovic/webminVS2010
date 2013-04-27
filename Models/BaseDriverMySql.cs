using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using _min.Interfaces;
using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;

namespace _min.Models
{
    /// <summary>
    /// Provides basic database layer using and IDbDeployableFactory query parts
    /// </summary>
    class BaseDriverMySql : IBaseDriver
    {
            // empty space always at the beggining of appended command!
            // non-string && non-deployable ValueType  => param, string => copy straight, other => wrong

        private MySqlConnection conn;
        // logTable declared in Environment, defined here as string query, int time (miliseconds)
        public DataTable logTable { get; private set; }
        protected bool writeLog;
        protected MySqlTransaction currentTransaction;
        DbDeployableMySql dbe = new DbDeployableMySql();

        public virtual bool IsInTransaction
        {
            get;
            protected set;
        }

        // parsed from the query beginning so that we can throw an exception when the query type doesnt match the method used to execute it
        private enum QueryType 
        {
            Unknown, Insert, Update, Delete, Select 
        };

        public BaseDriverMySql(string connstring, DataTable logTable = null, bool writeLog = false)
        {
            conn = new MySqlConnection(connstring);
            this.logTable = logTable;
            this.writeLog = writeLog;
            if(writeLog && logTable.Rows.Count == 0){       // no rows - reinitialize without loss
                this.logTable = new DataTable();
                this.logTable.Columns.Add("query", typeof(string));
                this.logTable.Columns.Add("time", typeof(int));
            }
        }

        private QueryType getQueryType(string query) {
            query = query.Trim();
            string firstWord = query.Split(' ').First();
            switch (firstWord.ToUpper())
            {
                case "SELECT":
                case "SHOW":
                    return QueryType.Select;
                case "INSERT":
                    return QueryType.Insert;
                case "UPDATE":
                    return QueryType.Update;
                case "DELETE":
                    return QueryType.Delete;
                default:
                    throw new FormatException("Unrecognised type of MySql Command");
            }
        }

        private void log(string query, Stopwatch watch){
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
        protected MySqlCommand translate(params object[] parts) {

            int paramCount = 0;
            MySqlCommand resultCmd = new MySqlCommand();
            StringBuilder resultQuery = new StringBuilder();

            foreach(object part in parts){
                if (part is string)
                {         // strings are directly appended
                    string pString = (string)part;
                    resultQuery.Append(" " + pString);
                    
                }
                else if (part is IMySqlQueryDeployable)
                {
                    ((IMySqlQueryDeployable)part).Deoploy(resultCmd, resultQuery, ref paramCount);
                }
                else if (part is ValueType)
                {
                    resultQuery.Append(" @param" + paramCount);
                    resultCmd.Parameters.AddWithValue("@param" + paramCount++, part);
                }
                else throw new FormatException("Unrecognised query part " + part.GetType().ToString());
            }

            resultCmd.CommandText = resultQuery.ToString();
            return resultCmd;
        }

        /// <summary>
        /// Composes an SELECT SQL query from the passed parts and returns a DataTable with structure determined by the format of 
        /// result data, but without fetching the data
        /// </summary>
        /// <param name="parts">IMySQLQueryDeployable objects, strings or ValueTypes</param>
        /// <returns></returns>
        public System.Data.DataTable fetchSchema(params object[] parts)
        {
            MySqlCommand cmd = translate(parts);
            cmd.Connection = conn;
            QueryType type = this.getQueryType(cmd.CommandText);
            if (type != QueryType.Select)
            {
                throw new Exception("Trying to fetch from a non-select query");
            }
            DataTable result = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
            Stopwatch watch = new Stopwatch();
            if (logTable is DataTable)
            {
                watch.Start();
            }
            try
            {
                if(!IsInTransaction)
                    conn.Open();
                adapter.FillSchema(result, SchemaType.Source);
            }
            finally
            {
                if(!IsInTransaction)
                    conn.Close();
            }
            if (writeLog)
            {
                watch.Stop();
                this.log(cmd.CommandText, watch);
            }
            return result;
        }

        /// <summary>
        /// Composes an SQL query from the passed parts and returns the result in a table - the query MUST be an SELECT statement.
        /// </summary>
        /// <param name="parts">IMySQLQueryDeployable objects, strings or ValueTypes</param>
        /// <returns></returns>        
        public System.Data.DataTable fetchAll(params object[] parts)
        {
            MySqlCommand cmd = translate(parts);
            cmd.Connection = conn;
            QueryType type = this.getQueryType(cmd.CommandText);
            if (type != QueryType.Select)
            {
                throw new Exception("Trying to fetch from a non-select query");
            }
            DataTable result = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
            Stopwatch watch = new Stopwatch();
            if (logTable is DataTable) {
                watch.Start();
            }
            try
            {
                if(!IsInTransaction)
                    conn.Open();
                adapter.Fill(result);
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

            return result;
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
            MySqlCommand cmd = this.translate(parts);
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

        public int LastId() {
            string id = fetchSingle("SELECT LAST_INSERT_ID()").ToString();
            return Int32.Parse(id);
        }

        public int NextAIForTable(string tableName) {
            DataRow res = fetch("SHOW TABLE STATUS LIKE '" + tableName + "'");
            return (int)res["Auto_increment"];
        }

        /// <summary>
        /// Checks whether this column is unique in the table by checking for unique constraint in the schema
        /// => artificial constraints (not set in database) cannot be created
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        protected bool CheckUniqueness(string tableName, string columnName)
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
        protected bool CheckUniqueness(string tableName, string columnName, object newValue, DataRow updatedItemPK = null)
        {
            if (updatedItemPK == null)   // INSERT - there can be row with this value
                return (Int64)fetchSingle("SELECT NOT EXISTS( SELECT", dbe.Col(columnName), " FROM", dbe.Table(tableName),
                    " WHERE", dbe.Col(columnName), " =", dbe.InObj(newValue), ")") == 1;       // boolean return
            else    // UPDATE - no row except this row
                return (Int64)fetchSingle("SELECT NOT EXISTS( SELECT", dbe.Col(columnName), " FROM", dbe.Table(tableName),
                " WHERE NOT ", dbe.Condition(updatedItemPK), " AND ", dbe.Col(columnName), " =", dbe.InObj(newValue as object), ")") == 1;   // boolean return
        }

        protected bool CheckUniqueness(string tableName, string columnName, object newValue, string idColumnName, int id)
        {
            return (Int64)fetchSingle("SELECT NOT EXISTS( SELECT", dbe.Col(columnName), " FROM", dbe.Table(tableName),
            " WHERE NOT ", dbe.Col(idColumnName), "=", id, " AND ", dbe.Col(columnName), " =", dbe.InObj(newValue), ")") == 1;     // boolean return
        }

        public void TestConnection() {
            MySqlCommand cmd = new MySqlCommand("SELECT 1");
            cmd.Connection = conn;
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }

    }
}
