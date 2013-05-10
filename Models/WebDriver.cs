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

    class WebDriver : IWebDriver
    {
        private IBaseDriver driver;

        private DbDeployableFactory dbe = new DbDeployableFactory();
        public WebDriver(IBaseDriver driver)
        {
            this.driver = driver;
        }


        /// <summary>
        /// Fills panel with data based on the columns included and possibly the Primary Key
        /// </summary>
        /// <param name="panel"></param>
        public void FillPanel(Panel panel)
        {
            if (panel.fields.Count() > 0)
            { // editable Panel, fetch the DataRow, simple controls - must have unique PK
                var columns = panel.fields.Where(x => x is IColumnField).Select(x => ((IColumnField)x).ColumnName).ToList<string>();
                DataTable table = driver.fetchAll("SELECT ", dbe.Cols(columns), " FROM ", panel.tableName, "WHERE", dbe.Condition(panel.PK));
                if (table.Rows.Count > 1) throw new Exception("PK is not unique");
                if (table.Rows.Count == 0) throw new WebDriverDataModificationException(
                    "No data fulfill the condition. The record may have been removed.");
                DataRow row = table.Rows[0];
                panel.OriginalData = table.Rows[0];
                
                foreach (IField field in panel.fields)       // fill the fields
                {
                    if (field is IColumnField)
                    {        // value
                        IColumnField cf = (IColumnField)field;
                        cf.Data = row[cf.ColumnName].GetType() != typeof(MySql.Data.Types.MySqlDateTime) ? row[cf.ColumnName] :
                            ((MySql.Data.Types.MySqlDateTime)row[cf.ColumnName]).GetDateTime();
                    }
                    else
                    {           // mapping values are yet to fetch
                        M2NMappingField m2nf = (M2NMappingField)field;
                        m2nf.Data = FetchMappingValues(m2nf.Mapping, (int)panel.PK[0]);
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
                    DataTable fetched = driver.fetchAll("SELECT", 
                        dbe.Col(tc.PKColNames[0], "Id"), ",",
                        dbe.Cols(selectCols), dbe.Col(tc.parentColName, "ParentId"), ",",
                        "CAST(", dbe.Col(tc.displayColName), "AS CHAR) AS \"Caption\"", ",",
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
            foreach (IField field in panel.fields)
            {
                if (field is FKField)
                {     // FK options
                    FKField fkf = (FKField)field;
                    fkf.FKOptions = FetchFKOptions(fkf.FK);
                }
                else if (field is M2NMappingField)
                {
                    M2NMappingField m2nf = (M2NMappingField)field;
                    m2nf.FKOptions = FetchFKOptions((FK)m2nf.Mapping);
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
        // will be done by the fields & controls
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
                fetchAccordingly = driver.fetchSchema;
            else
                fetchAccordingly = driver.fetchAll;

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
                ntc.data = fetchAccordingly("SELECT ", dbe.Cols(specSelect), " FROM ", dbe.Table(ntc.panel.tableName), dbe.Joins(joins));
            }
            else
            {
                ntc.data = fetchAccordingly("SELECT ", dbe.Cols(specSelect), " FROM ", dbe.Table(ntc.panel.tableName), 
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
            foreach (IField f in panel.fields)
            {
                if(f is IColumnField)
                {
                    IColumnField cf = (IColumnField)f;
                    if (panel.RetrievedManagedData[cf.ColumnName] != null && cf.Unique)
                    {
                        bool unique = driver.CheckUniqueness(panel.tableName, cf.ColumnName, panel.RetrievedManagedData[cf.ColumnName]);
                        if (!unique) throw new ConstraintException("Field \"" + cf.Caption + "\" is restrained to be unique and \""
                             + cf.Data.ToString() + "\" is already present");
                    }
                }
            }
            int ID;
            try
            {
                driver.BeginTransaction();
                driver.query("INSERT INTO ", dbe.Table(panel.tableName), dbe.InsVals(panel.RetrievedInsertData));
                ID = driver.LastId();      // TODO safe? Does transaction ensure insert separation?
                driver.CommitTransaction();
            }
            catch (MySql.Data.MySqlClient.MySqlException mye) {
                // Can occur, if there is a unique Key on multiple columns - such constraint cannot be set in panel management
                // (very rare indeed). The exception is attached a user-friendly comment and bubbles to the Show.cs, where
                // it will be displayed as a standard validation error (but probably with a notable delay).
                
                // will already be out of transaction - BaseDriver closes it immediately
                //if(IsInTransaction)
                //    driver.RollbackTransaction();
                throw new ConstraintException(FriendlyConstraintException(mye.Message, panel), null);
            }
            foreach (IField f in panel.fields) {
                if (f is M2NMappingField)
                {
                    M2NMappingField m2nf = (M2NMappingField)f;
                    MapM2NVals(m2nf.Mapping, ID, (List<int>)m2nf.Data);
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
            foreach (IField f in panel.fields) {
                if (f is IColumnField) {
                    IColumnField cf = (IColumnField)f;
                    if (cf.Unique && panel.RetrievedManagedData[cf.ColumnName] != null)       // TODO make sure all fields are set to null when should be
                    {
                        bool unique = driver.CheckUniqueness(panel.tableName, cf.ColumnName, panel.RetrievedManagedData[cf.ColumnName], panel.PK);
                        if (!unique) throw new ConstraintException("Field \"" + cf.Data + "\" is restrained to be unique and \""
                             + cf.Data.ToString() + "\" is already present");
                     }
                }
            }
            foreach (DataColumn PKcol in panel.PK.Table.Columns) {  // row PK midified
                if (panel.PK[PKcol.ColumnName].ToString() != panel.RetrievedManagedData[PKcol.ColumnName].ToString())
                    throw new WebDriverValidationException("The field '" + PKcol.ColumnName + 
                        "' is a part of the item`s identity and cannot be changed unless the item is recreated."); 
            }
            try
            {
                driver.BeginTransaction();
                int affected = driver.query("UPDATE " + panel.tableName + " SET ", dbe.UpdVals(panel.RetrievedInsertData), " WHERE ", dbe.Condition(panel.PK));
                if (affected > 1)
                {
                    driver.RollbackTransaction();
                    throw new Exception("Panel PK not unique, trying to update multiple rows at a time!");
                }
                driver.CommitTransaction();
            }
            catch (ConstraintException ce) {
                driver.RollbackTransaction();
                throw new ConstraintException(FriendlyConstraintException(ce.Message, panel), null);
            }
            foreach (IField f in panel.fields)
            {
                if (f is M2NMappingField)
                {
                    M2NMappingField m2nf = (M2NMappingField)f;
                    int ID = (int)panel.PK[m2nf.Mapping.myColumn];
                    UnmapM2NMappingKey(m2nf.Mapping, ID);
                    MapM2NVals(m2nf.Mapping, ID, (List<int>)m2nf.Data);
                }
            }
        }

        private void CheckAgainstOriginalDataRow(Panel panel){
            var columns = panel.fields.Where(x => x is IColumnField).Select(x => ((IColumnField)x).ColumnName).ToList<string>();
            DataRow actualRow = driver.fetch("SELECT ", dbe.Cols(columns), " FROM ",  dbe.Table(panel.tableName), "WHERE", dbe.Condition(panel.PK));
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
            driver.BeginTransaction();
            foreach (IField f in panel.fields) {
                if (f is M2NMappingField) {
                    M2NMapping mapping = ((M2NMappingField)f).Mapping;
                    UnmapM2NMappingKey(mapping, (int)(panel.PK[mapping.mapMyColumn]));
                }
            }
            int affected = driver.query("DELETE FROM", dbe.Table(panel.tableName), " WHERE", dbe.Condition(panel.PK));
            
            if (affected > 1)
            {
                driver.RollbackTransaction();
                throw new Exception("Panel PK not unique, trying to delete more rows at a time!");
            } 
            driver.CommitTransaction();
        }

        private SortedDictionary<int, string> FetchFKOptions(FK fk)
        {
            DataTable tbl = driver.fetchAll("SELECT ", dbe.Cols(new string[] {fk.refColumn, fk.displayColumn}), " FROM ", dbe.Table(fk.refTable));
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
            driver.query("DELETE FROM", dbe.Table(mapping.mapTable), "WHERE", dbe.Col(mapping.mapMyColumn), " = ", key);
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
                driver.query("INSERT INTO", mapping.mapTable, dbe.InsVals(row));
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
            DataTable tbl = driver.fetchAll("SELECT", dbe.Col(mapping.mapRefColumn), "FROM", dbe.Table(mapping.mapTable), "WHERE", dbe.Col(mapping.mapMyColumn), " = ", dbe.InObj(key));
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
            return driver.fetchSchema("SELECT ", dbe.Cols(panel.PKColNames), " FROM ", dbe.Table(panel.tableName)).NewRow();
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
            foreach (IField f in panel.fields) {
                if(f is IColumnField)
                    newMessage = newMessage.Replace("'" + ((IColumnField)f).ColumnName + "'", "\"" + ((IColumnField)f).Caption + "\"");
            }
            return newMessage;
        }
    }
}
