using System.Collections.Generic;
using _min.Interfaces;
using MySql.Data.MySqlClient;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Data.Sql;

namespace _min.Models
{

    /// <summary>
    /// A set of query parts that can append themselves to a MySql command
    /// </summary>
    public partial class DbDeployableFactory : IDbDeployableFactory
    {   // the other part is ConditionMySql
        
        // empty space always at the beggining of appended command!
        // non-string && non-deployable ValueType  => param, string => copy straight, other => wrong

        /// <summary>
        /// simple object parameter
        /// </summary>
        class InputObj : IDbInObj, IMySqlQueryDeployable, IMSSqlQueryDeployabe
        { 
            public object o { get; set; }
            public InputObj(object o)
            {
                this.o = o;
            }

            public void Deploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                sb.Append(" @param" + paramCount);
                cmd.Parameters.AddWithValue("@param" + paramCount++, o);
            }

            public void Deploy(SqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                sb.Append(" @param" + paramCount);
                cmd.Parameters.AddWithValue("@param" + paramCount++, o);
            }
        }

        public abstract class Vals : IDbVals, IMySqlQueryDeployable, IMSSqlQueryDeployabe
        {
            public Dictionary<string, object> vals { get; set; }
            public Vals(Dictionary<string, object> vals)
            {
                this.vals = vals;
            }
            public Vals(DataRow r)
            {
                this.vals = DataRowToDictionary(r);
            }

            private Dictionary<string, object> DataRowToDictionary(DataRow row)
            {
                Dictionary<string, object> res = new Dictionary<string, object>();
                foreach (DataColumn col in row.Table.Columns)
                {
                    res[col.ColumnName] = row[col.ColumnName];
                }
                return res;
            }

