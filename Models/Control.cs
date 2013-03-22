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
using _min.Controls;

using UControl = System.Web.UI.Control;
using WC = System.Web.UI.WebControls;

namespace _min.Models
{
    [DataContract]
    [KnownType(typeof(TreeControl))]
    [KnownType(typeof(NavTableControl))]
    public class Control
    {
        [IgnoreDataMember]
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
        public List<string> displayColumns { get; set; }

        public virtual Dictionary<UserAction, int> ActionsDicitionary {
            get {
                return new Dictionary<UserAction, int> { { action, (int)targetPanelId } };
            }
        }

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
            : this(panelId, panel, panel.panelId, panel, data, PKColNames, action)
        {
        }

        public Control(int panelId, DataTable data, List<string> PKColNames, UserAction action)
        {
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

        public virtual UControl ToUControl(WC.CommandEventHandler handler, string navigateUrl = null)
        {
            WC.Button button = new WC.Button();
            button.Text = this.action.ToString();
            button.CommandName = "_" + action.ToString();
            button.Command += (WC.CommandEventHandler)handler;
            if (action == UserAction.Delete) button.OnClientClick = "return confirm('Really?')";
            button.ID = "Control" + controlId;
            return button;
        }

    }

    [DataContract]
    [KnownType(typeof(FK))]
    public class NavTableControl : Control {
        [DataMember]
        public List<FK> FKs { get; private set; }
        [DataMember]
        public List<UserAction> actions { get; private set; }

        public override Dictionary<UserAction, int> ActionsDicitionary
        {
            get
            {
                Dictionary<UserAction, int> res = new Dictionary<UserAction, int>();
                foreach (UserAction u in actions) {
                    res.Add(u, (int)targetPanelId);
                }
                return res;
            }
        }

        public NavTableControl(int panelId, Panel panel, int targetPanelId, Panel targetPanel,
            DataTable data, List<string> PKColNames, List<FK> Fks, List<UserAction> actions)
            :base(panelId, panel, targetPanelId, targetPanel, data, PKColNames, UserAction.Multiple)
        {
            this.FKs = Fks;
            this.actions = actions;
        }

        public NavTableControl(int panelId, DataTable data, List<string> PKColNames, List<FK> FKs, List<UserAction> actions)
            :base(panelId, data, PKColNames, UserAction.Multiple)
        {
            this.FKs = FKs;
            this.actions = actions;
        
        }

        public virtual UControl ToUControl(WC.GridViewCommandEventHandler handler, string navigateUrl = null)
        {
            // take care of all the dependant controls as well
            WC.GridView grid = new WC.GridView();
            grid.DataKeyNames = PKColNames.ToArray();
            
            grid.AutoGenerateColumns = false;
            grid.DataSource = data;

            grid.AllowPaging = grid.Rows.Count > 15;
            grid.PageSize = 15;

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
            foreach (string col in PKColNames)
            {
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
            grid.ID = "Control" + controlId;
            return grid;
        }

    }

    [DataContract]
    [KnownType(typeof(HierarchyNavTable))]
    public class TreeControl : Control
    {
        [DataMember]
        public List<UserAction> actions { get; private set; }
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

        public override Dictionary<UserAction, int> ActionsDicitionary
        {
            get
            {
                Dictionary<UserAction, int> res = new Dictionary<UserAction, int>();
                foreach (UserAction u in actions)
                {
                    res.Add(u, (int)targetPanelId);
                }
                return res;
            }
        }

        public TreeControl(int panelId, HierarchyNavTable data, string PKColName,    // tree controls must have a single-column primary key
            string parentColName, string displayColName,
            List<UserAction> actions)
            : base(panelId, data, PKColName, UserAction.Multiple)
        {
            this.actions = actions;
            this.parentColName = parentColName;
            this.displayColName = displayColName;
            this.displayColumns = new List<string> { displayColName };
            ds = new DataSet();
            //this.data.DataSet = null;
            this.data.TableName = "data";
            ds.Tables.Add(this.data);
            storedHierarchyDataSet = storedHierarchyData.DataSet;
        }

        public TreeControl(int panelId, HierarchyNavTable data, string PKColName,    // tree controls must have a single-column primary key
    string parentColName, string displayColName,
    UserAction action)
            : base(panelId, data, PKColName, action)
        {
            this.actions = new List<UserAction> { action };
            this.parentColName = parentColName;
            this.displayColName = displayColName;
            this.displayColumns = new List<string> { displayColName };
            ds = new DataSet();
            //this.data.DataSet = null;
            this.data.TableName = "data";
            ds.Tables.Add(this.data);
            storedHierarchyDataSet = storedHierarchyData.DataSet;
        }

        public UControl ToUControl(WC.MenuEventHandler handler, string navigateUrl = null)
        {
            if(panel.type != PanelTypes.MenuDrop) throw new ArgumentException(
                "MenuEventHandler can operate only on a Menu - within a MenuDrop panel");
            WC.Menu res = new WC.Menu();
            res.Orientation = WC.Orientation.Horizontal;
            WC.MenuItem item;
            foreach (HierarchyRow r in storedHierarchyData.Rows)
            {
                if (r.ParentId == null)
                {
                    item = new WC.MenuItem(r.Caption, r.NavId.ToString());
                    AddSubmenuForItem(r, item);
                    res.Items.Add(item);
                }
            }
            res.MenuItemClick += handler;
            res.ID = "Control" + controlId;
            return res;
        }

        public override UControl ToUControl(WC.CommandEventHandler handler, string navigateUrl = null)
        {
            TreeNavigatorControl tn = new TreeNavigatorControl(storedHierarchyData, actions);
            tn.ActionChosen += handler;
            return tn;
        }

        private void AddSubmenuForItem(HierarchyRow row, WC.MenuItem item)
        {
            DataRow[] children = row.GetChildRows("Hierarchy");
            WC.MenuItem childItem;
            foreach (HierarchyRow child in children)
            {
                childItem = new WC.MenuItem(child.Caption, child.NavId.ToString());
                item.ChildItems.Add(childItem);
                AddSubmenuForItem(child, childItem);
            }
        }
    }

}
