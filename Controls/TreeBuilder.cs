using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Web.UI.WebControls;
using System.ComponentModel;
using System.Web.UI;
using _min.Models;
using MPanel = _min.Models.Panel;
using WC = System.Web.UI.WebControls;
using System.Data;

namespace _min.Controls
{
    /// <summary>
    /// Used in the editation of the main menu; consists of a treeview of menu nodes and a select box of available panels. Nodes can be added / deleted / renamed
    /// and bound / unbound form panels. A node not bound to any panel remains in the menu.
    /// </summary>
    [ToolboxData("<{0}:TreeBuilderControl runat=\"server\"></{0}:TreeBuilderControl>")]
    public class TreeBuilderControl : CompositeControl
    {
        private ListBox panelList = new ListBox();
        private TreeView menuTree = new TreeView();
        private TextBox newLabelTB = new TextBox();
        private Button addButton = new Button();
        private Button removeButton = new Button();
        private Button bindButton = new Button();
        private Button unbindButton = new Button();
        private Button renameButton = new Button();
        private DataSet hierarchyDataset = new DataSet();
        private HierarchyNavTable menuHierarchy = null;
        private DataTable panelsTable = null;

        /// <summary>
        /// stores the orinal hierarchy in a dataset that will be saved to viewstatee so that this doesn`t need to be called upon every postback.
        /// </summary>
        /// <param name="hierarchy">The data property of the original menu</param>
        /// <param name="basePanel">the root panel of the architecture</param>
        public void SetInitialState(DataTable hierarchy, MPanel basePanel)
        {
            hierarchyDataset = new DataSet();
            menuHierarchy = new HierarchyNavTable();
            panelsTable = new DataTable();
            panelsTable.Columns.Add("Id", typeof(int));
            panelsTable.Columns.Add("Name", typeof(string));
            menuHierarchy.TableName = "menuHierarchy";
            menuHierarchy.Merge(hierarchy);
            hierarchyDataset.Tables.Add(menuHierarchy);
            hierarchyDataset.Relations.Add(new DataRelation("Hierarchy", menuHierarchy.Columns["Id"], menuHierarchy.Columns["ParentId"], true));
            
            AddPanelToList(basePanel);
            panelsTable.TableName = "panelsTable";
            hierarchyDataset.Tables.Add(panelsTable);
            ViewState["hierarchyDS"] = hierarchyDataset;
        }

        private string _ID;
        public override string ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        /// <summary>
        /// the changed (and changing) menu hierarchy
        /// </summary>
        public HierarchyNavTable Hierarchy {
            get {
                return menuHierarchy;
            }
        }

        /// <summary>
        /// called upon building the TreeView from the hierarchy table - creates a new Treenode and sets its parent to the "itme"
        /// </summary>
        /// <param name="row"></param>
        /// <param name="item"></param>
        private void AddSubtreeForItem(HierarchyRow row, WC.TreeNode item)
        {
            HierarchyRow[] children = row.GetHierarchyChildRows("Hierarchy");
            
            WC.TreeNode childItem;
            foreach (HierarchyRow child in children)
            {
                childItem = new WC.TreeNode(child.Caption, child.Id.ToString());
                item.ChildNodes.Add(childItem);
                AddSubtreeForItem(child, childItem);
            }
        }

        /// <summary>
        /// initialization - add a Panel to the select box
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="level"></param>
        private void AddPanelToList(MPanel panel, int level = 0) {
            DataRow r = panelsTable.NewRow();
            r["Id"] = panel.panelId;
            r["Name"] = panel.panelName;
            panelsTable.Rows.Add(r);
            foreach(MPanel chp in panel.children){
                AddPanelToList(chp, level+1);
            }
        }

        protected override void CreateChildControls()
        {

            menuTree = new TreeView();
            menuTree.SelectedNodeStyle.Font.Bold = true;
            foreach (HierarchyRow r in (hierarchyDataset.Tables[0]).Rows)
            {
                if (r.ParentId == null)
                {
                    TreeNode item = new TreeNode(r.Caption, r.Id.ToString());
                    menuTree.Nodes.Add(item);
                    AddSubtreeForItem(r, item);
                }
            }

            
            //inList.EnableViewState = true;
            //outList.EnableViewState = true;
            panelList.DataSource = panelsTable;
            panelList.DataValueField = "Id";
            panelList.DataTextField = "Name";
            panelList.DataBind();
            panelList.ID = "panelList";
            removeButton.Text = "Remove selected";
            renameButton.Text = "Rename selected";
            addButton.Text = "AddNode";
            bindButton.Text = "Bind To Panel";
            unbindButton.Text = "Unbind Panel";
            
            menuTree.SelectedNodeChanged += OnSelectedNodeChanged;
            bindButton.Click += OnBindButtonClicked;
            unbindButton.Click += OnUnbindButtonClicked;
            addButton.Click += OnAddButtonClicked;
            removeButton.Click += OnRemoveButtonClicked;
            renameButton.Click += OnRenameButtonClicked;
            
            panelList.Height = 500;
            menuTree.ShowLines = true;
            menuTree.CollapseAll();
            this.Controls.Clear();
            this.Controls.Add(menuTree);
            this.Controls.Add(panelList);
            this.Controls.Add(bindButton);
            this.Controls.Add(unbindButton);
            this.Controls.Add(newLabelTB);
            this.Controls.Add(addButton);
            this.Controls.Add(removeButton);
            this.Controls.Add(renameButton);
        }


