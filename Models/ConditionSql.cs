using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using _min.Interfaces;
using System.Data.SqlClient;
using System.Data.Sql;


namespace _min.Models
{
    public partial class DbDeployableFactory : IDbDeployableFactory
    {
        /// <summary>
        /// a condition (should be deployed after the keyword "WHERE") suitable for testing primary key equality renge of numeric values
        /// </summary>
        class ConditionSql : IMySqlQueryDeployable, IMSSqlQueryDeployabe
        {
            private DataRow lowerBounds;
            private DataRow upperBounds;    // optional

            public ConditionSql(DataRow lBounds, DataRow uBounds = null)
            {
                lowerBounds = lBounds;
                upperBounds = uBounds;
            }

            public void Deploy(MySql.Data.MySqlClient.MySqlCommand cmd, StringBuilder sb, ref int paramCount)
            {

                if (upperBounds == null)
                {
                    bool first = true;
                    foreach (DataColumn col in lowerBounds.Table.Columns)
                    {
                        sb.Append(first ? "" : " AND ");
                        sb.Append(" `" + col.ColumnName + "` = @param" + paramCount);
                        cmd.Parameters.AddWithValue("@param" + paramCount++, lowerBounds[col]);
                        first = false;
                    }
                }
                else
                {
                    bool first = true;
                    foreach (DataColumn col in lowerBounds.Table.Columns)
                    {
                        sb.Append(first ? " `" : " AND `" + col.ColumnName + "` ");
                        if (col.DataType == typeof(int) || col.DataType == typeof(double))
                        {
                            sb.Append("BETWEEN @param" + paramCount + " AND @param" + paramCount + 1);
                        }
                        else
                        {
                            sb.Append(" <= @param" + paramCount + " AND `" + col.ColumnName + "` >= @param" + paramCount + 1);
                        }
                        cmd.Parameters.AddWithValue("@param" + paramCount++, lowerBounds[col]);
                        cmd.Parameters.AddWithValue("@param" + paramCount++, upperBounds[col]);
                        first = false;
                    }
                }
            }


            public void Deploy(SqlCommand cmd, StringBuilder sb, ref int paramCount)
            {

                if (upperBounds == null)
                {
                    bool first = true;
                    foreach (DataColumn col in lowerBounds.Table.Columns)
                    {
                        sb.Append(first ? "" : " AND ");
                        sb.Append(" [" + col.ColumnName + "] = @param" + paramCount);
                        cmd.Parameters.AddWithValue("@param" + paramCount++, lowerBounds[col]);
                        first = false;
                    }
                }
                else
                {
                    bool first = true;
                    foreach (DataColumn col in lowerBounds.Table.Columns)
                    {
                        sb.Append(first ? " [" : " AND [" + col.ColumnName + "] ");
                        if (col.DataType == typeof(int) || col.DataType == typeof(double))
                        {
                            sb.Append("BETWEEN @param" + paramCount + " AND @param" + paramCount + 1);
                        }
                        else
                        {
                            sb.Append(" <= @param" + paramCount + " AND [" + col.ColumnName + "] >= @param" + paramCount + 1);
                        }
                        cmd.Parameters.AddWithValue("@param" + paramCount++, lowerBounds[col]);
                        cmd.Parameters.AddWithValue("@param" + paramCount++, upperBounds[col]);
                        first = false;
                    }
                }
            }

        }
    }
}
