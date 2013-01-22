using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;
using _min.Models;
using _min.Interfaces;
using System.Data;
using System.Runtime.Serialization;

namespace _min.Models
{
    [DataContract]
    [KnownType(typeof(M2NMapping))]
    public class FK : IEquatable<FK>
    {
        [IgnoreDataMember]
        public IWebDriver driver { 
            get {
                return driver;
            }
            set {
                if (driver != null) throw new Exception("Driver already set.");
                driver = value;
            }
        }
        [DataMember]
        public string myTable { get; private set; }
        [DataMember]
        public string myColumn { get; private set; }
        [DataMember]
        public string refTable { get; private set; }
        [DataMember]
        public string refColumn { get; private set; }
        [DataMember]
        public string displayColumn { get; set; }
        [DataMember]
        public Dictionary<string, int> options { get; private set; }


        public FK(//string fk_table, string fk_column, 
            string myTable, string myColumn,
            string refTable, string refColumn,
            string displayColumn) {
                this.myTable = myTable;
                this.myColumn = myColumn;
                this.refTable = refTable;
                this.refColumn = refColumn;
                this.displayColumn = displayColumn;
                //this.driver = driver;
        }

        //these three methods are the only real meaning of this class

        public bool  validateInput(string inputValue)
        {
            return options.ContainsKey(inputValue);
        }

        public int valueForInput(string inputValue) {
            return options[inputValue];
        }

        public string CaptionForValue(int value) {
            if (driver == null) throw new NullReferenceException("No driver assigned");
            return driver.fetchSingle("SELECT `", displayColumn, 
                "` FROM `", refTable, "` WHERE `", refColumn, "` = ", value) as string;
        }

        public void refreshOptions() {  // need to call this to fill FK with data before use
            if (driver == null) throw new NullReferenceException("No driver assigned");
            DataTable tab = driver.fetchFKOptions(this);
            if ((tab.Columns[0].DataType != typeof(string)) || (tab.Columns[1].DataType != typeof(int)))
            {
                throw new Exception("Unsuitable foreign key for FK");
            }
            options.Clear();
            foreach (DataRow row in tab.Rows)
            {
                options.Add((string)row[0], (int)row[1]);
            }
        }

        // initially redefined becase of Architect.checkPanelProposal checking whether matching FKs still exist in the db
        public bool Equals(FK other)
        {
            if (other == null) return false;

            return this.displayColumn == other.displayColumn
                && this.myColumn == other.myColumn
                && this.myTable == other.myTable
                && this.refColumn == other.refColumn
                && this.refTable == other.refTable;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            FK FKObj = obj as FK;
            if (FKObj == null)
                return false;
            else
                return Equals(FKObj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    [DataContract]
    public class M2NMapping : FK, IEquatable<M2NMapping>
    {
        [DataMember]
        public string mapTable { get; private set; }
        [DataMember]
        public string mapRefColumn { get; private set; }
        [DataMember]
        public string mapMyColumn { get; private set; }
        
        
        public M2NMapping(
            string myTable, string myColumn, string refTable, string refColumn, string mapTable,
            string displayColumn, string mapMyColumn, string mapRefColumn)
                :base(myTable, myColumn, refTable, refColumn, displayColumn){
            this.mapTable = mapTable;
            this.mapMyColumn = mapMyColumn;
            this.mapRefColumn = mapRefColumn;
        }

        public bool validateWholeInput(List<string> inputValues)
        {
            foreach (string iv in inputValues)
                if (!validateInput(iv))
                    return false;
            return true;
        }

        public List<int> valuesForInput(List<string> inputValues) { 
            List<int> res = new List<int>();
            foreach (string iv in inputValues) {
                res.Add(valueForInput(iv));
            }
            return res;
        }

        public List<string> CaptionsForValues(List<int> values) { 
            List<string> res = new List<string>();
            foreach(int val in values) {
                res.Add(CaptionForValue(val));
            }
            return res;
        }

        public void unMap(int key) {        // clears mapping for given key
            if (driver == null) throw new NullReferenceException("No driver assigned");
            driver.UnmapM2NMappingKey(this, key);
        }

        public void mapVals(int key, int[] vals) {   // maps to given key
            if (driver == null) throw new NullReferenceException("No driver assigned");
            driver.MapM2NVals(this, key, vals);
            DataTable table = new DataTable();
        }


        // initial redefined becase of Architect.checkPanelProposal checking whether matching FKs still exist in the db
        public bool Equals(M2NMapping other)
        {
            if (other == null) return false;

            return
                ((this as FK).Equals(other as FK))
                && this.mapMyColumn == other.mapMyColumn
                && this.mapRefColumn == other.mapRefColumn
                && this.mapTable == other.mapTable;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            M2NMapping M2NObj = obj as M2NMapping;
            if (M2NObj == null)
                return false;
            else
                return Equals(M2NObj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }


}
