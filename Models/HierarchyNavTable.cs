using System;
using System.Data;

namespace _min.Models
{
    /// <summary>
    /// Provides a strongly typed DataTable for hierarchical data in TreeControl-s.
    /// Contains four columns - [Id], [ParentId] (FK -> [Id]), [Caption] and [NavId] - the PK of the row in the containing control`s target panel`s table
    /// to the detail of which the menu item bound to this row leads.
    /// </summary>
    public class HierarchyNavTable : DataTable
    {
        public HierarchyRow this[int idx]
        {
            get { return (HierarchyRow)Rows[idx]; }
        }

        /// <summary>
        /// just for convenience - call Find on the table instead of Rows
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public HierarchyRow Find(int key) {
            return (HierarchyRow)Rows.Find(key);
        }

        public HierarchyNavTable()
        {
            Columns.Add(new DataColumn("Id", typeof(int)));
            Columns.Add(new DataColumn("ParentId", typeof(int)));
            Columns["ParentId"].AllowDBNull = true;
            Columns.Add(new DataColumn("Caption", typeof(string)));
            Columns.Add(new DataColumn("NavId", typeof(int)));
            Columns["NavId"].AllowDBNull = true;
            PrimaryKey = new DataColumn[] { Columns["Id"] };
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

    /// <summary>
    /// a row of a HierarchyNavTable - provided with properties to set the columns
    /// </summary>
    public class HierarchyRow : DataRow
    {
        public int Id
        {
            get { return (int)base["Id"]; }
            set { base["Id"] = value; }
        }

        public int? NavId
        {
            get
            {
                if (base["NavId"] != DBNull.Value)
                    return (int)base["NavId"];
                return null;
            }
            set
            {
                if (value != null)
                    base["NavId"] = (int)value;
                else
                    base["NavId"] = DBNull.Value;
            }
        }

        public int? ParentId    // DBNull can (and must) be simply passed as null
        {
            get
            {
                if (base["ParentId"] != DBNull.Value)
                    return (int)base["ParentId"];
                else
                    return null;
            }
            set
            {
                if (value != null)
                    base["ParentId"] = (int)value;
                else
                    base["ParentId"] = DBNull.Value;
            }
        }

        public string Caption
        {
            // nulls can occur if the user chooses a nullable column as the top representative in EditNav
            // (which would be foolish); are hand
            get {
                if (base["Caption"] == DBNull.Value)
                    return null;
                else
                    return (string)base["Caption"]; 
            }
            set {
                if (value == null)
                    base["Caption"] = DBNull.Value;
                else
                    base["Caption"] = value; 
            }
        }

        internal HierarchyRow(DataRowBuilder builder)
            : base(builder)
        { }

        public HierarchyRow[] GetHierarchyChildRows(string relationName) { 
            DataRow[] children = GetChildRows(relationName);
            HierarchyRow[] typedChildren = new HierarchyRow[children.Length];
            for (int i = 0; i < children.Length; i++) {
                typedChildren[i] = (HierarchyRow)(children[i]);
            }
            return typedChildren;
        }

          
    }
}
