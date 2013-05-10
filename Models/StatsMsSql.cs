using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using _min.Interfaces;
using CC = _min.Common.Constants;
using _min.Models;

namespace _min.Models
{

    class StatsMsSql : IStats 
    {
        private BaseDriverMsSql driver;
        DbDeployableFactory dbe = new DbDeployableFactory();

        private Dictionary<string, DataColumnCollection> columnTypes = null;
        private Dictionary<string, List<string>> columnsToDisplay = null;
        private Dictionary<string, List<M2NMapping>> mappings = null;
        private Dictionary<string, List<string>> globalPKs = null;
        private Dictionary<string, List<FK>> globalFKs = null;
        private List<string> tables = null;

        /// <summary>
        /// stores resrutn value of GetColumnTYpes
        /// </summary>
        public Dictionary<string, DataColumnCollection> ColumnTypes {       // actually Extended datacolumn collection
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
        public Dictionary<string, List<string>> ColumnsToDisplay
        {
            get
            {
                if (columnsToDisplay == null)
                    GetColumnTypes();
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

        public StatsMsSql(BaseDriverMsSql driver)
        {
            this.driver = driver;
        }

        /// <summary>
        /// changes the preferred display columns to those passed in the argument
        /// </summary>
        /// <param name="pref">user display preferences</param>
        public void SetDisplayPreferences(Dictionary<string, string> pref) {
            
            foreach (string tblName in pref.Keys) {
                if (ColumnsToDisplay[tblName][0] != pref[tblName]) {
                    string top = columnsToDisplay[tblName][0];
                    // automatically proposed top of display order will now be at the position where the new
                    // top (given by the user preference) used to be - simpy swap
                    columnsToDisplay[tblName][columnsToDisplay[tblName].FindIndex(x => x == pref[tblName])] = top;
                    columnsToDisplay[tblName][0] = pref[tblName];
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
            DataTable stats = driver.fetchAll("SELECT COLS.*,"
                 +" COLUMNPROPERTY(OBJECT_ID(COLS.TABLE_NAME), COLS.COLUMN_NAME, 'IsIdentity') AS 'HAS_IDENTITY'"
                 +" FROM INFORMATION_SCHEMA.COLUMNS AS COLS"
                 +" JOIN INFORMATION_SCHEMA.TABLES AS TBLS ON COLS.TABLE_NAME = TBLS.TABLE_NAME " +  
                "WHERE TBLS.TABLE_SCHEMA = 'dbo' AND TABLE_TYPE = 'BASE TABLE' ORDER BY COLS.TABLE_NAME, ORDINAL_POSITION");
            
            Dictionary<string, DataColumnCollection> res = new Dictionary<string, DataColumnCollection>();

            BaseDriverMsSql tempWebDb = new BaseDriverMsSql(Common.Environment.project.ConnstringWeb);
            
            tempWebDb.BeginTransaction();

            //tempWebDb.query("SET SQL_SELECT_LIMIT=0");
            foreach(string tableName in TableList()){
                DataTable schema = tempWebDb.fetchAll("SELECT TOP 0 * FROM ", dbe.Table(tableName));
                res[tableName] = schema.Columns;
            }
            //tempWebDb.query("SET SQL_SELECT_LIMIT=DEFAULT");
            tempWebDb.CommitTransaction();
            tempWebDb = null;


            foreach (DataRow r in stats.Rows) {
                DataColumn col = res[r["TABLE_NAME"] as string][r["COLUMN_NAME"] as string];        // set ColumnName


                //string dataType = r["DATA_TYPE"] as string;
                
                
                if (col.DataType == typeof(string)) {
                    col.MaxLength = Convert.ToInt32(r["CHARACTER_MAXIMUM_LENGTH"]);
                }
                bool hasIdentity = (int)r["HAS_IDENTITY"] == 1;    // set AutoIncrement
                if (hasIdentity)
                    col.AutoIncrement = true;
                if(!col.AutoIncrement)
                    col.ExtendedProperties.Add(Common.Constants.COLUMN_EDITABLE, true); // TODO add more restrictive rules...

                object colDefault = r["COLUMN_DEFAULT"];      // set DefaultValue
                //if(!((colDefault is DBNull) || (colDefault.ToString() == String.Empty))){
                    //string colDefaltStr = colDefault as string;
                    //if(colDefaltStr == "CURRENT_TIMESTAMP")
                    //    col.ExtendedProperties.Remove(Common.Constants.COLUMN_EDITABLE);
                    //else{
                       //object parsed;
                       //if(Common.Functions.TryTryParse(colDefaltStr, col.DataType, out parsed)){
                       //     col.DefaultValue = parsed;
                       //}
                    //}
                //}

                col.AllowDBNull = ((string)r["IS_NULLABLE"]) == "YES";
                if(!(r["CHARACTER_MAXIMUM_LENGTH"] is DBNull)) col.MaxLength = Convert.ToInt32(r["CHARACTER_MAXIMUM_LENGTH"]);

                
            }       // for each row in stats
            columnTypes = res;

            ColumnsToDisplay = new Dictionary<string, List<string>>();
            IComparer<DataColumn> comparer = new Common.ColumnDisplayComparer();
            foreach (string tableName in columnTypes.Keys) {
                List<DataColumn> innerList = new List<DataColumn>();
                foreach (DataColumn col in columnTypes[tableName])
                    innerList.Add(col);
                innerList.Sort(comparer);
                columnsToDisplay[tableName] = (from DataColumn c in innerList select c.ColumnName).ToList<string>();
            
            }
            
            foreach (string tableName in FKs.Keys) {
                foreach (FK fk in FKs[tableName]) { 
                    columnTypes[fk.myTable][fk.myColumn].ExtendedProperties["FK"] = fk;
                }
            }

        }

        /// <summary>
        /// searches the db for FKs and converts them into FK objects
        /// </summary>
        /// <returns>Dictionary&lt;TableName, ListOfFKs&gt;</returns>
        public Dictionary<string, List<FK>> ForeignKeys() {
            Dictionary<string, List<FK>> res = new Dictionary<string, List<FK>>();
            //!!!!!!!!
            DataTable stats = driver.fetchAll(
@"SELECT CONSTRAINT_NAME = REF_CONST.CONSTRAINT_NAME,
TABLE_CATALOG = FK.TABLE_CATALOG,
TABLE_SCHEMA = FK.TABLE_SCHEMA,
TABLE_NAME = FK.TABLE_NAME,
COLUMN_NAME = FK_COLS.COLUMN_NAME,
REFERENCED_TABLE_CATALOG = PK.TABLE_CATALOG,
REFERENCED_TABLE_SCHEMA = PK.TABLE_SCHEMA,
REFERENCED_TABLE_NAME = PK.TABLE_NAME,
REFERENCED_COLUMN_NAME = PK_COLS.COLUMN_NAME
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS REF_CONST
INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS FK
ON REF_CONST.CONSTRAINT_CATALOG = FK.CONSTRAINT_CATALOG
AND REF_CONST.CONSTRAINT_SCHEMA = FK.CONSTRAINT_SCHEMA
AND REF_CONST.CONSTRAINT_NAME = FK.CONSTRAINT_NAME
AND FK.CONSTRAINT_TYPE = 'FOREIGN KEY'
INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS PK ON REF_CONST.UNIQUE_CONSTRAINT_CATALOG = PK.CONSTRAINT_CATALOG
AND REF_CONST.UNIQUE_CONSTRAINT_SCHEMA = PK.CONSTRAINT_SCHEMA
AND REF_CONST.UNIQUE_CONSTRAINT_NAME = PK.CONSTRAINT_NAME
AND PK.CONSTRAINT_TYPE = 'PRIMARY KEY'
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE FK_COLS ON REF_CONST.CONSTRAINT_NAME = FK_COLS.CONSTRAINT_NAME
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE PK_COLS ON PK.CONSTRAINT_NAME = PK_COLS.CONSTRAINT_NAME");
//HAVING TABLE_CATALOG = REFERENCED_TABLE_CATALOG"); (somehow should be here)

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
            return FKs.SelectMany(kv => from fk in kv.Value where fk.refTable == fk.myTable select fk).ToList<FK>();
        }

        /// <summary>
        /// primary key columns for tables in the database
        /// </summary>
        /// <returns>TableName->ListOfColumns</returns>
        public Dictionary<string, List<string>> PrimaryKeyCols(){

            DataTable stats = driver.fetchAll("SELECT ccu.TABLE_NAME, ccu.COLUMN_NAME " +
                    "FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc " +
                    "JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu ON tc.CONSTRAINT_NAME = ccu.Constraint_name " +
                    "WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY' AND tc.TABLE_SCHEMA = 'dbo'");

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
            return (from tbl in Tables where !PKs.ContainsKey(tbl) select tbl).ToList<string>();
        }

        /// <summary>
        /// simply lists the tables in the database
        /// </summary>
        /// <returns></returns>
        public List<string> TableList() {
            DataTable tab = driver.fetchAll("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' "
            + "AND TABLE_TYPE = ", dbe.InObj("BASE TABLE"), " ORDER BY TABLE_NAME");
            return new List<string>(from row in tab.AsEnumerable() select row["TABLE_NAME"] as string);
        }

        // these tabes are potential M2NMapping tables
        public List<string> TwoColumnTables()
        {
            DataTable tab = driver.fetchAll("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA =  'dbo' GROUP BY TABLE_NAME " +
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
