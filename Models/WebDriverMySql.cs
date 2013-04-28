using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using _min.Interfaces;
using CE = _min.Common.Environment;
using CC = _min.Common.Constants;

namespace _min.Models
{
    class WebDriverValidationException : Exception {    // for known user-caused errors
        public WebDriverValidationException(string message)
            : base(message)
        { }
    }

    class WebDriverDataModificationException : DataException {
        public WebDriverDataModificationException(string message)
            :base(message)
        { }
    }

    class WebDriverMySql : BaseDriverMySql, IWebDriver
    {

        private DbDeployableMySql dbe = new DbDeployableMySql();
        public WebDriverMySql(string connstring, DataTable logTable = null, bool writeLog = false)
            : base(connstring, logTable, writeLog)
        { }


        /// <summary>
        /// Fills panel with data based on the columns included and possibly the Primary Key
        /// </summary>
        /// <param name="panel"></param>
        public void FillPanel(Panel panel)
        {
            if (panel.fields.Count() > 0)
            { // editable Panel, fetch the DataRow, simple controls - must have unique PK
                var columns = panel.fields.Where(x => !(x is M2NMappingField)).Select(x => x.column).ToList<string>();
                DataTable table = fetchAll("SELECT ", dbe.Cols(columns), " FROM ", panel.tableName, "WHERE", dbe.Condition(panel.PK));
                if (table.Rows.Count > 1) throw new Exception("PK is not unique");
                if (table.Rows.Count == 0) throw new WebDriverDataModificationException(
                    "No data fulfill the condition. The record may have been removed.");
                DataRow row = table.Rows[0];
                panel.OriginalData = table.Rows[0];
                
                foreach (Field field in panel.fields)       // fill the fields
                {
                    if (!(field is M2NMappingField))        // value
                        field.value =  row[field.column].GetType() != typeof(MySql.Data.Types.MySqlDateTime) ? row[field.column] : 
                            ((MySql.Data.Types.MySqlDateTime)row[field.column]).GetDateTime();
                    else
                    {           // mapping values are yet to fetch
                        M2NMappingField m2nf = (M2NMappingField)field;
                        m2nf.value = FetchMappingValues(m2nf.Mapping, (int)panel.PK[0]);
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
                {   // there are no data in storedHierarchy - that is only the case of menu in base panel - fill it
                    TreeControl tc = (TreeControl)c;
                    tc.data.DataSet.EnforceConstraints = false;
                    tc.data.Rows.Clear();
                    HierarchyNavTable hierarchy = (HierarchyNavTable)(tc.data);

                    List<IDbCol> selectCols = new List<IDbCol>();
                    // don`t need to use constants - the column names are set in HierarchNavTable
                    DataTable fetched = fetchAll("SELECT", 
                        dbe.Col(tc.PKColNames[0], "Id"), ",",
                        dbe.Cols(selectCols), dbe.Col(tc.parentColName, "ParentId"), ",",
                        "CAST(", dbe.Col(tc.displayColName), "AS CHAR) AS `Caption`", ",",
                        dbe.Col(tc.PKColNames[0], "NavId"),
                        "FROM", panel.tableName);
                    

                    hierarchy.Merge(fetched);
                    tc.data.DataSet.EnforceConstraints = true;

                }
            }

            /*
            foreach (Panel p in panel.children)
                FillPanel(p);
             */ 
        }

        public void FillPanelFKOptions(Panel panel)
        {
            foreach (Field field in panel.fields)
            {
                if (field is FKField)
                {     // FK options
                    FKField fkf = (FKField)field;
                    fkf.options = FetchFKOptions(fkf.FK);
                }
                else if (field is M2NMappingField)
                {
                    M2NMappingField m2nf = (M2NMappingField)field;
                    m2nf.options = FetchFKOptions((FK)m2nf.Mapping);
                }
            }
            //foreach (Panel p in panel.children)
            //FillPanelFks(p);
        }

        /// <summary>
        /// generate a few varchar-like strings - for FKs, mappings
        /// </summary>
        /// <returns></returns>
        private string[] Lipsum()
        {
            Random rnd = new Random();
            int a = rnd.Next() % 5 + 5;
            string[] res = new string[a];
            for (int i = 0; i < a; i++)
                res[i] = NLipsum.Core.LipsumGenerator.Generate(1, NLipsum.Core.Features.Words, "{0}", NLipsum.Core.Lipsums.Faust);
            return res;
        }

        /// <summary>
        /// a single lipsum word
        /// </summary>
        /// <returns></returns>
        private string LWord()
        {
            return NLipsum.Core.LipsumGenerator.Generate(1, NLipsum.Core.Features.Words, "{0}", NLipsum.Core.Lipsums.TheRaven);
        }

        /// <summary>
        /// a few lipsum words
        /// </summary>
        /// <returns></returns>
        private string LSentence()
        {
            return NLipsum.Core.LipsumGenerator.Generate(1, NLipsum.Core.Features.Sentences, "{0}", NLipsum.Core.Lipsums.RobinsonoKruso);
        }

        private string LText()
        {
            return NLipsum.Core.LipsumGenerator.GenerateHtml(3);
        }

        /// <summary>
        /// fill the panel with example data in Architect mode
        /// </summary>
        /// <param name="panel"></param>
        public void FillPanelArchitect(Panel panel)
        {
            Random rnd = new Random();
            int amount;
            foreach (Field field in panel.fields)
            {
                amount = rnd.Next() % 5 + 5;
                switch (field.type)
                {
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
                    case _min.Common.FieldTypes.ShortText:
                        field.value = LWord();
                        if (field.validationRules.Contains(Common.ValidationRules.Ordinal)) field.value = rnd.Next() % 10000;
                        else if (field.validationRules.Contains(Common.ValidationRules.Decimal)) field.value = rnd.NextDouble() * 1000;
                        break;
                    case _min.Common.FieldTypes.Text:
                        field.value = LText();
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
                        AssignDataForNavTable((NavTableControl)c, true);        // assigns only the schema - no values
                        // number of rows
                        int n = rnd.Next() % 5 + 5;
                        DataRow[] rows = new DataRow[n];
                        for (int i = 0; i < n; i++)
                            rows[i] = c.data.NewRow();
                        
                        c.data.Constraints.Clear(); // can do this - just for the Architect - so that are no unique constraint exceptions
                        foreach (DataColumn col in c.data.Columns)
                        {
                            if (col.DataType == typeof(DateTime) || col.DataType == typeof(MySql.Data.Types.MySqlDateTime))
                            {

                                foreach (DataRow r in rows)
                                {
                                    DateTime dt = DateTime.Now + new TimeSpan(rnd.Next() % 30, rnd.Next() % 24, rnd.Next() % 60, rnd.Next() % 60);
                                    if (col.DataType == typeof(DateTime))
                                        r[col] = dt;
                                    else r[col] = new MySql.Data.Types.MySqlDateTime(dt);
                                }
                            }
                            else if(col.DataType == typeof(int) || col.DataType == typeof(long) || col.DataType == typeof(short)
                                || col.DataType == typeof(sbyte))
                            {
                                foreach (DataRow r in rows) r[col] = rnd.Next() % 100;
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
                    else if (c is TreeControl)
                    {
                        c.data.DataSet.EnforceConstraints = false;
                        c.data.Rows.Clear();
                        c.data.DataSet.EnforceConstraints = true;
                        int n = rnd.Next() % 10 + 10;
                        HierarchyNavTable hierarchy = (HierarchyNavTable)(c.data);
                        for (int i = 0; i < n; i++) // generate a random tree - each node picks a parent or no parent with equal chance
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


        /// <summary>
        /// fills all the fields that are either FKField or M2NMappingField, in the given panel (called by FillpanelArchitect)
        /// </summary>
        /// <param name="panel"></param>
        public void FillPanelFKOptionsArchitect(Panel panel) {
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
                        string[] opts = Lipsum();
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
                    default:
                        break;
                }
            }
        }

        // used by AssignDataForNavTable
        delegate DataTable FetchMethod(params object[] parts);

        /// <summary>
        /// fills "data" tables of NavTableControls
        /// </summary>
        /// <param name="ntc"></param>
        /// <param name="schemaOnly"></param>
        private void AssignDataForNavTable(NavTableControl ntc, bool schemaOnly)
        {
            
            List<string> toSelect = ntc.displayColumns.Union(ntc.PKColNames).ToList();
            List<IDbCol> specSelect = new List<IDbCol>();
            List<IDbCol> prefixedKeys = new List<IDbCol>();
            List<string> neededTables = new List<string>();
            // different FKs to the same table - JOIN colision prevention by giving each table an alias
            Dictionary<string, int> countingForeignTableUse = new Dictionary<string, int>();
            foreach (string col in toSelect)
            {
                FK correspondingFK = ntc.FKs.Where(x => x.myColumn == col).FirstOrDefault();    // can be at most one
                if (correspondingFK is FK && ((FK)correspondingFK).refTable != ntc.panel.tableName) // dont join on itself
                {
                    neededTables.Add(correspondingFK.refTable);

                    if (!countingForeignTableUse.ContainsKey(correspondingFK.refTable)) // don`t need an alias
                    {
                        specSelect.Add(dbe.Col(correspondingFK.refTable,
                                correspondingFK.displayColumn, col));
                        countingForeignTableUse[correspondingFK.refTable] = 1;
                    }
                    else
                    {       // use SALT + the count so that it is (hopefully) unique
                        countingForeignTableUse[correspondingFK.refTable]++;
                        specSelect.Add(dbe.Col(correspondingFK.refTable + Common.Constants.SALT + countingForeignTableUse[correspondingFK.refTable],
                            correspondingFK.displayColumn, col));
                    }

                    // we must provide the real column value (its a part of the PK) in another column
                    if (ntc.PKColNames.Contains(col) && correspondingFK is FK) { 
                        prefixedKeys.Add(dbe.Col(ntc.panel.tableName, col, CC.TABLE_COLUMN_REAL_VALUE_PREFIX + col));
                    }
                }
                else
                {       // no FK, just use the original column name
                    specSelect.Add(dbe.Col(ntc.panel.tableName, col, col));
                }
            }

            specSelect.AddRange(prefixedKeys);

            FetchMethod fetchAccordingly;       // so that we can use the same fetch-code for both Architect and Admin mode
            if (schemaOnly)
                fetchAccordingly = fetchSchema;
            else
                fetchAccordingly = fetchAll;

            if (countingForeignTableUse.Values.Any(x => x > 1))
            {
                // FK table aliases
                // not 100% solution - tables with suffix "1"...
                List<IDbJoin> joins = new List<IDbJoin>();
                ntc.FKs.Reverse();  // the counter counts down when creating joins in reverse order
                foreach (FK fk in ntc.FKs)
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


        /// <summary>
        /// inserts a new row into the table managed by the panel, with the vlaues stored in it`s fields, also inserts in mapping tables
        /// </summary>
        /// <param name="panel"></param>
        /// <returns></returns>
        public int InsertIntoPanel(Panel panel)
        {
            foreach (Field f in panel.fields)
            {
                if (!(f is M2NMappingField) && f.validationRules.Contains(Common.ValidationRules.Unique))
                {
                    if (panel.RetrievedData[f.column] != null)
                    {
                        bool unique = CheckUniqueness(panel.tableName, f.column, panel.RetrievedData[f.column]);
                        if (!unique) throw new ConstraintException("Field \"" + f.caption + "\" is restrained to be unique and \""
                             + f.value.ToString() + "\" is already present");
                    }
                }
            }
            int ID;
            try
            {
                BeginTransaction();
                query("INSERT INTO " + panel.tableName + " ", dbe.InsVals(panel.RetrievedInsertData));
                ID = LastId();      // TODO safe? Does transaction ensure insert separation?
                CommitTransaction();
            }
            catch (MySql.Data.MySqlClient.MySqlException mye) {
                // Can occur, if there is a unique Key on multiple columns - such constraint cannot be set in panel management
                // (very rare indeed). The exception is attached a user-friendly comment and bubbles to the Show.cs, where
                // it will be displayed as a standard validation error (but probably with a notable delay).
                
                // will already be out of transaction - BaseDriver closes it immediately
                //if(IsInTransaction)
                //    RollbackTransaction();
                throw new ConstraintException(FriendlyConstraintException(mye.Message, panel), null);
            }
            foreach (Field f in panel.fields) {
                if (f is M2NMappingField)
                {
                    M2NMappingField m2nf = (M2NMappingField)f;
                    MapM2NVals(m2nf.Mapping, ID, m2nf.ValueList);
                }
            }
            return ID;
        }

        /// <summary>
        /// updates the db row identified by the panel PK, using data from panel fields
        /// </summary>
        /// <param name="panel"></param>
        // TODO : row PK cannot change once created!
        public void UpdatePanel(Panel panel)
        {
            CheckAgainstOriginalDataRow(panel);
            foreach (Field f in panel.fields) {
                if (!(f is M2NMappingField) && f.validationRules.Contains(Common.ValidationRules.Unique)) {
                    if (panel.RetrievedData[f.column] != null)       // TODO make sure all fields are set to null when should be
                    {
                        bool unique = CheckUniqueness(panel.tableName, f.column, panel.RetrievedData[f.column], panel.PK);
                        if (!unique) throw new ConstraintException("Field \"" + f.caption + "\" is restrained to be unique and \""
                             + f.value.ToString() + "\" is already present");
                     }
                }
            }
            foreach (DataColumn PKcol in panel.PK.Table.Columns) {  // row PK midified
                if (panel.PK[PKcol.ColumnName].ToString() != panel.RetrievedData[PKcol.ColumnName].ToString())
                    throw new WebDriverValidationException("The field '" + PKcol.ColumnName + 
                        "' is a part of the item`s identity and cannot be changed unless the item is recreated."); 
            }
            try
            {
                BeginTransaction();
                int affected = query("UPDATE " + panel.tableName + " SET ", dbe.UpdVals(panel.RetrievedData), " WHERE ", dbe.Condition(panel.PK));
                if (affected > 1)
                {
                    RollbackTransaction();
                    throw new Exception("Panel PK not unique, trying to update multiple rows at a time!");
                }
                CommitTransaction();
            }
            catch (ConstraintException ce) {
                RollbackTransaction();
                throw new ConstraintException(FriendlyConstraintException(ce.Message, panel), null);
            }
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

        private void CheckAgainstOriginalDataRow(Panel panel){
            var columns = panel.fields.Where(x => !(x is M2NMappingField)).Select(x => x.column).ToList<string>();
            DataRow actualRow = fetch("SELECT ", dbe.Cols(columns), " FROM ", panel.tableName, "WHERE", dbe.Condition(panel.PK));
            if (actualRow == null) throw new WebDriverDataModificationException(
                "The record could not be found. It may have been removed in the meantime.");
            //if(!DataRowComparer.Equals(actualRow, panel.OriginalData))
            foreach(DataColumn col in actualRow.Table.Columns){
                if((actualRow[col.ColumnName]).ToString() != (panel.OriginalData[col.ColumnName]).ToString())   // did not match equal rows otherways
                    throw new WebDriverDataModificationException(
                        "The record has been modified since loaded."
                    + " Please save your data elsewhere and check for the new values.");
            }
        }

        /// <summary>
        /// Deletes the row determined by the panel`s PK from the panl`s table.
        /// Also clears the mapping (this may not be neccessary if the constraint CASCADEs, but it is probably the desired behaviour in either case).
        /// </summary>
        /// <param name="panel"></param>
        public void DeleteFromPanel(Panel panel)
        {
            BeginTransaction();
            foreach (Field f in panel.fields) {
                if (f is M2NMappingField) {
                    M2NMapping mapping = ((M2NMappingField)f).Mapping;
                    UnmapM2NMappingKey(mapping, (int)(panel.PK[mapping.mapMyColumn]));
                }
            }
            int affected = query("DELETE FROM", dbe.Table(panel.tableName), " WHERE", dbe.Condition(panel.PK));
            
            if (affected > 1)
            {
                RollbackTransaction();
                throw new Exception("Panel PK not unique, trying to delete more rows at a time!");
            } 
            CommitTransaction();
        }

        private SortedDictionary<int, string> FetchFKOptions(FK fk)
        {
            DataTable tbl = fetchAll("SELECT `" + fk.refColumn + "`, `" + fk.displayColumn + "` FROM `" + fk.refTable + "`");
            SortedDictionary<int, string> res = new SortedDictionary<int, string>();
            foreach (DataRow r in tbl.Rows)
            {
                if (r[0] == DBNull.Value || r[0].ToString() == "") continue;
                res.Add(Int32.Parse(r[0].ToString()), r[1].ToString());     // awful... obj to int through int - find a better way
            }
            return res;
        }


        private void UnmapM2NMappingKey(M2NMapping mapping, int key)
        {
            query("DELETE FROM `" + mapping.mapTable + "` WHERE ", dbe.Col(mapping.mapMyColumn), " = ", key);
        }

        /// <summary>
        /// INSERTs rows neccessary for mapping the inserted / updated datarow to another table via a M2NMappingField. Does not clear the mapping by itself
        /// => UnmapM2NMappingKey must be called on update
        /// </summary>
        /// <param name="mapping"></param>
        /// <param name="key"></param>
        /// <param name="vals"></param>
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

        /// <summary>
        /// Fetches the IDs of rows that are associated to the panel`s datarow through a maptable and edited in a M2NMappingField
        /// </summary>
        /// <param name="mapping"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private List<int> FetchMappingValues(M2NMapping mapping, int key)
        {
            DataTable tbl = fetchAll("SELECT", dbe.Col(mapping.mapRefColumn), "FROM", mapping.mapTable, "WHERE", dbe.Col(mapping.mapMyColumn), " = ", key);
            List<int> res = new List<int>();
            foreach (DataRow r in tbl.Rows)
                res.Add((int)(r[0]));
            return res;
        }

        /// <summary>
        /// The structure of the panl`s PK property (a DataRow), no data included
        /// </summary>
        /// <param name="panel"></param>
        /// <returns></returns>
        public DataRow PKColRowFormat(Panel panel)
        {
            return fetchSchema("SELECT ", dbe.Cols(panel.PKColNames), " FROM `" + panel.tableName + "` LIMIT 1").NewRow();
        }

        /// <summary>
        /// Provides the Constraint exception thrown by MySQL command because of multi-column unique index with a less database-like message,
        /// does not modify it directly
        /// </summary>
        /// <param name="originalMessage"></param>
        /// <param name="panel"></param>
        /// <returns>the modified string</returns>
        private string FriendlyConstraintException(string originalMessage, Panel panel) {
            string newMessage = originalMessage;
            foreach (Field f in panel.fields) {
                newMessage = newMessage.Replace("'" + f.column + "'", "\"" + f.caption + "\"");
            }
            return newMessage;
        }
    }
}
