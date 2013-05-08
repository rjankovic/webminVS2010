using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Configuration;
using System.Text.RegularExpressions;

using _min.Common;
using _min.Interfaces;
using _min.Models;
using _min.Navigation;
using _min.Controls;
using CC = _min.Common.Constants;
using CE = _min.Common.Environment;
using MPanel = _min.Models.Panel;

namespace _min.Architect
{
    /// <summary>
    /// Edit a navigation panel structure - either a NavTable or a NavTree (a NavTree can always be converted into a NavTable)
    /// </summary>
    public partial class EditNav : System.Web.UI.Page
    {
        MPanel actPanel;
        FK hierarchy;
        List<FK> FKs = new List<FK>();
        MinMaster mm;

        protected void Page_Init(object sender, EventArgs e)
        {

            ValidationResult.Items.Clear();
            mm = (MinMaster)Master;

            
            string projectName = Page.RouteData.Values["projectName"] as string;
            int panelId = Int32.Parse(Page.RouteData.Values["panelId"] as string);

            actPanel = mm.SysDriver.Panels[panelId];
            
            DataColumnCollection cols = mm.Stats.ColumnTypes[actPanel.tableName];

            PanelName.Text = actPanel.panelName;
            
            _min.Models.Control control = actPanel.controls.Where(x => x is NavTableControl || x is TreeControl).First();

            FKs = mm.Stats.FKs[actPanel.tableName];

            List<string> colNames = (from DataColumn col in cols select col.ColumnName).ToList<string>();
            
            // a M2NControl to select the columns of the table displayed in the GridView - for a tree we take only the first item
            DisplayCols.SetOptions(colNames);
            DisplayCols.SetIncludedOptions(control.displayColumns);
            
            List<string> possibleAcitons = new List<string>(new string[] { UserAction.Insert.ToString(), UserAction.View.ToString(), 
                                           UserAction.Delete.ToString() });
            List<UserAction> originalActions = new List<UserAction>();
            if (control is NavTableControl) { 
                foreach(UserAction ua in ((NavTableControl)control).actions)
                    originalActions.Add(ua);
            }
            else{
                foreach(UserAction ua in ((TreeControl)control).actions)
                    originalActions.Add(ua);
            }
            

            foreach(_min.Models.Control simpleControl in actPanel.controls){
                if(simpleControl == control) continue;
                originalActions.Add(simpleControl.action);
            }

            List<string> originalActionStrings = new List<string>();
            foreach (UserAction a in originalActions) 
                originalActionStrings.Add(a.ToString());

            actionsControl.SetOptions(possibleAcitons);
            actionsControl.SetIncludedOptions(originalActionStrings);
            

            hierarchy = mm.Stats.SelfRefFKs().Find(x => x.myTable == actPanel.tableName);
            string[] CTypeOptions = hierarchy == null ? new string[] {"Navigation Table"} : 
                new string[] {"Navigation Table", "Navigation Tree"};
            // a radio button list - contains navtable and maybe treeview option
            NavControlType.DataSource = CTypeOptions;
            NavControlType.DataBind();
            // let the default be the current
            if (control is TreeControl) NavControlType.SelectedIndex = 1; else NavControlType.SelectedIndex = 0;

            BackButton.PostBackUrl = BackButton.GetRouteUrl("ArchitectShowRoute", new { projectName = projectName });

        }

        protected void SaveButton_Click(object sender, EventArgs e)
        {


            string panelName = PanelName.Text;
            List<string> displayCols = DisplayCols.RetrieveStringData();
            List<UserAction> actions = new List<UserAction>();
            foreach(string s in actionsControl.RetrieveStringData()){
                actions.Add((UserAction)Enum.Parse(typeof(UserAction), s));
            }

            ValidationResult.Items.Clear();
            // validate the proposal
            if (panelName == ".")
            {
                ValidationResult.Items.Add("Give the pannel a name, please.");
            }
            else if (displayCols.Count == 0) {
                ValidationResult.Items.Add("Select at leas one column to display");
            }
            else if (actions.Count == 0)
            {
                ValidationResult.Items.Add("Check at least one action users can perform in thie panel, please");
            }
            else {
                ValidationResult.Items.Add("Valid");
                // => create the panel and save it
                _min.Models.Control c;
                List<_min.Models.Control> controls = new List<_min.Models.Control>();

                _min.Models.Control insertButton = null;

                // insert is a separate button
                if (actions.Contains(UserAction.Insert))
                {
                    insertButton = new _min.Models.Control(actPanel.panelId, "Insert", UserAction.Insert);
                    actions.Remove(UserAction.Insert);
                }

                if (NavControlType.SelectedValue.EndsWith("Table"))
                {
                    List<FK> neededFKs = (from FK fk in FKs where displayCols.Contains(fk.myColumn) select fk).ToList<FK>();
                    c = new NavTableControl(actPanel.panelId, new System.Data.DataTable(), mm.Stats.PKs[actPanel.tableName],
                        neededFKs, actions);
                    c.displayColumns = displayCols;
                }
                else {  // NavTree
                    actions.Remove(UserAction.Delete);      // cannot use delete in NavTrees
                    c = new TreeControl(actPanel.panelId, new HierarchyNavTable(), mm.Stats.PKs[actPanel.tableName][0], 
                        hierarchy.myColumn, displayCols[0], actions);
                }
                controls.Add(c);
                if (insertButton != null)
                    controls.Add(insertButton);
                

                foreach (_min.Models.Control listedControl in controls) {
                    listedControl.targetPanelId = actPanel.controls[0].targetPanelId;
                }

                MPanel resPanel = new MPanel(actPanel.tableName, actPanel.panelId, 
                    c is TreeControl ? PanelTypes.NavTree : PanelTypes.NavTable, new List<MPanel>(), 
                    new List<IField>(), controls, actPanel.PKColNames, null, actPanel.parent);
                resPanel.panelName = panelName;

                actPanel = resPanel;

                mm.SysDriver.BeginTransaction();
                mm.SysDriver.UpdatePanel(actPanel);
                mm.SysDriver.CommitTransaction();
                mm.SysDriver.IncreaseVersionNumber();
                ValidationResult.Items.Add("Saved");
                Response.Redirect(Page.Request.RawUrl);
            }

        }

    }

}