using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Runtime.Serialization;
using System.IO;

using _min.Interfaces;
using _min.Common;
using _min.Templates;

using UControl = System.Web.UI.Control;
using WC = System.Web.UI.WebControls;

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
        public Panel panel
        {
            get
            {
                return _panel;
            }
            set
            {
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
        [DataMember]
        public bool independent { get; set; }   // for grid option controls
        [DataMember]
        public List<string> displayColumns { get; set; }

        public Control(int panelId, Panel panel, int targetPanelId, Panel targetPanel,
            DataTable data, List<string> PKColNames, UserAction action, bool independent = true)
        {
            this.panelId = panelId;
            this.data = data;
            this.PKColNames = PKColNames;
            this.action = action;
            this.panel = panel;
            this.targetPanel = targetPanel;
            this.targetPanelId = targetPanelId;
            this.independent = independent;
        }

        public Control(int panelId, Panel panel,
            DataTable data, List<string> PKColNames, UserAction action)
            : this(panelId, panel, panel.panelId, panel, data, PKColNames, action)
        {
        }

        public Control(int panelId, DataTable data, List<string> PKColNames, UserAction action)
        {
            this.panelId = panelId;
            this.data = data;
            this.PKColNames = PKColNames;
            this.action = action;
            this.independent = true;
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

        public void SetCreationId(int id)
        {
            if (this.controlId == null)
            {
                this.controlId = id;
            }
        }

        public void RefreshPanelId()
        {
            this.panelId = panel.panelId;
        }

        public virtual UControl ToUControl(WC.GridViewCommandEventHandler handler, string navigateUrl = null)
        {
            // take care of all the dependant controls as well
            WC.GridView grid = new WC.GridView();
            grid.DataKeyNames = PKColNames.ToArray();
            HashSet<UserAction> actions = new HashSet<UserAction>();
            actions.Add(this.action);
            IEnumerable<UserAction> actionsQuery = from control in panel.controls
                                                   where control.independent == false
                                                   select control.action;
            foreach (UserAction act in actionsQuery)
                actions.Add(act);

            grid.AutoGenerateColumns = false;
            grid.DataSource = data;
            
            WC.TemplateField tf = new WC.TemplateField();
            tf.HeaderTemplate = new SummaryGridCommandColumn(WC.ListItemType.Header);
            tf.FooterTemplate = new SummaryGridCommandColumn(WC.ListItemType.Footer);
            tf.ItemTemplate = new SummaryGridCommandColumn(WC.ListItemType.Item, actions);
            grid.Columns.Add(tf);


            foreach (string col in displayColumns)
            {
                WC.BoundField bf = new WC.BoundField();
                bf.DataField = col;
                bf.HeaderText = col;
                grid.Columns.Add(bf);
            }
            foreach (string col in PKColNames) {
                if (displayColumns.Contains(col)) continue;
                WC.BoundField bf = new WC.BoundField();
                bf.DataField = col;
                bf.Visible = false;
                grid.Columns.Add(bf);
            }

            grid.DataBind();
            /*
            foreach(WC.DataControlField f in grid.Columns){
                if (f is WC.BoundField) {
                    WC.BoundField bf = (WC.BoundField)f;
                    if (!displayColumns.Contains(bf.DataField))
                        f.Visible = false;
                }
            }*/
            grid.RowCommand += handler;
            grid.ID = "control" + controlId;
            return grid;
        }

        public virtual UControl ToUControl(WC.CommandEventHandler handler, string navigateUrl = null)
        {
            WC.Button button = new WC.Button();
            button.Text = this.action.ToString();
            button.CommandName = action.ToString();
            button.Command += (WC.CommandEventHandler)handler;
            button.ID = "control" + controlId;
            return button;
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
        public DataSet storedHierarchyDataSet { get; set; }
        [IgnoreDataMember]
        public HierarchyNavTable storedHierarchyData { get; set; }
        [IgnoreDataMember]
        public override DataTable data
        {
            get
            {
                return storedHierarchyData;
            }
            set
            {
                if (value is HierarchyNavTable)
                    storedHierarchyData = value as HierarchyNavTable;
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
            //this.data.DataSet = null;
            this.data.TableName = "data";
            ds.Tables.Add(this.data);
            storedHierarchyDataSet = storedHierarchyData.DataSet;
            //storedHierarchyDataSet.Tables.Add(storedHierarchyData);
            //storedHierarchyDataSet.Relations.Add("Hierarchy", 
            //    storedHierarchyData.Columns["Id"], storedHierarchyData.Columns["ParentId"], false);

            //reenable!!!
            //ds.Relations.Add("hierarchy", ds.Tables[0].Columns["Id"], ds.Tables[0].Columns["ParentId"]);
        }

        public UControl ToUControl(WC.MenuEventHandler handler, string navigateUrl = null)
        {
            if(panel.type != PanelTypes.MenuDrop) throw new ArgumentException(
                "MenuEventHandler can operate only on a Menu - within a MenuDrop panel");
            WC.Menu res = new WC.Menu();
            res.Orientation = WC.Orientation.Horizontal;
            WC.MenuItem item;
            foreach (DataRow r in storedHierarchyData.Rows)
            {
                if ((int)(r["ParentId"]) == 0)
                {
                    item = new WC.MenuItem(r["Caption"].ToString(), r["NavId"].ToString());
                    AddSubmenuForItem(r, item);
                    res.Items.Add(item);
                }
            }
            res.MenuItemClick += handler;
            return res;
        }

        public UControl ToUControl(EventHandler handler, string navigateUrl = null)
        {
            if (panel.type != PanelTypes.NavTree) throw new ArgumentException(
                 "This handler cannot handle events different from that fired by a TreeView in a NavtreePanel`s contorl");
            WC.TreeView res = new WC.TreeView();
            res.ShowLines = true;
            WC.TreeNode item;
            foreach (DataRow r in data.Rows)
            {
                if ((int)(r["ParentId"]) == 0)
                {
                    item = new WC.TreeNode(r["Caption"].ToString(), r["NavId"].ToString());
                    AddSubtreeForItem(r, item);
                    res.Nodes.Add(item);
                }
            }
            res.SelectedNodeChanged += handler;
            
            return res;
        }

        private void AddSubmenuForItem(DataRow row, WC.MenuItem item)
        {
            DataRow[] children = row.GetChildRows("Hierarchy");
            WC.MenuItem childItem;
            foreach (DataRow child in children)
            {
                childItem = new WC.MenuItem(child["Caption"].ToString(), child["NavId"].ToString());
                item.ChildItems.Add(childItem);
                AddSubmenuForItem(child, childItem);
            }
        }

        private void AddSubtreeForItem(DataRow row, WC.TreeNode item)
        {
            DataRow[] children = row.GetChildRows("Hierarchy");
            WC.TreeNode childItem;
            foreach (DataRow child in children)
            {
                childItem = new WC.TreeNode(child["Caption"].ToString(), child["NavId"].ToString());
                item.ChildNodes.Add(childItem);
                AddSubtreeForItem(child, childItem);
            }
        }
    }

}
