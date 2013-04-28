using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using _min.Interfaces;
using CC = _min.Common.Constants;
using _min.Models;

namespace _min.Models
{
    class StatsMySql : BaseDriverMySql, IStats 
    {
        private string webDb;
        DbDeployableMySql dbe = new DbDeployableMySql();

        private Dictionary<string, DataColumnCollection> columnTypes = null;
        private Dictionary<string, List<string>> columnsToDisplay = null;
        private Dictionary<string, List<M2NMapping>> mappings = null;
        private Dictionary<string, List<string>> globalPKs = null;
        private Dictionary<string, List<FK>> globalFKs = null;
        private List<string> tables = null;

        /// <summary>
        /// stores resrutn value of GetColumnTYpes
        /// </summary>
        public Dictionary<string, DataColumnCollection> ColumnTypes {
            get {
                if (columnTypes == null)
                    GetColumnTypes();
                return columnTypes;
            }
            private set {
                columnTypes = value;
            }
        }
        

        /// <summary>
        /// stores return of ColumnsToDisplay - preferred colums to display in summaries for each table
        /// </summary>
        public Dictionary<string, List<string>> ColumnsToDisplay {
            get
            {
                if (columnsToDisplay == null)
                    FindColumnsToDisplay();
                return columnsToDisplay;
            
            }
            private set {
                columnsToDisplay = value;
            }
        }

        /// <summary>
        /// stores return of FindMappings
        /// </summary>
        public Dictionary<string, List<M2NMapping>> Mappings {
            get {
                if (mappings == null) {
                    mappings = new Dictionary<string, List<M2NMapping>>();
                    List<string> tables = TableList();
                    foreach (string table in tables) {
                        mappings[table] = new List<M2NMapping>();
                    }
                    List<M2NMapping> amp = FindMappings();
                    var grouped = from mp in amp
                                  group mp by mp.myTable into g
                                  select new
                                  {
                                      table = g.Key,
                                      mappings = g.ToList<M2NMapping>()
                                  };
                    foreach (var group in grouped) {
                        mappings[group.table] = group.mappings;
                    }
                }
                return mappings;
            }
        }

        /// <summary>
        /// list of PK columns for each table
        /// </summary>
        public Dictionary<string, List<string>> PKs {
            get {
                if (globalPKs == null) globalPKs = PrimaryKeyCols();
                return globalPKs;
            }
        }

        /// <summary>
        /// list of foreign keys for each table
        /// </summary>
        public Dictionary<string, List<FK>> FKs {
            get {
                if (globalFKs == null)
                    globalFKs = ForeignKeys();
                return globalFKs;
            }
        }

        /// <summary>
        /// list of tables in the db
        /// </summary>
        public List<string> Tables {
            get {
                if (tables == null)
                    tables = TableList();
                return tables;
            }
        }

        public StatsMySql(string connstring, string webDb, DataTable logTable = null, bool writeLog = false)
            :base(connstring, logTable, writeLog)
        { 
            this.webDb = webDb;
        }

        /// <summary>
        /// changes the preferred display columns to those passed in the argument
        /// </summary>
        /// <param name="pref">user display preferences</param>
        public void SetDisplayPreferences(Dictionary<string, string> pref) {
            
            if (columnsToDisplay == null)
                FindColumnsToDisplay();
            foreach (string tblName in pref.Keys) {
                if (columnsToDisplay[tblName][0] != pref[tblName]) {
                    string top = columnsToDisplay[tblName][0];
                    // automatically proposed top of display order will now be at the position where the new
                    // top (given by the user preference) used to be - simpy swap
                    columnsToDisplay[tblName][columnsToDisplay[tblName].FindIndex(x => x == pref[tblName])] = top;
                    columnsToDisplay[tblName][0] = pref[tblName];
                }
            }
             
        }

