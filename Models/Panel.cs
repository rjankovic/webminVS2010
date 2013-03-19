using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using _min.Interfaces;
using _min.Common;
using System.Data;
using System.Runtime.Serialization;
using System.IO;

namespace _min.Models
{
    [DataContract]      // dont serialize iiner object - controls and fields
    public class Panel
    {
        [DataMember]
        public string tableName { get; private set; }
        [IgnoreDataMember]
        public List<Panel> children { get; private set; }
        [IgnoreDataMember]
        public List<Field> fields { get; private set; }    // including Docks
        [IgnoreDataMember]
        public List<Control> controls { get; private set; }
        [DataMember]
        public List<string> PKColNames { get; set; }
        [IgnoreDataMember]
        public DataRow PK { get; set; }
        [IgnoreDataMember]
        public DataRow RetrievedData { get; private set; }
        [IgnoreDataMember]
        public DataRow RetrievedInsertData { get; private set; }
        [IgnoreDataMember]
        private Panel _parent;
        [IgnoreDataMember]
        public Panel parent
        {
            get
            {
                return _parent;
            }
            set
            {
                if (_parent == null)
                    _parent = value;
                else throw new Exception("Panel already set");
            }
        }
        
        [IgnoreDataMember]
        public int panelId { get; set; }
        [DataMember]
        public PanelTypes type { get; set; }
        [DataMember]
        public string panelName {get; set;}
        [DataMember]
        public bool isBaseNavPanel { get; set; }
        [DataMember]
        public int? idHolder { get; set; }
        [DataMember]
        public int displayAccessRights { get; set; }

        public Panel(string tableName, int panelId, PanelTypes type, List<Panel> children,
            List<Field> fields, List<Control> controls, List<string> PKColNames, DataRow PK = null, Panel parent = null){
            this.tableName = tableName;
            this.panelId = panelId;
            this.children = children;
            this.fields = fields;
            this.controls = controls;
            this.PKColNames = PKColNames;
            this.PK = PK;
            this.parent = parent;
            this.type = type;
            if (this.controls == null) this.controls = new List<Control>();
            if (this.fields == null) this.fields = new List<Field>();
            if (this.PKColNames == null) this.PKColNames = new List<string>();
            if (this.children == null) this.children = new List<Panel>();
            foreach (Panel child in this.children) {
                child.parent = this;
            }
            foreach (Field f in this.fields) {
                f.panel = this;
            }
            foreach (Control c in this.controls) {
                c.panel = this;
            }
        }

        public string Serialize()
        {
            MemoryStream ms = new MemoryStream();
            DataContractSerializer ser = new DataContractSerializer(typeof(Panel));
            ser.WriteObject(ms, this);

            return Functions.StreamToString(ms);
        }

        public void AddChildren(List<Panel> children)
        {
            if (children.Count > 0 && this.children == null) this.children = new List<Panel>();
            foreach (Panel p in children)
            {
                if(this.children.Any(p2 => p2.panelId == p.panelId))
                    throw new Exception("Panel already contains a child with this id.");
                this.children.Add(p);
                p.parent = this;
            }
        }

        public void AddChild(Panel p) {
            if (this.children.Any(p2 => p2.panelId == p.panelId))
                throw new Exception("Panel already contains a child with this id.");
            this.children.Add(p);
            p.parent = this;        
        }


        public void SetCreationId(int id)
        {
            if (panelId == 0) panelId = id;
            else
            throw new Exception("Panel id already initialized");
            fields.ForEach(x => x.RefreshPanelId());
            controls.ForEach(x => x.RefreshPanelId());
        }

        public void SetParentPanel(Panel parentPanel)
        {
            if (parent == null) parent = parentPanel;
            else
                throw new Exception("Panel parent already initialized");
        }

        public void AddFields(List<Field> fields)
        {
            foreach (Field newField in fields) {
                if (this.fields.Any(f => f.fieldId == newField.fieldId && this.panelId != 0))
                    throw new Exception("Panel already contains a field with this id.");
                this.fields.Add(newField);
                newField.panel = this;
            }
        }

        public void AddControls(List<Control> controls)
        {
            foreach (Control newControl in controls)
            {
                int dupl = 0;
                if(this.panelId != 0)
                dupl = (from c in controls where c.action == newControl.action && c != newControl select c).Count();
                if (Convert.ToInt32(dupl) > 0)
                    throw new Exception("Panel already contains a control for this action.");
                this.controls.Add(newControl);
                newControl.panel = this;
            }
        }

        public void InitAfterDeserialization() {
            if (this.children != null || this.fields != null || this.controls != null)
                throw new Exception("Some of the collections have already been initializaed");
            this.children = new List<Panel>();
            this.fields = new List<Field>();
            this.controls = new List<Control>();
        }

        public void RetrieveDataFromFields() {
            DataTable tbl = new DataTable();
            DataTable insTbl = new DataTable();
            if(PK != null){
                foreach(DataColumn col in PK.Table.Columns)
                    tbl.Columns.Add(new DataColumn(col.ColumnName, col.DataType));
            }
            foreach (Field f in fields) {
                if (f is M2NMappingField) continue;
                if (f.value != null && !PK.Table.Columns.Contains(f.column))
                {
                    tbl.Columns.Add(new DataColumn(f.column, f.value.GetType()));
                    insTbl.Columns.Add(new DataColumn(f.column, f.value.GetType()));
                }
                else {
                    tbl.Columns.Add(new DataColumn(f.column, typeof(int)));
                    insTbl.Columns.Add(new DataColumn(f.column, typeof(int)));
                }
            }

            RetrievedData = tbl.NewRow();
            RetrievedInsertData = insTbl.NewRow();
            if (PK != null) {
                foreach (DataColumn col in PK.Table.Columns)
                    RetrievedData[col.ColumnName] = PK[col.ColumnName];
            }
            foreach (Field f in fields)
            {
                if (f is M2NMappingField) continue;
                if (f.value != null)
                {
                    RetrievedData[f.column] = f.value;
                    RetrievedInsertData[f.column] = f.value;
                }
                else {
                    RetrievedData[f.column] = DBNull.Value;
                    RetrievedInsertData[f.column] = DBNull.Value;
                }
            }
        }
    }
}
