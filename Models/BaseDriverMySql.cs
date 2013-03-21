using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using _min.Interfaces;
using MySql.Data.MySqlClient;
using MySql;
using System.Data;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace _min.Models
{


    class BaseDriverMySql : IBaseDriver
    {
            // empty space always at the beggining of appended command!
            // non-string && non-deployable ValueType  => param, string => copy straight, other => wrong

    

        private MySqlConnection conn;
        // logTable declared in Environment, defined here as string query, int time (miliseconds)
        public DataTable logTable { get; private set; }
        protected bool writeLog;

        public virtual bool IsInTransaction
        {
            get;
            protected set;
        }

        private enum QueryType 
        {
            Unknown, Insert, Update, Delete, Select 
        };
        private enum Expectations
        {
            Single, InsertValues, UpdateValues, Conditions, InList, Columns, Tables
        };

        private static readonly Dictionary<string, Expectations> lastWordsDecoder =
                new Dictionary<string, Expectations>(StringComparer.OrdinalIgnoreCase)
        {
            {"IN", Expectations.InList},
            {"INSERT", Expectations.InsertValues},
            {"INTO", Expectations.InsertValues},
            {"SET", Expectations.UpdateValues},
            {"WHERE", Expectations.Conditions},            
            {"HAVING", Expectations.Conditions},
            {"SELECT", Expectations.Columns},
            {"FROM", Expectations.Tables}
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

        /* translate dibi-style query to string */
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
                conn.Open();
                adapter.FillSchema(result, SchemaType.Source);
            }
            finally
            {
                conn.Close();
            }
            if (writeLog)
            {
                watch.Stop();
                this.log(cmd.CommandText, watch);
            }
            return result;
        }

        
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

        public System.Data.DataRow fetch(params object[] parts)
        {
            DataTable table = this.fetchAll(parts);
            if (table.Rows.Count > 0)
                return table.Rows[0];
            else
                return null;
        }

        public object fetchSingle(params object[] parts)
        {
            DataTable table = this.fetchAll(parts);
            if (table.Rows.Count > 0)
                return table.Rows[0][0];
            else
                return null;
        }

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
                if(!IsInTransaction)
                    conn.Open();
                rowsAffected = cmd.ExecuteNonQuery();
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

        public void StartTransaction() {
            if (IsInTransaction) throw new Exception("Already in transaction");
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            cmd.CommandText = "START TRANSACTION";
            conn.Open();
            cmd.ExecuteNonQuery();
            IsInTransaction = true;
        }

        public void CommitTransaction()
        {
            if (!IsInTransaction) throw new Exception("Mot in transaction");
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            cmd.CommandText = "COMMIT";
            cmd.ExecuteNonQuery();
            conn.Close();
            IsInTransaction = false;
        }
        public void RollbackTransaction()
        {
            if (!IsInTransaction) throw new Exception("Mot in transaction");
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            cmd.CommandText = "ROLLBACK";
            cmd.ExecuteNonQuery();
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

    }
}