        /// <summary>
        /// sets default value to ColumnsToDisplay - preferrably use short text fields
        /// </summary>
        private void FindColumnsToDisplay()
        {
            columnsToDisplay = new Dictionary<string, List<string>>();
            foreach (string tab in ColumnTypes.Keys)
            {
                DataColumnCollection cols = ColumnTypes[tab];

                List<DataColumn> innerList = new List<DataColumn>();
                foreach (DataColumn col in cols)
                    innerList.Add(col);
                IComparer<DataColumn> comparer = new Common.ColumnDisplayComparer();
                innerList.Sort(comparer);
                columnsToDisplay[tab] = new List<string>();
                foreach (DataColumn c in innerList)
                {
                    columnsToDisplay[tab].Add(c.ColumnName);
                }
            }
        }

        /// <summary>
        /// extracts information from the COLUMNS table of INFORMATION_SCHEMA
        /// sets each column`s datatype, default value, nullability, autoincrement, uniqueness 
        /// and if the datatype is ENUM, sets the extended property as a List&lt;string&gt; labeled Common.Constants.ENUM_COLUMN_VALUES
        /// </summary>
        private void GetColumnTypes()
        {
            DataTable stats = fetchAll("SELECT COLUMNS.* FROM COLUMNS JOIN TABLES USING(TABLE_SCHEMA, TABLE_NAME) " +  
                "WHERE TABLE_TYPE != \"VIEW\" AND TABLES.TABLE_SCHEMA = \"" + webDb + 
                "\" ORDER BY COLUMNS.TABLE_NAME, ORDINAL_POSITION");
            
            Dictionary<string, DataColumnCollection> res = new Dictionary<string, DataColumnCollection>();

            WebDriverMySql tempWebDb = new WebDriverMySql(Common.Environment.project.ConnstringWeb);

            tempWebDb.BeginTransaction();

            tempWebDb.query("SET SQL_SELECT_LIMIT=0");
            foreach(string tableName in TableList()){
                DataTable schema = tempWebDb.fetchAll("SELECT * FROM ", dbe.Table(tableName));
                res[tableName] = schema.Columns;
            }
            tempWebDb.query("SET SQL_SELECT_LIMIT=DEFAULT");
            tempWebDb.CommitTransaction();
            tempWebDb = null;

            foreach (DataRow r in stats.Rows) {
                DataColumn col = res[r["TABLE_NAME"] as string][r["COLUMN_NAME"] as string];        // set ColumnName

                string columnType = r["COLUMN_TYPE"] as string;
                if (columnType.StartsWith("enum")) {        // enum type
                    string vals = columnType.Substring(5, columnType.Length - 5 - 1);       // the brackets
                    string[] split = vals.Split(new char[]{','});
                    List<string> EnumValues = new List<string>();
                    foreach (string s in split) {
                        EnumValues.Add(s.Substring(1, s.Length - 2));
                    }
                    col.ExtendedProperties.Add(CC.ENUM_COLUMN_VALUES, EnumValues);
                }

                
                if (col.DataType == typeof(string)) {
                    col.MaxLength = Convert.ToInt32(r["CHARACTER_MAXIMUM_LENGTH"]);
                }
                string extra = r["EXTRA"] as string;    // set AutoIncrement
                if (extra == "auto_increment")
                    col.AutoIncrement = true;
                if(!col.AutoIncrement)
                    col.ExtendedProperties.Add(Common.Constants.COLUMN_EDITABLE, true); // TODO add more restrictive rules...

                object colDefault = r["COLUMN_DEFAULT"];      // set DefaultValue
                if(!((colDefault is DBNull) || (colDefault.ToString() == String.Empty))){
                    string colDefaltStr = colDefault as string;
                    if(colDefaltStr == "CURRENT_TIMESTAMP")
                        col.ExtendedProperties.Remove(Common.Constants.COLUMN_EDITABLE);
                    else{
                       object parsed;
                       if(Common.Functions.TryTryParse(colDefaltStr, col.DataType, out parsed)){
                            col.DefaultValue = parsed;
                       }
                    }
                }

                col.AllowDBNull = ((string)r["IS_NULLABLE"]) == "YES";
                if(!(r["CHARACTER_MAXIMUM_LENGTH"] is DBNull)) col.MaxLength = Convert.ToInt32(r["CHARACTER_MAXIMUM_LENGTH"]);

                
            }       // for each row in stats
            columnTypes = res;
        }

