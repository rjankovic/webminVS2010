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
 
        [DataMember]
        public string myTable { get; protected set; }
        [DataMember]
        public string myColumn { get; protected set; }
        [DataMember]
        public string refTable { get; protected set; }
        [DataMember]
        public string refColumn { get; protected set; }
        [DataMember]
        public string displayColumn { get; set; }
        

        public FK( 
            string myTable, string myColumn,
            string refTable, string refColumn,
            string displayColumn) {
                this.myTable = myTable;
                this.myColumn = myColumn;
                this.refTable = refTable;
                this.refColumn = refColumn;
                this.displayColumn = displayColumn;
        }

        // initially redefined becase of Architect.CheckPanelProposal checking whether matching FKs still exist in the db
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
        
        // initially redefined becase of Architect.checkPanelProposal checking whether matching FKs still exist in the db
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
