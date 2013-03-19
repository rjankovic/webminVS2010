using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using _min.Interfaces;
using _min.Models;
using MySql.Data.MySqlClient;
using System.Text;
using System.Data;

namespace _min.Models
{


    public partial class DbDeployableMySql : IDbDeployableFactory
    {
        // empty space always at the beggining of appended command!
        // non-string && non-deployable ValueType  => param, string => copy straight, other => wrong

        class InputStr : IDbInStr, IMySqlQueryDeployable
        {       // input string 
            public string s { get; set; }
            public InputStr(string s)
            {
                this.s = s;
            }

            public void Deoploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                sb.Append(" @param" + paramCount);
                cmd.Parameters.AddWithValue("@param" + paramCount++, s);
            }
        }

        public abstract class Vals : IDbVals, IMySqlQueryDeployable
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

            public abstract void Deoploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount);
        }

        class InsertVals : Vals
        {
            public InsertVals(Dictionary<string, object> vals)
                : base(vals)
            { }
            public InsertVals(DataRow r)
                : base(r)
            { }
            public override void Deoploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
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
        }

        class UpdateVals : Vals
        {
            public UpdateVals(Dictionary<string, object> vals)
                : base(vals)
            { }
            public UpdateVals(DataRow r)
                : base(r)
            { }
            public override void Deoploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                bool first = true;
                foreach (KeyValuePair<string, object> kvp in vals)
                {
                    sb.Append((first ? "" : ", ") + "`" + kvp.Key + "` = @param" + paramCount);
                    first = false;
                    cmd.Parameters.AddWithValue("@param" + paramCount++, kvp.Value);
                }
            }
        }


        class Column : IDbCol, IMySqlQueryDeployable
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

            public void Deoploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                sb.Append(" "
                    + (table == null ? "" : ("`" + table + "`."))
                    + ("`" + column + "`")
                    + (alias == null ? "" : (" AS '" + alias + "'")));
            }
        }

        class Columns : IMySqlQueryDeployable
        {
            private List<Column> cols;
            public Columns(List<Column> cols)
            {
                this.cols = cols;
            }

            public Columns(List<string> colNames)
            {
                this.cols = new List<Column>();
                foreach (string s in colNames)
                    cols.Add(new Column(s));
            }

            public void Deoploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                bool first = true;
                foreach (Column c in cols)
                {
                    if (!first) sb.Append(", ");
                    first = false;
                    c.Deoploy(cmd, sb, ref paramCount);     // the first col will also create the needed space
                }
            }
        }

        class InnerJoin : IDbJoin, IMySqlQueryDeployable
        {
            public FK fk { get; set; }
            public string alais { get; set; }

            public InnerJoin(FK fk)
            {
                this.fk = fk;
            }

            public InnerJoin(FK fk, string alias)
            {
                this.fk = fk;
                this.alais = alias;
            }

            public void Deoploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                sb.Append(" JOIN `" + fk.refTable + "`" + (alais == null ? "" : (" AS '" + alais + "'"))
                    + " ON `" + fk.myTable + "`.`" + fk.myColumn + "` = `" + fk.refTable + "`.`" + fk.refColumn + "`");
            }
        }

        class InnerJoins : IMySqlQueryDeployable
        {
            private List<InnerJoin> joins;
            public InnerJoins(List<InnerJoin> joins)
            {
                this.joins = joins;
            }
            public InnerJoins(List<FK> FKs)
            {
                List<InnerJoin> joins = new List<InnerJoin>();
                foreach (FK fk in FKs)
                {
                    joins.Add(new InnerJoin(fk));
                }
                this.joins = joins;
            }


            public void Deoploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
            {
                foreach (InnerJoin j in joins)
                {
                    j.Deoploy(cmd, sb, ref paramCount);
                }
            }
        }

        class InStatement : IDbInList, IMySqlQueryDeployable
        {
            public List<object> list { get; set; }
            public InStatement(List<object> list)
            {
                this.list = list;
            }

            public void Deoploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount)
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

        public IDbInStr InStr(string s)
        {
            return new InputStr(s);
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

        public IMySqlQueryDeployable Cols(List<IDbCol> cols)
        {
            List<Column> cs = new List<Column>();
            foreach (IDbCol c in cols)
                cs.Add((Column)c);
            return new Columns(cs);
        }

        public IMySqlQueryDeployable Cols(List<string> colNames)
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

        public IMySqlQueryDeployable Joins(List<IDbJoin> joins)
        {
            List<InnerJoin> joinsCast = new List<InnerJoin>();
            foreach(IDbJoin j in joins)
                joinsCast.Add((InnerJoin)j);
            return new InnerJoins(joinsCast);
        }

        public IMySqlQueryDeployable Joins(List<FK> FKs)
        {
            return new InnerJoins(FKs);
        }

        public IDbInList InList(List<object> list)
        {
            return new InStatement(list);
        }


        public IMySqlQueryDeployable Condition(System.Data.DataRow lowerBounds, System.Data.DataRow upperBounds = null)
        {
            return new ConditionMySql(lowerBounds, upperBounds);
        }
    }
}