        /// <summary>
        /// searches the db for FKs and converts them into FK objects
        /// </summary>
        /// <returns>Dictionary&lt;TableName, ListOfFKs&gt;</returns>
        public Dictionary<string, List<FK>> ForeignKeys() {
            Dictionary<string, List<FK>> res = new Dictionary<string, List<FK>>();
            DataTable stats = fetchAll("SELECT * FROM KEY_COLUMN_USAGE WHERE CONSTRAINT_SCHEMA = \""
                + webDb + "\" AND REFERENCED_COLUMN_NAME IS NOT NULL");

            foreach (string tblName in Tables) {
                res[tblName] = new List<FK>();
            }

            foreach (DataRow r in stats.Rows)
            {
                string tbl = r["TABLE_NAME"] as string;
                res[tbl].Add(new FK(tbl, r["COLUMN_NAME"] as string, r["REFERENCED_TABLE_NAME"] as string,
                    r["REFERENCED_COLUMN_NAME"] as string, ColumnsToDisplay[r["REFERENCED_TABLE_NAME"] as string][0]));
            }
            return res;        
        }

        /// <summary>
        /// finds the hierarchical relation within table only if valid for use in a NavTree
        /// </summary>
        /// <returns></returns>
        public List<FK> SelfRefFKs() {
            List<FK> res = new List<FK>();
            DataTable stats = fetchAll("SELECT * FROM KEY_COLUMN_USAGE WHERE CONSTRAINT_SCHEMA = \""
                + webDb + "\" AND TABLE_NAME = REFERENCED_TABLE_NAME AND REFERENCED_COLUMN_NAME IS NOT NULL");
            foreach (DataRow r in stats.Rows)
            {
                FK fk = (new FK(r["TABLE_NAME"] as string, r["COLUMN_NAME"] as string, r["REFERENCED_TABLE_NAME"] as string,
                    r["REFERENCED_COLUMN_NAME"] as string, r["REFERENCED_COLUMN_NAME"] as string));
                // the following checks will be done in the Architect and errors will be displayed to the user
                
                //if(res.Any(x => x.myTable == fk.myTable)) continue; // there can be only one sef-ref FK per table
                //if(PKs[fk.myTable].Count != 1) continue;    // must have a single-column primary key...
                //if (fk.refColumn != PKs[fk.myTable][0]) continue;   // ...to which the FK refers
                res.Add(fk);
            }
            return res;
        }

        /*
        // NOT USED in the current version - if it was, the indexes would be retrieved for the whole DB at once
        public List<List<string>> Indexes(string tableName)
        {
            DataTable stats = fetchAll("SELECT *, GROUP_CONCAT(COLUMN_NAME) AS COLUMNS FROM KEY_COLUMN_USAGE WHERE CONSTRAINT_SCHEMA = \""
                + webDb + "\" AND TABLE_NAME = \"" + tableName + "\" ORDER BY CONSTRAINT_NAME, ORDINAL_POSITION GROUP BY CONSTRAINT_NAME");
            List<List<string>> res = new List<List<string>>();
            foreach (DataRow r in stats.Rows)
            {
                res.Add(new List<string>(((string)r["COLUMNS"]).Split(',')));
            }
            return res;
        }
        */

