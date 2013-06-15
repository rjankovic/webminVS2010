using System;
using System.Text;
using _min.Interfaces;
using System.Data;
using System.Diagnostics;
using System.Data.SqlClient;

namespace _min.Models
{
    /// <summary>
    /// Provides basic database layer for MS SQL Server using and IDbDeployableFactory query parts
    /// </summary>
    class BaseDriverMsSql : BaseDriver, IBaseDriver
    {
            // empty space always at the beggining of appended command!
            // non-string && non-deployable ValueType  => param, string => copy straight, other => wrong

        private SqlTransaction _currentTransaction;
        private SqlDataAdapter _adapter;
        private SqlConnection _conn;

        protected override IDbTransaction currentTransaction {
            get { return _currentTransaction; }
            set { _currentTransaction = (SqlTransaction)value; }
        }
        protected override IDbDataAdapter adapter {
            get { return _adapter; }
            set { _adapter = (SqlDataAdapter)value; }
        }
        protected override IDbConnection conn {
            get { return _conn; }
            set { _conn = (SqlConnection)value; }
        }
        

        public BaseDriverMsSql(string connstring, DataTable logTable = null, bool writeLog = false)
            :base(connstring, logTable, writeLog)
        {
            conn = new SqlConnection(connstring);
            adapter = new SqlDataAdapter();
        }

        
        private void log(string query, Stopwatch watch){
                DataRow logInfo = logTable.NewRow();
                logInfo["query"] = query;
                logInfo["time"] = watch.ElapsedMilliseconds;
                logTable.Rows.Add(logInfo);
        }

        /// <summary>
        /// see BaseDriver
        /// </summary>
        /// <param name="parts">IMySQLQueryDeployable objects, strings or ValueTypes</param>
        /// <returns></returns>
        protected override IDbCommand translate(params object[] parts) {

            int paramCount = 0;
            SqlCommand resultCmd = new SqlCommand();
            StringBuilder resultQuery = new StringBuilder();

            foreach(object part in parts){
                if (part is string)
                {         // strings are directly appended
                    string pString = (string)part;
                    resultQuery.Append(" " + pString);
                    
                }
                else if (part is IMySqlQueryDeployable)
                {
                    ((IMSSqlQueryDeployabe)part).Deploy(resultCmd, resultQuery, ref paramCount);
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
            string id = fetchSingle("SELECT @@IDENTITY").ToString();
            return Int32.Parse(id);
        }

        public override int NextAIForTable(string tableName) {
            return (int)fetchSingle("SELECT IDENT_CURRENT('" + tableName + "')+1");
        }

        public override void TestConnection() {
            SqlCommand cmd = new SqlCommand("SELECT SERVERPROPERTY('productversion')");
            cmd.Connection = _conn;
            conn.Open();
            string version = (string)cmd.ExecuteScalar();
            conn.Close();
            // 1 or 10= (1 is basically impossible)
            if (!version.StartsWith("1")) throw new Exception("Incompatible SQL Server version: " + version);
        }

    }
}
