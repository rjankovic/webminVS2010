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
using _min.Common;

namespace _min.Controls
{
    /// <summary>
    /// Used by NavTree panels as the update selection control. Can be also used to handle more than one user action at a time, by letting the user select the 
    /// action that executed for the selected item from a RadioButtonList, but this was disovered inconvenient and chaotic.
    /// </summary>
    [ToolboxData("<{0}:TreeNavigator runat=server></{0}:M2NMapping>")]
    public class TreeNavigatorControl : CompositeControl
    {
        private TreeView tree = new TreeView();
        private RadioButtonList radios = new RadioButtonList();
        private HierarchyNavTable hierarchy = new HierarchyNavTable();
        private List<UserAction> actions;

        public event WC.CommandEventHandler ActionChosen;

        private string _ID;
        public override string ID
        {
            get { 
                return _ID; 
            }
            set { 
                _ID = value;
                tree.ID = ID + "_TreeView";
                radios.ID = ID + "_RadioButtonList";
            }
        }

        public TreeNavigatorControl(HierarchyNavTable hierarchy, List<UserAction> actions) {
            this.hierarchy = hierarchy;
            this.actions = actions;

            
                tree = new TreeView();

                tree.ShowLines = true;
                WC.TreeNode item;

                foreach (HierarchyRow r in hierarchy.Rows)
                {
                    if (r.ParentId == null)
                    {
                        item = new WC.TreeNode(r.Caption, r.NavId.ToString());
                        AddSubtreeForItem(r, item);
                        tree.Nodes.Add(item);
                    }
                }
                tree.SelectedNodeChanged += SelectionChanged;
                tree.SelectedNodeStyle.Font.Bold = true;
                
                radios = new RadioButtonList();
                radios.DataSource = actions;
                radios.DataBind();

                // if there is only one action option, don`t show the radios at all
                if (actions.Count == 1)
                {
                    radios.SelectedIndex = 0;
                    radios.Visible = false;
                }
                radios.SelectedIndexChanged += SelectionChanged;
                radios.AutoPostBack = true;


            
            this.Controls.Add(tree);
            this.Controls.Add(radios);
        }

        private void AddSubtreeForItem(HierarchyRow row, WC.TreeNode item)
        {
            HierarchyRow[] children = row.GetHierarchyChildRows("Hierarchy");
            WC.TreeNode childItem;
            foreach (HierarchyRow child in children)
            {
                childItem = new WC.TreeNode((child.Caption ?? "NULL"), child["NavId"].ToString());
                item.ChildNodes.Add(childItem);
                AddSubtreeForItem(child, childItem);
            }
        }

        /// <summary>
        /// if an UserAction is selected within the RadioButtonList, thehandler for the item selected in the TreeView
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectionChanged(object sender, EventArgs e) {
            
            if (tree.SelectedNode == null) return;
            if (radios.SelectedIndex < 0) return;
            CommandEventArgs command = new CommandEventArgs("_" + radios.SelectedValue, Int32.Parse(tree.SelectedValue));
            
            ActionChosen(this, command);
        }
        
        protected override void Render(HtmlTextWriter writer)
        {
            AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "10", false);
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            tree.Attributes.Add("runat", "server");
            tree.RenderControl(writer);
            writer.RenderEndTag();
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            radios.Attributes.Add("runat", "server");
            radios.RenderControl(writer);
            writer.RenderEndTag();
            writer.RenderEndTag();
            
            writer.RenderEndTag();
        }
    }
}