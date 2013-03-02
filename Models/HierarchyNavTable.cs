﻿using System;
using System.Data;
using _min.Models;
using _min.Common;

namespace _min.Models
{
    
    public class HierarchyNavTable : DataTable
    {
        
        public HierarchyRow this[int idx]
        {
            get { return (HierarchyRow)Rows[idx]; }
        }

        public HierarchyRow Find(int key) {
            return (HierarchyRow)Rows.Find(key);
        }

        public HierarchyNavTable()
        {
            Columns.Add(new DataColumn("Id", typeof(int)));
            Columns.Add(new DataColumn("ParentId", typeof(int)));
            Columns.Add(new DataColumn("Caption", typeof(string)));
            Columns.Add(new DataColumn("NavId", typeof(int)));
            PrimaryKey = new DataColumn[] { Columns["Id"] };
            //Columns["ParentId"].AllowDBNull = true;   // should use NULL, not 0
            //Columns["NavId"].AllowDBNull = true;
            Columns["Id"].AutoIncrementSeed = 1;
            Columns["Id"].AutoIncrement = true;
        }

        public void Add(HierarchyRow row)
        {
            Rows.Add(row);
        }

        public void Remove(HierarchyRow row)
        {
            Rows.Remove(row);
        }

        public HierarchyRow GetNewRow()
        {
            HierarchyRow row = (HierarchyRow)NewRow();

            return row;
        }

        protected override Type GetRowType()
        {
            return typeof(HierarchyRow);
        }

        protected override DataRow NewRowFromBuilder(DataRowBuilder builder)
        {
            return new HierarchyRow(builder);
        }
    }

    public class HierarchyRow : DataRow
    {
        public int Id
        {
            get { return (int)base["Id"]; }
            set { base["Id"] = value; }
        }

        public int NavId
        {
            get { return (int)base["NavId"]; }
            set { base["NavId"] = (int)value; }
        }

        public int ParentId
        {
            get { return (int)base["ParentId"]; }
            set { base["ParentId"] = (int)value; }
        }

        public string Caption
        {
            get { return (string)base["Caption"]; }
            set { base["Caption"] = value; }
        }

        internal HierarchyRow(DataRowBuilder builder)
            : base(builder)
        {
            /*
            Id = 0;
            ParentId = 0;
            NavId = 0;
            Caption = string.Empty;
             */
        }
    }
}
