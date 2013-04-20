using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using _min.Interfaces;


namespace _min.Models
{
    public partial class DbDeployableMySql : IDbDeployableFactory
    {
        /// <summary>
        /// a condition (should be deployed after the keyword "WHERE") suitable for testing primary key equality renge of numeric values
        /// </summary>
        class ConditionMySql : IMySqlQueryDeployable
        {
            private DataRow lowerBounds;
            private DataRow upperBounds;    // optional

            public ConditionMySql(DataRow lBounds, DataRow uBounds = null)
            {
                lowerBounds = lBounds;
                upperBounds = uBounds;
            }

            public void Deoploy(MySql.Data.MySqlClient.MySqlCommand cmd, StringBuilder sb, ref int paramCount)
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
        }
    }
}
