using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using _min.Interfaces;
using NLipsum;
using _min.Models;

namespace _min.Models
{
    class WebDriverMySql : BaseDriverMySql, IWebDriver
    {

        private DbDeployableMySql dbe = new DbDeployableMySql();
        public WebDriverMySql(string connstring, DataTable logTable = null, bool writeLog = false)
            : base(connstring, logTable, writeLog)
        { }




        public void FillPanel(Panel panel)
        {
            if (panel.fields.Count() > 0)
            { // editable Panel, fetch the DataRow, simple controls
                var columns = panel.fields.Select(x => x.column);
                DataTable table = fetchAll("SELECT ", columns, " FROM ", panel.tableName, "WHERE", new ConditionMySql(panel.PK));
                if (table.Rows.Count > 1) throw new Exception("PK is not unique");
                if (table.Rows.Count == 0) throw new Exception("No data fullfill the condition");
                DataRow row = table.Rows[0];
                foreach (Field field in panel.fields)
                {
                    field.value = row[field.column];

                    if (field is FKField || field is M2NMappingField) {     // M2NMapping is derived from FK
                        FK fk = field is FKField ? ((FKField)field).FK : ((M2NMappingField)field).Mapping;
                        DataTable options = fetchFKOptions(fk);
                        foreach (DataRow r in options.Rows) {
                            fk.options.Add(r[fk.displayColumn] as string, (int)r[fk.refColumn]);
                        }
                    }
                }
            }

            foreach (Control c in panel.controls)
            {
                if (c is NavTableControl)
                {
                    AssignDataForNavTable((NavTableControl)c, false);
                }
                else if (c is TreeControl && c.data is DataTable) {
                    TreeControl tc = (TreeControl)c;
                    tc.data.Rows.Clear();
                    HierarchyNavTable hierarchy = (HierarchyNavTable)(tc.data);
                    DataTable fetched = fetchAll("SELECT ", tc.PKColNames[0], tc.parentColName, tc.displayColName);
                    foreach(DataRow fetchedRow in fetched.Rows)
                    {
                        HierarchyRow r = (HierarchyRow)hierarchy.NewRow();
                        r.Id = (int)fetchedRow[tc.PKColNames[0]];
                        r.ParentId = (int)fetchedRow[tc.parentColName];
                        r.Caption = fetchedRow[tc.displayColName] as string;
                        r.NavId = r.Id;
                        c.data.Rows.Add(r);
                    }
                }
            }

            foreach (Panel p in panel.children)
                FillPanel(p);
        }


        private string[] Lipsum()
        {
            Random rnd = new Random();
            int a = rnd.Next() % 5 + 5;
            string[] res = new string[a];
            for (int i = 0; i < a; i++)
                res[i] = NLipsum.Core.LipsumGenerator.Generate(1, NLipsum.Core.Features.Words, "{0}", NLipsum.Core.Lipsums.Faust);
            return res;
        }

        private string LWord()
        {
            return NLipsum.Core.LipsumGenerator.Generate(1, NLipsum.Core.Features.Words, "{0}", NLipsum.Core.Lipsums.TheRaven);
        }

        private string LSentence()
        {
            return NLipsum.Core.LipsumGenerator.Generate(1, NLipsum.Core.Features.Sentences, "{0}", NLipsum.Core.Lipsums.RobinsonoKruso);
        }

