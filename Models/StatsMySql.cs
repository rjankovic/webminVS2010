using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using _min.Interfaces;
using System.Reflection;
using CC = _min.Common.Constants;

namespace _min.Models
{
    class StatsMySql : BaseDriverMySql, IStats 
    {
        private string webDb;


        private Dictionary<string, DataColumnCollection> columnTypes = null;
        private Dictionary<string, List<string>> columnsToDisplay = null;
        private Dictionary<string, List<M2NMapping>> mappings = null;
        private Dictionary<string, List<string>> globalPKs = null;
        private Dictionary<string, List<FK>> globalFKs = null;
        private List<string> tables = null;


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

        public Dictionary<string, List<M2NMapping>> Mappings {
            get {
                if (mappings == null) {
                    mappings = new Dictionary<string, List<M2NMapping>>();
                    List<string> tables = TableList();
                    foreach (string table in tables) {
                        mappings[table] = new List<M2NMapping>();
                    }
                    List<M2NMapping> amp = findMappings();
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

        public Dictionary<string, List<string>> PKs {
            get {
                if (globalPKs == null) globalPKs = GlobalPKs();
                return globalPKs;
            }
        }

        public Dictionary<string, List<FK>> FKs {
            get {
                if (globalFKs == null)
                    globalFKs = AllForeignKeys();
                return globalFKs;
            }
        }

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

        private void GetColumnTypes()   // extracts information from the COLUMNS table of INFORMATION_SCHEMA
        {
            // uneffective
            DataTable stats = fetchAll("SELECT COLUMNS.* FROM COLUMNS JOIN TABLES USING(TABLE_SCHEMA, TABLE_NAME) " +  
                "WHERE TABLE_TYPE != \"VIEW\" AND TABLES.TABLE_SCHEMA = \"" + webDb + 
                "\" ORDER BY COLUMNS.TABLE_NAME, ORDINAL_POSITION");
            
            Dictionary<string, DataColumnCollection> res = new Dictionary<string, DataColumnCollection>();
            foreach(string tableName in TableList()){
                DataTable schema = fetchSchema("SELECT * FROM `" + webDb + "`.`" + tableName + "`");
                schema.PrimaryKey = new DataColumn[] { };
                schema.Constraints.Clear();
                res[tableName] = schema.Columns;

            }

            foreach (DataRow r in stats.Rows) {
                /*
                if (tbl == null || tbl.TableName != r["TABLE_NAME"].ToString())
                {
                    tbl = new DataTable(r["TABLE_NAME"].ToString());
                    res.Add(tbl.TableName, tbl.Columns);
                }
                */
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

                //col.ExtendedProperties.Add(Common.Constants.FIELD_POSITION, Convert.ToInt32(r["ORDINAL_POSITION"]));
                //string typeStr = col.DataType.ToString() as string;      // set DataType
                //Type representedType = typeof(object);
                
                if (col.DataType == typeof(string)) { 
                    col.ExtendedProperties.Add("length", Convert.ToInt32(r["CHARACTER_MAXIMUM_LENGTH"]));
                }
                /*
                else switch (typeStr) {     // using fetchSchema instead
                    case "timestamp":
                    case "datetime":
                        representedType = typeof(DateTime);
                        break;
                    case "date":
                        representedType = typeof(DateTime);
                        col.ExtendedProperties.Add("DateOnly", true);
                        break;
                    case "int":
                        representedType = typeof(int);
                        break;
                    case "bigint":
                        representedType = typeof(long);
                        break;
                    case "tinyint":
                        representedType = typeof(short);
                        break;
                    case "float":
                        representedType = typeof(float);
                        break;
                    case "double":
                    case "decimal":
                        representedType = typeof(double);
                        break;
                    default:
                        if(typeStr.StartsWith("ënum"))
                        {
                            typeStr = typeStr.Remove(0, "enum('".Length);
                            // and hopefully replace `'` with `"`
                            typeStr = typeStr.Remove(typeStr.Length-1, 1).Replace("\"", "\\\"").Replace("'", "\"");
                            col.DataType = typeof(Enum);
                            col.ExtendedProperties.Add(Common.Constants.COLUMN_ENUM_VALUES, typeStr);
                        }
                        else
                            throw new Exception("Unrecognised column type: " + typeStr);
                        break;
                }
                col.DataType = representedType;
                */
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

                //tbl.Columns.Add(col);
                
            }       // for each row in stats
            columnTypes = res;
        }

        public List<FK> foreignKeys(string tableName)
        {
            
            List<FK> res = new List<FK>();
            DataTable stats = fetchAll("SELECT * FROM KEY_COLUMN_USAGE WHERE CONSTRAINT_SCHEMA = \"" 
                + webDb + "\" AND TABLE_NAME = \"" + tableName + "\" AND REFERENCED_COLUMN_NAME IS NOT NULL");
            foreach (DataRow r in stats.Rows)
            {
                res.Add(new FK(r["TABLE_NAME"] as string, r["COLUMN_NAME"] as string, r["REFERENCED_TABLE_NAME"] as string,
                    r["REFERENCED_COLUMN_NAME"] as string, ColumnsToDisplay[r["REFERENCED_TABLE_NAME"] as string][0]));
            }
            return res;
        }

        public Dictionary<string, List<FK>> AllForeignKeys() {
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

        public List<FK> selfRefFKs() {
            List<FK> res = new List<FK>();
            DataTable stats = fetchAll("SELECT * FROM KEY_COLUMN_USAGE WHERE CONSTRAINT_SCHEMA = \""
                + webDb + "\" AND TABLE_NAME = REFERENCED_TABLE_NAME AND REFERENCED_COLUMN_NAME IS NOT NULL");
            foreach (DataRow r in stats.Rows)
            {
                res.Add(new FK(r["TABLE_NAME"] as string, r["COLUMN_NAME"] as string, r["REFERENCED_TABLE_NAME"] as string,
                    r["REFERENCED_COLUMN_NAME"] as string, r["REFERENCED_COLUMN_NAME"] as string));
            }
            return res;
        }

        
        // finds the hierarchical relation within table only if valid for use in a NavTree
        public FK SelfRefFKStrict(string tableName)
        {
            List<FK> res = new List<FK>();
            DataTable stats = fetchAll("SELECT * FROM KEY_COLUMN_USAGE WHERE CONSTRAINT_SCHEMA = \""
                + webDb + "\" AND TABLE_NAME = REFERENCED_TABLE_NAME AND REFERENCED_COLUMN_NAME IS NOT NULL AND TABLE_NAME = '" 
                + tableName + "'");
            foreach (DataRow r in stats.Rows)
            {
                res.Add(new FK(r["TABLE_NAME"] as string, r["COLUMN_NAME"] as string, r["REFERENCED_TABLE_NAME"] as string,
                    r["REFERENCED_COLUMN_NAME"] as string, r["REFERENCED_COLUMN_NAME"] as string));
            }

            if (res.Count != 1) return null;
            FK fk = res[0];
            if (PKs[fk.myTable].Count != 1) return null;
            if (fk.refColumn != PKs[fk.myTable][0]) return null;
            return fk;
        }

        public List<List<string>> indexes(string tableName)
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

        public List<string> primaryKeyCols(string tableName)
        {
            DataTable stats = fetchAll("SELECT COLUMN_NAME FROM KEY_COLUMN_USAGE WHERE CONSTRAINT_SCHEMA = \""
                + webDb + "\" AND TABLE_NAME = \"" + tableName + "\" AND CONSTRAINT_NAME = \"PRIMARY\" "
                + " ORDER BY ORDINAL_POSITION");
            return new List<string>(from row in stats.AsEnumerable() select row["COLUMN_NAME"] as string);
        }

        public Dictionary<string, List<string>> GlobalPKs(){
            List<string> tables = TableList();
            Dictionary<string, List<string>> res = new Dictionary<string, List<string>>();
            foreach (string table in tables) {
                res[table] = primaryKeyCols(table);
            }
            return res;
        }

        public List<string> TablesMissingPK() {
            DataTable stats = fetchAll("SELECT L.TABLE_NAME FROM (SELECT TABLE_NAME FROM KEY_COLUMN_USAGE WHERE TABLE_SCHEMA =  '"
                + webDb + "' GROUP BY TABLE_NAME) AS L LEFT JOIN (SELECT TABLE_NAME FROM KEY_COLUMN_USAGE WHERE TABLE_SCHEMA =  '"
                + webDb + "' AND CONSTRAINT_NAME =  'PRIMARY' GROUP BY TABLE_NAME) AS R USING ( TABLE_NAME ) WHERE R.TABLE_NAME IS NULL");
            return new List<string>(from row in stats.AsEnumerable() select row["TABLE_NAME"] as string);
        }

        public List<string> TableList() {
            DataTable tab = fetchAll("SELECT TABLE_NAME FROM TABLES WHERE TABLE_SCHEMA = '" + webDb + "' AND TABLE_TYPE = \"BASE TABLE\" ORDER BY CREATE_TIME");
            return new List<string>(from row in tab.AsEnumerable() select row["TABLE_NAME"] as string);
        }

        public List<string> TwoColumnTables()
        {
            DataTable tab = fetchAll("SELECT TABLE_NAME FROM COLUMNS WHERE TABLE_SCHEMA =  '" + webDb + "' GROUP BY TABLE_NAME " +
                "HAVING COUNT( * ) = 2");
            return new List<string>(from row in tab.AsEnumerable() select row["TABLE_NAME"] as string);
        }

        public List<M2NMapping> findMappings()
        {
            List<M2NMapping> res = new List<M2NMapping>();
            List<string> twoColTabs = TwoColumnTables();
            List<List<FK>> FKs = new List<List<FK>>(from table in twoColTabs select foreignKeys(table));
            foreach (List<FK> currTabFKs in FKs)
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

        public DateTime TableCreation(string tableName) {
            return (DateTime)fetchSingle("SELECT CREATION_TIME FROM TABLES WHERE TABLE_SCHEMA = '",
                webDb, "' AND TABLE_NAME = '", tableName, "'"); 
        }

    }
}
