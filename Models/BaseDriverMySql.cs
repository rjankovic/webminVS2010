using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using _min.Interfaces;
using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;
using _min.Common;

namespace _min.Models
{
    
    /// <summary>
    /// Provides basic database layer using and IDbDeployableFactory query parts
    /// </summary>
    class BaseDriverMySql : BaseDriver, IBaseDriver
    {
            // empty space always at the beggining of appended command!
            // non-string && non-deployable ValueType  => param, string => copy straight, other => wrong
        /*
        protected override MySqlTransaction currentTransaction { get; set; }
        protected override MySqlDataAdapter adapter { get; set; }
        protected override MySqlConnection conn { get; set; }
        DbDeployableFactory dbe = new DbDeployableFactory();
        */

        private MySqlTransaction _currentTransaction;
        private MySqlDataAdapter _adapter;
        private MySqlConnection _conn;

        protected override IDbTransaction currentTransaction
        {
            get { return _currentTransaction; }
            set { _currentTransaction = (MySqlTransaction)value; }
        }
        protected override IDbDataAdapter adapter
        {
            get { return _adapter; }
            set { _adapter = (MySqlDataAdapter)value; }
        }
        protected override IDbConnection conn
        {
            get { return _conn; }
            set { _conn = (MySqlConnection)value; }
        }


        public BaseDriverMySql(string connstring, DataTable logTable = null, bool writeLog = false)
            :base(connstring, logTable, writeLog)
        {
            conn = new MySqlConnection(connstring);
            adapter = new MySqlDataAdapter();
        }
        /*
        protected override QueryType getQueryType(string query) {
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
        */
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
        protected override IDbCommand translate(params object[] parts) {

            int paramCount = 0;
            MySqlCommand resultCmd = new MySqlCommand();
            StringBuilder resultQuery = new StringBuilder();
            resultQuery.Append("SET NAMES utf8; ");
            foreach(object part in parts){
                if (part is string)
                {         // strings are directly appended
                    string pString = (string)part;
                    resultQuery.Append(" " + pString);
                    
                }
                else if (part is IMySqlQueryDeployable)
                {
                    ((IMySqlQueryDeployable)part).Deploy(resultCmd, resultQuery, ref paramCount);
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
        
        public override int LastId() {
            string id = fetchSingle("SELECT LAST_INSERT_ID()").ToString();
            return Int32.Parse(id);
        }

        public override int NextAIForTable(string tableName) {
            DataRow res = fetch("SHOW TABLE STATUS LIKE '" + tableName + "'");
            return (int)res["Auto_increment"];
        }

        public override void TestConnection() {
            MySqlCommand cmd = new MySqlCommand("SELECT VERSION()");
            cmd.Connection = _conn;
            conn.Open();
            string version = (string)cmd.ExecuteScalar();
            conn.Close();
            if (!version.StartsWith("5")) throw new Exception("Incompatible MySQL version: " + version);
        }

    }
}