        private string LText()
        {
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
                            if (!fkf.FK.options.ContainsKey(s))
                                fkf.FK.options.Add(s, rnd.Next());
                        break;
                    case _min.Common.FieldTypes.M2NMapping:


                        M2NMappingField m2nf = field as M2NMappingField;
                        //if (m2nf.Mapping.options.Count == 0)
                        //    break;
                        string[] opts = Lipsum();
                        //m2nf.Mapping.options.Clear();
                        foreach (string s in opts)
                            if (!m2nf.Mapping.options.ContainsKey(s))
                                m2nf.Mapping.options.Add(s, rnd.Next());
                        break;
                    case _min.Common.FieldTypes.Date:
                        field.value = DateTime.Now;
                        break;
                    case _min.Common.FieldTypes.DateTime:
                        field.value = DateTime.Now;
                        break;
                    case _min.Common.FieldTypes.Time:
                        field.value = DateTime.Now;
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

                    if (c is NavTableControl)
                    {
                        AssignDataForNavTable((NavTableControl)c, true);
                        int n = rnd.Next() % 5 + 5;
                        DataRow[] rows = new DataRow[n];
                        for (int i = 0; i < n; i++)
                            rows[i] = c.data.NewRow();
                        //c.data.Rows.Add(c.data.NewRow());

                        foreach (DataColumn col in c.data.Columns)
                        {
                            //if(!c.data.PrimaryKeycol.Unique = false;
                            if (col.DataType == typeof(DateTime))
                            {
                                foreach (DataRow r in rows) r[col] = DateTime.Now + new TimeSpan(rnd.Next() % 30, rnd.Next() % 24, rnd.Next() % 60, rnd.Next() % 60);
                            }
                            else if (col.DataType == typeof(int) || col.DataType == typeof(long) || col.DataType == typeof(short))
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
                                foreach (DataRow r in rows)
                                {
                                    string s = LSentence();
                                    if (col.MaxLength > -1 && col.MaxLength < s.Length)
                                    {
                                        s = s.Substring(0, col.MaxLength);
                                    }
                                    r[col] = s;
                                }
                            }
                        }
                        foreach (DataRow r in rows)
                            c.data.Rows.Add(r);
                    }
                    if (c.panel.type == Common.PanelTypes.NavTree && c.data is DataTable)
                    {
                        c.data.Rows.Clear();
                        int n = rnd.Next() % 10 + 10;
                        HierarchyNavTable hierarchy = (HierarchyNavTable)(c.data);
                        for (int i = 0; i < n; i++)
                        {
                            HierarchyRow r = (HierarchyRow)hierarchy.NewRow();
                            r.Id = i + 1;
                            r.ParentId = rnd.Next() % (i + 1);
                            r.Caption = LWord();
                            r.NavId = 0;
                            c.data.Rows.Add(r);
                        }
                    }
                }
            }

            foreach (Panel p in panel.children)
                FillPanelArchitect(p);
        }


        delegate DataTable FetchMethod(params object[] parts);

        private void AssignDataForNavTable(NavTableControl ntc, bool schemaOnly)
        {

            List<string> toSelect = ntc.displayColumns.Union(ntc.PKColNames).ToList();
            /*
            foreach(string s in c.PKColNames)
                if(!toSelect.Contains(s))
                    toSelect.Add(s);
             */
            List<IDbCol> specSelect = new List<IDbCol>();
            //List<Tuple<string, string, string>> specSelect = new List<Tuple<string, string, string>>();
            List<string> neededTables = new List<string>();
            // different FKs to teh same table - JOIN colision prevention
            Dictionary<string, int> countingForeignTableUse = new Dictionary<string, int>();
            foreach (string col in toSelect)
            {
                FK correspondingFK = ntc.FKs.Where(x => x.myColumn == col).FirstOrDefault();
                if (correspondingFK is FK && ((FK)correspondingFK).refTable != ntc.panel.tableName) // dont join on itself
                {
                    neededTables.Add(correspondingFK.refTable);

                    if (!countingForeignTableUse.ContainsKey(correspondingFK.refTable))
                    {
                        specSelect.Add(dbe.Col(correspondingFK.refTable,
                                correspondingFK.displayColumn, col));
                        countingForeignTableUse[correspondingFK.refTable] = 1;
                    }
                    else
                    {
                        countingForeignTableUse[correspondingFK.refTable]++;
                        specSelect.Add(dbe.Col(correspondingFK.refTable + Common.Constants.SALT + countingForeignTableUse[correspondingFK.refTable],
                            correspondingFK.displayColumn, col));
                    }
                }
                else
                {
                    specSelect.Add(dbe.Col(ntc.panel.tableName, col, col));
                }
            }

            FetchMethod fetchAccordingly;
            if (schemaOnly)
                fetchAccordingly = fetchSchema;
            else
                fetchAccordingly = fetchAll;

            if (countingForeignTableUse.Values.Any(x => x > 1))
            {
                // FK table aliases
                // not 100% solution - tables with suffix "1"...
                List<IDbJoin> joins = new List<IDbJoin>();
                ntc.FKs.Reverse();
                foreach (FK fk in ntc.FKs)    // so that the counter counts down
                {
                    string alias = fk.refTable +
                        (countingForeignTableUse[fk.refTable] > 1 ?
                        Common.Constants.SALT + (countingForeignTableUse[fk.refTable]--).ToString() : "");
                    joins.Add(dbe.Join(fk, alias));
                }
                ntc.data = fetchAccordingly("SELECT ", specSelect, " FROM `" + ntc.panel.tableName + "`", joins);
            }
            else
            {
                ntc.data = fetchAccordingly("SELECT ", dbe.Cols(specSelect), " FROM `" + ntc.panel.tableName + "`", dbe.Joins(ntc.FKs));
            }
        }



        public int insertPanel(Panel panel, DataRow values)
        {
            return query("INSERT INTO " + panel.tableName + " ", values);
        }

        public void updatePanel(Panel panel, DataRow values)
        {
            StartTransaction();
            int affected = query("UPDATE " + panel.tableName + " SET ", values, " WHERE ", dbe.Condition(panel.PK));
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
            int affected = query("DELETE FROM `" + panel.tableName + "` WHERE", dbe.Condition(panel.PK));
            if (affected > 1)
            {
                RollbackTransaction();
                throw new Exception("Panel PK not unique, trying to delete more rows at a time!");
            }
            CommitTransaction();
        }

        public DataTable fetchFKOptions(FK fk)
        {
            return fetchAll("SELECT `" + fk.displayColumn + "`, `" + fk.refColumn + "` FROM `" + fk.refTable);
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
                query("INSERT INTO", mapping.mapTable, dbe.InsVals(row));
            }
        }


        public DataRow PKColRowFormat(Panel panel)
        {
            return fetchSchema("SELECT ", dbe.Cols(panel.PKColNames), " FROM `" + panel.tableName + "` LIMIT 1").NewRow();
        }
    }
}
