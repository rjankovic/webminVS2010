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
            hierarchyDataset.Relations.Add(new DataRelation("Hierarchy", menuHierarchy.Columns["Id"], menuHierarchy.Columns["ParentId"], false));
            /*
            hierarchy.DataSet.Relations.Clear();
            hierarchy.DataSet.Tables.Remove(hierarchy);
            hierarchyDataset.Tables.Add(hierarchy);
            */ 
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

        public HierarchyNavTable Hierarchy {
            get {
                return menuHierarchy;
            }
        }


        private void AddSubtreeForItem(DataRow row, WC.TreeNode item)
        {
            DataRow[] children = row.GetChildRows("Hierarchy");
            WC.TreeNode childItem;
            foreach (DataRow child in children)
            {
                childItem = new WC.TreeNode(child["Caption"].ToString(), child["Id"].ToString());
                item.ChildNodes.Add(childItem);
                AddSubtreeForItem(child, childItem);
            }
        }

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
            foreach (HierarchyRow r in ((HierarchyNavTable)hierarchyDataset.Tables[0]).Rows)
            {
                if (r.ParentId == 0)
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
            hierarchyDataset.Relations.Add(new DataRelation("Hierarchy", menuHierarchy.Columns["Id"], menuHierarchy.Columns["ParentId"], false));
            hierarchyDataset.Tables.Add(panelsTable); 
        }

        protected void OnSelectedNodeChanged(object sender, EventArgs e)
        {
            int navId = ((HierarchyNavTable)hierarchyDataset.Tables[0]).Find(Int32.Parse(menuTree.SelectedValue)).NavId;
            if (navId != 0)
            {
                ListItem item = panelList.Items.FindByValue(navId.ToString());
                panelList.SelectedIndex = panelList.Items.IndexOf(item);
            }
            else panelList.SelectedIndex = -1;
        }

        protected void OnBindButtonClicked(object sender, EventArgs e) {
            if (panelList.SelectedIndex != -1 && menuTree.SelectedNode != null) {
                menuHierarchy.Find(Int32.Parse(menuTree.SelectedValue)).NavId = Int32.Parse(panelList.SelectedValue);
            }
        }

        protected void OnUnbindButtonClicked(object sender, EventArgs e)
        {
            if (panelList.SelectedIndex != -1 && menuTree.SelectedNode != null)
            {
                menuHierarchy.Find(Int32.Parse(menuTree.SelectedValue)).NavId = 0;
                panelList.SelectedIndex = -1;
            }
        }


        protected void OnAddButtonClicked(object sender, EventArgs e) {
            if(newLabelTB.Text == "") return;
            HierarchyRow newRow = (HierarchyRow)(menuHierarchy.NewRow());
            newRow.Caption = newLabelTB.Text;
            newRow.NavId = 0;
            newRow.ParentId = 0;
            if (menuTree.SelectedNode != null)
                newRow.ParentId = Int32.Parse(menuTree.SelectedValue);
            menuHierarchy.Rows.Add(newRow);
            
            //EnsureChildControls();
            RecreateChildControls();

            /*
            TreeNode newNode = new TreeNode(newRow.Caption, newRow.Id.ToString());
            if (menuTree.SelectedNode != null)
                menuTree.SelectedNode.ChildNodes.Add(newNode);
            else
                menuTree.Nodes.Add(newNode);
             */ 
        }

        protected void OnRemoveButtonClicked(object sender, EventArgs e) {
            if (menuTree.SelectedNode == null) return;
            HierarchyRow toRemove = menuHierarchy.Find(Int32.Parse(menuTree.SelectedValue));
            CascadeHierarchy(toRemove);
            menuHierarchy.Rows.Remove(toRemove);
            
            //menuTree.Nodes.Remove(menuTree.SelectedNode);
            RecreateChildControls();
        }

        private void CascadeHierarchy(HierarchyRow toRemove) {
            DataRow[] children = toRemove.GetChildRows("Hierarchy");
            foreach (DataRow r in children) {
                CascadeHierarchy((HierarchyRow)r);
                menuHierarchy.Remove((HierarchyRow)r);
            }
        }

        protected void OnRenameButtonClicked(object sender, EventArgs e)
        {
            if (newLabelTB.Text == "" || menuTree.SelectedNode == null) return;
            menuTree.SelectedNode.Text = newLabelTB.Text;
            HierarchyRow toRename = menuHierarchy.Find(Int32.Parse(menuTree.SelectedValue));
            toRename.Caption = newLabelTB.Text;
            
            RecreateChildControls();
        }

        public void FreeTables() {
            hierarchyDataset.Relations.Clear();
            hierarchyDataset.Tables.Clear();
        }
       

        protected override void Render(HtmlTextWriter writer)
        {

            AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "10", false);
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            menuTree.Attributes.Add("runat", "server");
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