        /// <summary>
        /// loads the current menu hierarchy from the ViewState
        /// </summary>
        /// <param name="savedState"></param>
        protected override void LoadViewState(object savedState)
        {
            base.LoadViewState(savedState);
            hierarchyDataset = (DataSet)ViewState["hierarchyDS"];
            hierarchyDataset.Relations.Clear();
            menuHierarchy = new HierarchyNavTable();
            menuHierarchy.Merge(hierarchyDataset.Tables["menuHierarchy"]);
            menuHierarchy.TableName = "menuHierarchy";
            panelsTable = hierarchyDataset.Tables["panelsTable"];
            
            hierarchyDataset.Tables.Clear();
            hierarchyDataset.Tables.Add(menuHierarchy);
            hierarchyDataset.Relations.Add(new DataRelation("Hierarchy", menuHierarchy.Columns["Id"], menuHierarchy.Columns["ParentId"], true));
            hierarchyDataset.Tables.Add(panelsTable); 
        }

        /// <summary>
        /// selects the associated panel in the select box (if any)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnSelectedNodeChanged(object sender, EventArgs e)
        {
            int? navId = ((HierarchyNavTable)hierarchyDataset.Tables[0]).Find(Int32.Parse(menuTree.SelectedValue)).NavId;
            if (navId != 0)
            {
                ListItem item = panelList.Items.FindByValue(navId.ToString());
                panelList.SelectedIndex = panelList.Items.IndexOf(item);
            }
            else panelList.SelectedIndex = -1;
        }

        /// <summary>
        /// updates the menu hierarchy - the NavId of the currently selected row (TreeView) will point to the selected panel`s ID from the list box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnBindButtonClicked(object sender, EventArgs e) {
            if (panelList.SelectedIndex != -1 && menuTree.SelectedNode != null) {
                menuHierarchy.Find(Int32.Parse(menuTree.SelectedValue)).NavId = Int32.Parse(panelList.SelectedValue);
            }
        }

        /// <summary>
        /// unbounds the selected treenode from its panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnUnbindButtonClicked(object sender, EventArgs e)
        {
            if (panelList.SelectedIndex != -1 && menuTree.SelectedNode != null)
            {
                menuHierarchy.Find(Int32.Parse(menuTree.SelectedValue)).NavId = null;
                panelList.SelectedIndex = -1;
            }
        }

        /// <summary>
        /// adds a new tree node as a next parent of the currently selected one
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnAddButtonClicked(object sender, EventArgs e) {
            if(newLabelTB.Text == "") return;
            HierarchyRow newRow = (HierarchyRow)(menuHierarchy.NewRow());
            newRow.Caption = newLabelTB.Text;
            newRow.NavId = null;
            newRow.ParentId = null;
            if (menuTree.SelectedNode != null)
                newRow.ParentId = Int32.Parse(menuTree.SelectedValue);
            menuHierarchy.Rows.Add(newRow);
            
            RecreateChildControls();

        }

        /// <summary>
        /// removes the selected TreeNode (if any); if it has any children, they leave too!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnRemoveButtonClicked(object sender, EventArgs e) {
            if (menuTree.SelectedNode == null) return;
            HierarchyRow toRemove = menuHierarchy.Find(Int32.Parse(menuTree.SelectedValue));
            hierarchyDataset.Relations["Hierarchy"].ChildKeyConstraint.DeleteRule = Rule.Cascade;
            menuHierarchy.Rows.Remove(toRemove);

            // the cascading constraint deletes all the child rows of the deleted row, but that only sets them to "Deleted",
            // they remain in the table (http://msmvps.com/blogs/williamryan/archive/2005/05/10/46445.aspx).
            List<HierarchyRow> deletedRows = (from HierarchyRow r in menuHierarchy.Rows where r.RowState == DataRowState.Deleted select r)
                .ToList<HierarchyRow>();
            foreach (HierarchyRow r in deletedRows)
                menuHierarchy.Rows.Remove(r);

            RecreateChildControls();
        }


        /// <summary>
        /// renames the selected TreeNode (if any)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnRenameButtonClicked(object sender, EventArgs e)
        {
            if (newLabelTB.Text == "" || menuTree.SelectedNode == null) return;
            menuTree.SelectedNode.Text = newLabelTB.Text;
            HierarchyRow toRename = menuHierarchy.Find(Int32.Parse(menuTree.SelectedValue));
            toRename.Caption = newLabelTB.Text;
            
            RecreateChildControls();
        }

        protected override void Render(HtmlTextWriter writer)
        {

            AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "10", false);
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            menuTree.Attributes.Add("runat", "server");
            menuTree.SelectedNodeStyle.Font.Bold = true;
            menuTree.RenderControl(writer);
            writer.RenderEndTag();
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            panelList.Attributes.Add("runat", "server");
            panelList.RenderControl(writer);
            writer.RenderEndTag();
            writer.RenderEndTag();
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            bindButton.Attributes.Add("runat", "server");
            unbindButton.Attributes.Add("runat", "server");
            bindButton.RenderControl(writer);
            unbindButton.RenderControl(writer);
            writer.RenderEndTag();
            writer.RenderEndTag();
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            newLabelTB.Attributes.Add("runat", "server");
            newLabelTB.RenderControl(writer);
            writer.RenderEndTag();
            writer.RenderEndTag();
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            addButton.Attributes.Add("runat", "server");
            removeButton.Attributes.Add("runat", "server");
            renameButton.Attributes.Add("runat", "server");
            addButton.RenderControl(writer);
            removeButton.RenderControl(writer);
            renameButton.RenderControl(writer);
            writer.RenderEndTag();
            writer.RenderEndTag();

            writer.RenderEndTag();
        }
    }
}