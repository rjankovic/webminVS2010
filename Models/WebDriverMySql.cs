using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using _min.Interfaces;

namespace _min.Models
{
    class WebDriverMySql : BaseDriverMySql, IWebDriver
    {

        public WebDriverMySql(string connstring, DataTable logTable = null, bool writeLog = false)
            : base(connstring, logTable, writeLog)
        { }


        public void FillPanel(Panel panel)
        {
            if (panel.fields.Count() > 0) { // editable Panel, fetch the DataRow, simple controls
                var columns = panel.fields.Select(x => x.column);
                DataTable table = fetchAll("SELECT ", columns, " FROM ", panel.tableName, "WHERE", new ConditionMySql(panel.PK));
                if (table.Rows.Count > 1) throw new Exception("PK not unique");
                if (table.Rows.Count == 0) throw new Exception("No data fullfill the condition");
                DataRow row = table.Rows[0];
                foreach (Field field in panel.fields) {
                    field.value = row[field.column];
                }
            }

            foreach (Control c in panel.controls) {
                if (c.data.Rows.Count == 0) {
                    List<string> columns = new List<string>();
                    foreach (DataColumn col in c.data.Columns)
                        columns.Add(col.ColumnName);
                    c.data = fetchAll("SELECT ", columns, " FROM ", panel.tableName, "WHERE", new ConditionMySql(panel.PK));
                }
                if (c.data.Rows.Count == 0) throw new Exception("No data fullfill the condition");
            }

            foreach (Panel p in panel.children)
                FillPanel(p);
        }

        public int insertPanel(Panel panel, DataRow values)
        {
            return query("INSERT INTO " + panel.tableName + " ", values);
        }

        public void updatePanel(Panel panel, DataRow values)
        {
            StartTransaction();
            int affected = query("UPDATE " + panel.tableName + " SET ", values, " WHERE ", panel.PK);
            if(affected > 1){
                RollbackTransaction();
                throw new Exception("Panel PK not unique, trying to update more rows at a time!");
            }
            CommitTransaction();
        }

        public void deletePanel(Panel panel)
        {
            StartTransaction();
            int affected = query("DELETE FROM `" + panel.tableName + "` WHERE", panel.PK);
            if (affected > 1)
            {
                RollbackTransaction();
                throw new Exception("Panel PK not unique, trying to delete more rows at a time!");
            }
            CommitTransaction();
        }

        public DataTable fetchFKOptions(FK fk) {
            return fetchAll("SELECT `" + fk.displayColumn + "`.`" + fk.refColumn + "` FROM `" + fk.refTable);
        }

        public void UnmapM2NMappingKey(M2NMapping mapping, int key){
            query("DELETE FROM `", mapping.mapTable, "` WHERE `", mapping.mapMyColumn, "` = ", key);
        }

        public void MapM2NVals(M2NMapping mapping, int key, int[] vals) {
            DataTable table = new DataTable();
            table.Columns.Add(mapping.mapMyColumn, typeof(int));
            table.Columns.Add(mapping.mapRefColumn, typeof(int));
            DataRow row = table.NewRow();
            foreach (int val in vals)
            {
                row[0] = key;
                row[1] = val;
                query("INSERT INTO", mapping.mapTable, row);
            }
        }
    }
}