        /// <summary>
        /// primary key columns for tables in the database
        /// </summary>
        /// <returns>TableName->ListOfColumns</returns>
        public Dictionary<string, List<string>> PrimaryKeyCols(){

            DataTable stats = fetchAll("SELECT TABLE_NAME, COLUMN_NAME FROM KEY_COLUMN_USAGE WHERE CONSTRAINT_SCHEMA = \""
                + webDb + "\" AND CONSTRAINT_NAME = \"PRIMARY\" "
                + " ORDER BY TABLE_NAME, ORDINAL_POSITION");

            Dictionary<string, List<string>> res = new Dictionary<string, List<string>>();
            string tblName;
            foreach (DataRow r in stats.Rows) {
                tblName = r["TABLE_NAME"] as string;
                if (!res.ContainsKey(tblName)) res[tblName] = new List<string>();
                res[tblName].Add(r["COLUMN_NAME"] as string);
            }
            return res;
        }


        /// <summary>
        /// tables without a primary key cannot participate in the interface - the primary key is used for navigation (see the Navigator class)
        /// </summary>
        /// <returns>list of the tables</returns>
        public List<string> TablesMissingPK() {
            DataTable stats = fetchAll("SELECT L.TABLE_NAME FROM (SELECT TABLE_NAME FROM TABLES WHERE TABLE_SCHEMA =  '"
                + webDb + "' AND TABLE_TYPE = 'BASE TABLE') AS L LEFT JOIN (SELECT TABLE_NAME FROM KEY_COLUMN_USAGE WHERE TABLE_SCHEMA =  '"
                + webDb + "' AND CONSTRAINT_NAME =  'PRIMARY' GROUP BY TABLE_NAME) AS R USING ( TABLE_NAME ) WHERE R.TABLE_NAME IS NULL");
            return new List<string>(from row in stats.AsEnumerable() select row["TABLE_NAME"] as string);
        }

        /// <summary>
        /// simply lists the tables in the database
        /// </summary>
        /// <returns></returns>
        public List<string> TableList() {
            DataTable tab = fetchAll("SELECT TABLE_NAME FROM TABLES WHERE TABLE_SCHEMA = '" + webDb + "' AND TABLE_TYPE = \"BASE TABLE\" ORDER BY TABLE_NAME");
            return new List<string>(from row in tab.AsEnumerable() select row["TABLE_NAME"] as string);
        }

        // these tabes are potential M2NMapping tables
        public List<string> TwoColumnTables()
        {
            DataTable tab = fetchAll("SELECT TABLE_NAME FROM COLUMNS WHERE TABLE_SCHEMA =  '" + webDb + "' GROUP BY TABLE_NAME " +
                "HAVING COUNT( * ) = 2");
            return new List<string>(from row in tab.AsEnumerable() select row["TABLE_NAME"] as string);
        }

        /// <summary>
        /// M:N mappings in the database as M2NMapping objects
        /// </summary>
        /// <returns></returns>
        public List<M2NMapping> FindMappings()
        {
            List<M2NMapping> res = new List<M2NMapping>();
            List<string> twoColTabs = TwoColumnTables();
            List<List<FK>> twoColFKs = new List<List<FK>>(from table in twoColTabs select FKs[table]);
            foreach (List<FK> currTabFKs in twoColFKs)
            {
                if (currTabFKs.Count == 2)
                {
                    // cannot say which table will use the mapping field (the one with more data)
                    //  ASK
                    res.Add(new M2NMapping(currTabFKs[0].refTable, currTabFKs[0].refColumn, currTabFKs[1].refTable,
                        currTabFKs[1].refColumn, currTabFKs[0].myTable, currTabFKs[0].refColumn,
                        currTabFKs[0].myColumn, currTabFKs[1].myColumn));
                    
                    res.Add(new M2NMapping(currTabFKs[1].refTable, currTabFKs[1].refColumn, currTabFKs[0].refTable,
                        currTabFKs[0].refColumn, currTabFKs[1].myTable, currTabFKs[1].refColumn,
                        currTabFKs[1].myColumn, currTabFKs[0].myColumn));
                }
            }
            return res;
        }

    }
}
