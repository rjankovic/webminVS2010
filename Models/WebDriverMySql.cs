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
                var columns = panel.fields.Select(x => x.column).ToList<string>();
                DataTable table = fetchAll("SELECT ", dbe.Cols(columns), " FROM ", panel.tableName, "WHERE", dbe.Condition(panel.PK));
                if (table.Rows.Count > 1) throw new Exception("PK is not unique");
                if (table.Rows.Count == 0) throw new Exception("No data fullfill the condition");
                DataRow row = table.Rows[0];
                
                foreach (Field field in panel.fields)
                {
                    if (!(field is M2NMappingField))        // value
                        field.value =  row[field.column].GetType() != typeof(MySql.Data.Types.MySqlDateTime) ? row[field.column] : 
                            ((MySql.Data.Types.MySqlDateTime)row[field.column]).GetDateTime();
                    else
                    {
                        M2NMappingField m2nf = (M2NMappingField)field;
                        m2nf.value = fetchMappingValues(m2nf.Mapping, (int)panel.PK[0]);
                    }
                }
            }

            foreach (Control c in panel.controls)
            {
                if (c is NavTableControl)
                {
                    AssignDataForNavTable((NavTableControl)c, false);
                }
                // always gets the whole table, save in session
                else if (c is TreeControl && c.data is DataTable)
                {
                    TreeControl tc = (TreeControl)c;
                    tc.data.DataSet.EnforceConstraints = false;
                    tc.data.Rows.Clear();
                    HierarchyNavTable hierarchy = (HierarchyNavTable)(tc.data);

                    List<IDbCol> selectCols = new List<IDbCol>();
                    selectCols.Add(dbe.Col(tc.PKColNames[0], "Id"));
                    selectCols.Add(dbe.Col(tc.parentColName, "ParentId"));
                    selectCols.Add(dbe.Col(tc.displayColName, "Caption"));
                    selectCols.Add(dbe.Col(tc.PKColNames[0], "NavId"));
                    DataTable fetched = fetchAll("SELECT", dbe.Cols(selectCols), "FROM", panel.tableName);

                    hierarchy.Merge(fetched);
                    tc.data.DataSet.EnforceConstraints = true;

                }
            }

            foreach (Panel p in panel.children)
                FillPanel(p);
        }

        public void FillPanelFKOptions(Panel panel)
        {
            foreach (Field field in panel.fields)
            {
                if (field is FKField)
                {     // FK options
                    FKField fkf = (FKField)field;
                    fkf.options = fetchFKOptions(fkf.FK);
                }
                else if (field is M2NMappingField)
                {
                    M2NMappingField m2nf = (M2NMappingField)field;
                    m2nf.options = fetchFKOptions((FK)m2nf.Mapping);
                }
            }
            //foreach (Panel p in panel.children)
            //FillPanelFks(p);
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
                        fkf.options = new SortedDictionary<int, string>();
                        foreach (string s in options)
                                fkf.options.Add(rnd.Next(), s);
                        break;
                    case _min.Common.FieldTypes.M2NMapping:
                        M2NMappingField m2nf = field as M2NMappingField;
                        m2nf.options = new SortedDictionary<int, string>();
                        //if (m2nf.Mapping.options.Count == 0)
                        //    break;
                        string[] opts = Lipsum();
                        //m2nf.Mapping.options.Clear();
                        int rndNext;
                        foreach (string s in opts)
                        {
                            rndNext = rnd.Next();
                            if (!m2nf.options.ContainsKey(rndNext))
                                m2nf.options.Add(rndNext, s);
                            else
                                continue;
                        }
                        break;
                    case _min.Common.FieldTypes.Enum:
                        field.value = 1;
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
                        c.data.DataSet.EnforceConstraints = false;
                        c.data.Rows.Clear();
                        c.data.DataSet.EnforceConstraints = true;
                        int n = rnd.Next() % 10 + 10;
                        HierarchyNavTable hierarchy = (HierarchyNavTable)(c.data);
                        for (int i = 0; i < n; i++)
                        {
                            HierarchyRow r = (HierarchyRow)hierarchy.NewRow();
                            r.Id = i + 1;
                            r.ParentId = rnd.Next() % (i + 1);
                            if ((int)(r.ParentId) == 0) r.ParentId = null;
                            r.Caption = LWord();
                            r.NavId = r.Id;
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
                ntc.data = fetchAccordingly("SELECT ", dbe.Cols(specSelect), " FROM `" + ntc.panel.tableName + "`", dbe.Joins(joins));
            }
            else
            {
                ntc.data = fetchAccordingly("SELECT ", dbe.Cols(specSelect), " FROM `" + ntc.panel.tableName + "`", 
                    dbe.Joins(ntc.FKs.Where(x => x.refTable != x.myTable).ToList<FK>()));
            }
        }



        public int insertPanel(Panel panel)
        {
            StartTransaction();
            query("INSERT INTO " + panel.tableName + " ", dbe.InsVals(panel.RetrievedInsertData));
            int ID = LastId();
            CommitTransaction();
            foreach (Field f in panel.fields) {
                if (f is M2NMappingField)
                {
                    M2NMappingField m2nf = (M2NMappingField)f;
                    MapM2NVals(m2nf.Mapping, ID, m2nf.ValueList);
                }
            }
            return ID;
        }

        public void updatePanel(Panel panel)
        {
            StartTransaction();
            int affected = query("UPDATE " + panel.tableName + " SET ", dbe.UpdVals(panel.RetrievedData), " WHERE ", dbe.Condition(panel.PK));
            if (affected > 1)
            {
                RollbackTransaction();
                throw new Exception("Panel PK not unique, trying to update more rows at a time!");
            }
            CommitTransaction();

            foreach (Field f in panel.fields)
            {
                if (f is M2NMappingField)
                {
                    M2NMappingField m2nf = (M2NMappingField)f;
                    int ID = (int)panel.PK[m2nf.Mapping.myColumn];
                    UnmapM2NMappingKey(m2nf.Mapping, ID);
                    MapM2NVals(m2nf.Mapping, ID, m2nf.ValueList);
                }
            }
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

        private SortedDictionary<int, string> fetchFKOptions(FK fk)
        {
            DataTable tbl = fetchAll("SELECT `" + fk.refColumn + "`, `" + fk.displayColumn + "` FROM `" + fk.refTable + "`");
            SortedDictionary<int, string> res = new SortedDictionary<int, string>();
            foreach (DataRow r in tbl.Rows)
            {
                if (r[0] == DBNull.Value || r[0].ToString() == "") continue;
                res.Add((int)r[0], r[1] as string);
            }
            return res;
        }


        private void UnmapM2NMappingKey(M2NMapping mapping, int key)
        {
            query("DELETE FROM `" + mapping.mapTable + "` WHERE ", dbe.Col(mapping.mapMyColumn), " = ", key);
        }

        private void MapM2NVals(M2NMapping mapping, int key, List<int> vals)
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

        private List<int> fetchMappingValues(M2NMapping mapping, int key)
        {
            DataTable tbl = fetchAll("SELECT", dbe.Col(mapping.mapRefColumn), "FROM", mapping.mapTable, "WHERE", dbe.Col(mapping.mapMyColumn), " = ", key);
            List<int> res = new List<int>();
            foreach (DataRow r in tbl.Rows)
                res.Add((int)(r[0]));
            return res;
        }


        public DataRow PKColRowFormat(Panel panel)
        {
            return fetchSchema("SELECT ", dbe.Cols(panel.PKColNames), " FROM `" + panel.tableName + "` LIMIT 1").NewRow();
        }
    }
}
