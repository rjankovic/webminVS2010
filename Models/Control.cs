using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using _min.Interfaces;
using _min.Common;
using System.Runtime.Serialization;
using System.IO;

namespace _min.Models
{
    [DataContract]
    [KnownType(typeof(TreeControl))]
    public class Control
    {
        [DataMember]
        public int? controlId { get; set; }  // better setter once...
        [DataMember]
        public int? panelId { get; set; }
        [IgnoreDataMember]
        private Panel _panel;
        public Panel panel {
            get {
                return _panel;
            }
            set {
                if (_panel == null)
                {
                    _panel = value;
                    this.panelId = value.panelId;
                }
                else throw new Exception("Panel already set");
            }
        }
        [IgnoreDataMember]
        public virtual DataTable data { get; set; }
        [DataMember]
        public List<string> PKColNames { get; private set; }
        [DataMember]
        public Common.UserAction action { get; private set; }

        [DataMember]
        public int editationRightsRequired { get; private set; }
        [DataMember]
        public int insertRightsRequired { get; private set; }

        //indicates that the control shold appear in every row of a NavTable
        [DataMember]
        public bool navTableSpan { get; set; }
        [DataMember]
        public string navTableCopyCaption { get; set; }
        [DataMember]
        public string navTableDeleteCaption { get; set; }
        [DataMember]
        public string navTableEditCaption { get; set; }
        [DataMember]
        public bool navThroughRecords { get; set; }
        [DataMember]
        public bool navThroughPanels { get; set; } // duplicity but...
        [DataMember]
        public int? targetPanelId { get; set; }
        [IgnoreDataMember]
        public Panel targetPanel { get; set; }

        public Control(int panelId, Panel panel, int targetPanelId, Panel targetPanel,  
            DataTable data, List<string> PKColNames, UserAction action)
        {
            this.panelId = panelId;
            this.data = data;
            this.PKColNames = PKColNames;
            this.action = action;
            this.panel = panel;
            this.targetPanel = targetPanel;
            this.targetPanelId = targetPanelId;
        }

        public Control(int panelId, Panel panel,
            DataTable data, List<string> PKColNames, UserAction action)
            :this(panelId, panel, panel.panelId, panel, data, PKColNames, action)
        {
        }

        public Control(int panelId, DataTable data, List<string> PKColNames, UserAction action) {
            this.panelId = panelId;
            this.data = data;
            this.PKColNames = PKColNames;
            this.action = action;
        }

        public Control(int panelId, DataTable data, string PKColName, UserAction action)
            : this(panelId, data, new List<string>(new string[] { PKColName }), action)
        { }

        public Control(int panelId, string caption, UserAction action)
            : this(panelId, new DataTable(), "caption", action)
        {
            data.Columns.Add("caption");
            data.Rows.Add(caption);
        }

        public string Serialize()
        {
            MemoryStream ms = new MemoryStream();
            DataContractSerializer ser = new DataContractSerializer(typeof(Control));
            ser.WriteObject(ms, this);

            return Functions.StreamToString(ms);
        }

        public void SetCreationId(int id) {
            if (this.controlId == null) {
                this.controlId = id;
            }
        }

        public void RefreshPanelId() {
            this.panelId = panel.panelId;
        }

    }


    [DataContract]
    [KnownType(typeof(HierarchyNavTable))]
    public class TreeControl : Control
    {
        [DataMember]
        public string parentColName { get; private set; }
        [DataMember]
        public string displayColName { get; private set; }
        [DataMember]
        public HierarchyNavTable  hierarchyData { get; set; }
        [IgnoreDataMember]
        public override DataTable data
        {
            get
            {
                return hierarchyData;
            }
            set
            {
                if(value is HierarchyNavTable) 
                    hierarchyData = value as HierarchyNavTable;
                else
                    throw new FormatException("TreeControl can be filled with as HierarchyNavTable only");
            }
        }
        [IgnoreDataMember]
        public DataSet ds { get; private set; }
        [DataMember]
        public string recursiveFKCol { get; private set; }
        [DataMember]
        public readonly bool storeHierarchy;

        public TreeControl(int panelId, HierarchyNavTable data, string PKColName,    // tree controls must have a single-column primary key
            string parentColName, string displayColName,
            UserAction action)
                : base(panelId, data, PKColName, action) 
        {
            this.parentColName = parentColName;
            this.displayColName = displayColName;
            ds = new DataSet();
            this.data.TableName = "data";
            ds.Tables.Add(this.data);
            
            //reenable!!!
            //ds.Relations.Add("hierarchy", ds.Tables[0].Columns["Id"], ds.Tables[0].Columns["ParentId"]);
        }

    }

}
