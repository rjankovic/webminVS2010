﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using _min.Interfaces;
using NLipsum;

namespace _min.Models
{
    class WebDriverMySql : BaseDriverMySql, IWebDriver
    {

        public WebDriverMySql(string connstring, DataTable logTable = null, bool writeLog = false)
            : base(connstring, logTable, writeLog)
        { }


        public void FillPanel(Panel panel)
        {
            if (panel.fields.Count() > 0)
            { // editable Panel, fetch the DataRow, simple controls
                var columns = panel.fields.Select(x => x.column);
                DataTable table = fetchAll("SELECT ", columns, " FROM ", panel.tableName, "WHERE", new ConditionMySql(panel.PK));
                if (table.Rows.Count > 1) throw new Exception("PK not unique");
                if (table.Rows.Count == 0) throw new Exception("No data fullfill the condition");
                DataRow row = table.Rows[0];
                foreach (Field field in panel.fields)
                {
                    field.value = row[field.column];
                }
            }

            foreach (Control c in panel.controls)
            {
                if (c.data.Rows.Count == 0)
                {
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


        private string[] Lipsum() {
            Random rnd = new Random();
            int a = rnd.Next() % 5 + 5;
            string[] res = new string[a];
            for (int i = 0; i < a; i++)
                res[i] = NLipsum.Core.LipsumGenerator.Generate(1, NLipsum.Core.Features.Words, "{0}", NLipsum.Core.Lipsums.Faust);
            return res;
        }

        private string LWord() {
            return NLipsum.Core.LipsumGenerator.Generate(1, NLipsum.Core.Features.Words, "{0}", NLipsum.Core.Lipsums.TheRaven);
        }

        private string LSentence()
        {
            return NLipsum.Core.LipsumGenerator.Generate(1, NLipsum.Core.Features.Sentences, "{0}", NLipsum.Core.Lipsums.RobinsonoKruso);
        }

        private string LText() {
            return NLipsum.Core.LipsumGenerator.GenerateHtml(3);
        }

        public void FillPanelArchitect(Panel panel)
        {
            Random rnd = new Random();
            int amount;
            foreach (Field field in panel.fields)
            {
                amount = rnd.Next() % 5 + 5;
                switch (field.type)
                {
                    case _min.Common.FieldTypes.FK:
                        FKField fkf = field as FKField;
                        string[] options = Lipsum();
                        foreach (string s in options)
                            fkf.fk.options.Add(s, rnd.Next());
                        break;
                    case _min.Common.FieldTypes.M2NMapping:
                        
                        
                        M2NMappingField m2nf = field as M2NMappingField;
                        //if (m2nf.Mapping.options.Count == 0)
                        //    break;
                        string[] opts = Lipsum();
                        //m2nf.Mapping.options.Clear();
                        foreach (string s in opts)
                            if(!m2nf.Mapping.options.ContainsKey(s))
                                m2nf.Mapping.options.Add(s, rnd.Next());
                        break;
                    case _min.Common.FieldTypes.Date:
                        field.value = DateTime.Now.ToShortDateString();
                        break;
                    case _min.Common.FieldTypes.DateTime:
                        field.value = DateTime.Now.ToString();
                        break;
                    case _min.Common.FieldTypes.Time:
                        field.value = DateTime.Now.ToShortTimeString();
                        break;
                    case _min.Common.FieldTypes.Holder:
                        break;
                    case _min.Common.FieldTypes.Varchar:
                        field.value = LWord();
                        break;
                    case _min.Common.FieldTypes.Text:
                        field.value = LText();
                        break;
                    case _min.Common.FieldTypes.Decimal:
                        field.value = rnd.NextDouble() % 100000;
                        break;
                    case _min.Common.FieldTypes.Ordinal:
                        field.value = rnd.Next() % 10000;
                        break;
                    case _min.Common.FieldTypes.Bool:
                        field.value = false;
                        break;
                    case _min.Common.FieldTypes.Enum:
                        throw new NotImplementedException();
                    default:
                        break;
                }
            }

            if (panel.type == Common.PanelTypes.NavTable || panel.type == Common.PanelTypes.NavTree)
            {
                foreach (Control c in panel.controls)
                {
                    if (c.independent && c.displayColumns is List<string>)        // is null othervise ?
                    {
                        c.data = fetchSchema("SELECT ", c.displayColumns, " FROM ", panel.tableName);
                        int n = rnd.Next() % 5 + 5;
                        DataRow[] rows = new DataRow[n];
                        for (int i = 0; i < n; i++)
                            rows[i] = c.data.NewRow();
                            //c.data.Rows.Add(c.data.NewRow());
                        
                        foreach (DataColumn col in c.data.Columns)
                        {
                            if (col.DataType == typeof(int) || col.DataType == typeof(long) || col.DataType == typeof(short))
                            {
                                foreach (DataRow r in rows) r[col] = rnd.Next() % 10000;
                            }
                            else if (col.DataType == typeof(float) 
                                || col.DataType == typeof(double) 
                                || col.DataType == typeof(decimal))
                            {
                                foreach (DataRow r in rows) r[col] = rnd.NextDouble() % 100000;
                            }
                            else if (col.DataType == typeof(bool))
                            {
                                foreach (DataRow r in rows) r[col] = false;
                            }
                            else
                            {
                                foreach (DataRow r in rows) r[col] = LSentence();
                            }
                        }
                        foreach (DataRow r in rows)
                            c.data.Rows.Add(r);
                    }
                }
            }

            foreach (Panel p in panel.children)
                FillPanelArchitect(p);
        }

        public int insertPanel(Panel panel, DataRow values)
        {
            return query("INSERT INTO " + panel.tableName + " ", values);
        }

        public void updatePanel(Panel panel, DataRow values)
        {
            StartTransaction();
            int affected = query("UPDATE " + panel.tableName + " SET ", values, " WHERE ", panel.PK);
            if (affected > 1)
            {
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

        public DataTable fetchFKOptions(FK fk)
        {
            return fetchAll("SELECT `" + fk.displayColumn + "`.`" + fk.refColumn + "` FROM `" + fk.refTable);
        }

        public void UnmapM2NMappingKey(M2NMapping mapping, int key)
        {
            query("DELETE FROM `", mapping.mapTable, "` WHERE `", mapping.mapMyColumn, "` = ", key);
        }

        public void MapM2NVals(M2NMapping mapping, int key, int[] vals)
        {
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

        public DataRow PKColRowFormat(Panel panel) {
            return fetchSchema("SELECT ", panel.PKColNames, " FROM ", panel.tableName, " LIMIT 1").NewRow();
        }
    }
}