            public abstract void Deploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount);
            public abstract void Deploy(SqlCommand cmd, StringBuilder sb, ref int paramCount);
        }

        /// <summary>
        /// columns and values for an INSERT query
        /// </summary>
        class InsertVals : Vals
        {
            public InsertVals(Dictionary<string, object> vals)
                : base(vals)
            { }
            public InsertVals(DataRow r)
                : base(r)
            { }
            public override void Deploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                sb.Append(" (`" + string.Join("`,`", vals.Keys) + "`) VALUES(");
                bool first = true;
                foreach (object o in vals.Values)
                {
                    sb.Append((first ? " " : ", ") + "@param" + paramCount);
                    first = false;
                    cmd.Parameters.AddWithValue("@param" + paramCount++, o);
                }
                sb.Append(")");
            }

            public override void Deploy(SqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                sb.Append(" ([" + string.Join("],[", vals.Keys) + "]) VALUES(");
                bool first = true;
                foreach (object o in vals.Values)
                {
                    sb.Append((first ? " " : ", ") + ((o==null)?"NULL":("@param" + paramCount)));
                    first = false;
                    if(o != null)
                        cmd.Parameters.AddWithValue("@param" + paramCount++, o);
                }
                sb.Append(")");
            }
        }

        /// <summary>
        /// columns and values for an UPDATE query
        /// </summary>
        class UpdateVals : Vals
        {
            public UpdateVals(Dictionary<string, object> vals)
                : base(vals)
            { }
            public UpdateVals(DataRow r)
                : base(r)
            { }
            public override void Deploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                bool first = true;
                foreach (KeyValuePair<string, object> kvp in vals)
                {
                    sb.Append((first ? "" : ", ") + "`" + kvp.Key + "` = @param" + paramCount);
                    first = false;
                    cmd.Parameters.AddWithValue("@param" + paramCount++, kvp.Value);
                }
            }

            public override void Deploy(SqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                bool first = true;
                foreach (KeyValuePair<string, object> kvp in vals)
                {
                    sb.Append((first ? "" : ", ") + "[" + kvp.Key + "] = " + ((kvp.Value == null)?"NULL":("@param" + paramCount)));
                    first = false;
                    if(kvp.Value != null)
                        cmd.Parameters.AddWithValue("@param" + paramCount++, kvp.Value);
                }
            }
        }


        /// <summary>
        /// single column name
        /// </summary>
        class Column : IDbCol, IMySqlQueryDeployable, IMSSqlQueryDeployabe
        {
            public string table { get; set; }
            public string column { get; set; }
            public string alias { get; set; }

            public Column(string column)
            {
                this.column = column;
            }
            public Column(string column, string alias)
            {
                this.alias = alias;
                this.column = column;
            }
            public Column(string table, string column, string alias)
                : this(column, alias)
            {
                this.table = table;
            }

            public void Deploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                sb.Append(" "
                    + (table == null ? "" : ("`" + table + "`."))
                    + ("`" + column + "`")
                    + (alias == null ? "" : (" AS '" + alias + "'")));
            }

            public void Deploy(SqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                sb.Append(" "
                    + (table == null ? "" : ("[" + table + "]."))
                    + ("[" + column + "]")
                    + (alias == null ? "" : (" AS [" + alias + "]")));
            }
        }

        /// <summary>
        /// a few column names divided by commas
        /// </summary>
        class Columns : IMySqlQueryDeployable, IMSSqlQueryDeployabe
        {
            private IEnumerable<Column> cols;
            public Columns(IEnumerable<Column> cols)
            {
                this.cols = cols;
            }

            public Columns(IEnumerable<string> colNames)
            {
                List<Column> resCols = new List<Column>();
                foreach (string colname in colNames) {
                    resCols.Add(new Column(colname));
                }
                this.cols = resCols;
            }

            public void Deploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                bool first = true;
                foreach (Column c in cols)
                {
                    if (!first) sb.Append(", ");
                    first = false;
                    c.Deploy(cmd, sb, ref paramCount);     // the first col will also create the needed space
                }
            }

            public void Deploy(SqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                bool first = true;
                foreach (Column c in cols)
                {
                    if (!first) sb.Append(", ");
                    first = false;
                    c.Deploy(cmd, sb, ref paramCount);     // the first col will also create the needed space
                }
            }
        }

        class InnerJoin : IDbJoin, IMySqlQueryDeployable, IMSSqlQueryDeployabe
        {
            public FK fk { get; set; }
            public string alias { get; set; }

            public InnerJoin(FK fk)
            {
                this.fk = fk;
            }

            public InnerJoin(FK fk, string alias)
            {
                this.fk = fk;
                this.alias = alias;
            }

            public void Deploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                sb.Append(" JOIN `" + fk.refTable + "`" + (alias == null ? "" : (" AS `" + alias + "`"))
                    + " ON `" + fk.myTable + "`.`" + fk.myColumn + "` = `" + (alias == null ? fk.refTable : alias) + "`.`" + fk.refColumn + "`");
            }

            public void Deploy(SqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                sb.Append(" JOIN [" + fk.refTable + "]" + (alias == null ? "" : (" AS [" + alias + "]"))
                    + " ON [" + fk.myTable + "].[" + fk.myColumn + "] = [" + (alias == null ? fk.refTable : alias) + "].[" + fk.refColumn + "]");
            }
        }

        /// <summary>
        /// muliple joins concatenated
        /// </summary>
        class InnerJoins : IMySqlQueryDeployable, IMSSqlQueryDeployabe
        {
            private List<InnerJoin> joins;
            public InnerJoins(List<InnerJoin> joins)
            {
                this.joins = joins;
            }
            public InnerJoins(IEnumerable<FK> FKs)
            {
                List<InnerJoin> joins = new List<InnerJoin>();
                foreach (FK fk in FKs)
                {
                    joins.Add(new InnerJoin(fk));
                }
                this.joins = joins;
            }


            public void Deploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                foreach (InnerJoin j in joins)
                {
                    j.Deploy(cmd, sb, ref paramCount);
                }
            }

            public void Deploy(SqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                foreach (InnerJoin j in joins)
                {
                    j.Deploy(cmd, sb, ref paramCount);
                }
            }
        }

        /// <summary>
        /// IN(x,y,z,...) clause
        /// </summary>
        class InStatement : IDbInList, IMySqlQueryDeployable, IMSSqlQueryDeployabe
        {
            public List<object> list { get; set; }
            public InStatement(IEnumerable<object> list)
            {
                this.list = new List<object>(list);
            }

            public void Deploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                sb.Append("(");
                bool first = true;
                foreach (object item in list)
                {
                    sb.Append(first ? "" : ", ");
                    first = false;
                    sb.Append("@param" + paramCount);
                    cmd.Parameters.AddWithValue("@param" + paramCount++, item);
                }
                sb.Append(")");
            }

            public void Deploy(SqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                sb.Append("(");
                bool first = true;
                foreach (object item in list)
                {
                    sb.Append(first ? "" : ", ");
                    first = false;
                    sb.Append("@param" + paramCount);
                    cmd.Parameters.AddWithValue("@param" + paramCount++, item);
                }
                sb.Append(")");
            }
        }

        /// <summary>
        /// table name
        /// </summary>
        class DbTable : IDbTable
        {
            public string table { get; set; }
            public string alias { get; set; }

            public DbTable(string table, string alias = null) {
                this.table = table;
                this.alias = alias;      // TODO should check for empty strings
            }

            public void Deploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                sb.Append(" `" + table + "`"
                + (alias == null ? "" : (" AS '" + alias + "'")));
            }

            public void Deploy(SqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                sb.Append(" [" + table + "]"
                + (alias == null ? "" : (" AS [" + alias + "]")));
            }
        }


        public IDbInObj InObj(object o)
        {
            return new InputObj(o);
        }

        public IDbVals InsVals(Dictionary<string, object> vals)
        {
            return new InsertVals(vals);
        }
        public IDbVals InsVals(DataRow r)
        {
            return new InsertVals(r);
        }

        public IDbVals UpdVals(Dictionary<string, object> vals)
        {
            return new UpdateVals(vals);
        }
        public IDbVals UpdVals(DataRow r)
        {
            return new UpdateVals(r);
        }

        public IDbCol Col(string column)
        {
            return new Column(column);
        }

        public IDbCol Col(string column, string alias)
        {
            return new Column(column, alias);
        }

        public IDbCol Col(string table, string column, string alias)
        {
            return new Column(table, column, alias); 
        }

        public IQueryDeployable Cols(IEnumerable<IDbCol> cols)
        {
            List<Column> colsCast = new List<Column>();
            foreach (IDbCol c in cols)
                colsCast.Add((Column)c);
            return new Columns(colsCast);
        }

        public IQueryDeployable Cols(IEnumerable<string> colNames)
        {
            return new Columns(colNames);
        }

        public IDbJoin Join(_min.Models.FK fk)
        {
            return new InnerJoin(fk);
        }

        public IDbJoin Join(_min.Models.FK fk, string alias)
        {
            return new InnerJoin(fk, alias);
        }

        public IQueryDeployable Joins(IEnumerable<IDbJoin> joins)
        {
            List<InnerJoin> joinsCast = new List<InnerJoin>();
            foreach(IDbJoin j in joins)
                joinsCast.Add((InnerJoin)j);
            return new InnerJoins(joinsCast);
        }

        public IMySqlQueryDeployable Joins(IEnumerable<FK> FKs)
        {
            return new InnerJoins(FKs);
        }

        public IDbInList InList(IEnumerable<object> list)
        {
            return new InStatement(list);
        }


        public IQueryDeployable Condition(System.Data.DataRow lowerBounds, System.Data.DataRow upperBounds = null)
        {
            return new ConditionSql(lowerBounds, upperBounds);
        }

        public IDbTable Table(string table, string alias = null)
        {
            return new DbTable(table, alias);
        }
    }
}