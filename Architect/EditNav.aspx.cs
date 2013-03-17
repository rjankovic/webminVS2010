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

namespace _min_t7.Architect
{
    public partial class EditNav : System.Web.UI.Page
    {
        ISystemDriver sysDriver;
        IWebDriver webDriver;
        IStats stats;
        _min.Models.Architect architect;
        MPanel actPanel;
        FK hierarchy;
        List<FK> FKs = new List<FK>();

        protected void Page_Init(object sender, EventArgs e)
        {

            ValidationResult.Items.Clear();

            _min.Common.Environment.GlobalState = GlobalState.Architect;

            //if (!Page.IsPostBack && !Page.RouteData.Values.ContainsKey("panelId"))
            //    Session.Clear();
            _min.Models.Panel architecture = null;
            if (Session["Architecture"] is _min.Models.Panel)
            {
                architecture = (MPanel)Session["Architecture"];
            }

            string projectName = Page.RouteData.Values["projectName"] as string;
            int panelId = Int32.Parse(Page.RouteData.Values["panelId"] as string);

            sysDriver = new SystemDriverMySql(ConfigurationManager.ConnectionStrings["LocalMySqlServer"].ConnectionString);
            _min.Common.Environment.project = sysDriver.getProject(projectName);

            string WebDbName = Regex.Match(CE.project.connstringWeb, ".*Database=\"?([^\";]+)\"?.*").Groups[1].Value;
            webDriver = new WebDriverMySql(CE.project.connstringWeb);

            stats = new StatsMySql(CE.project.connstringIS, WebDbName);

            architect = new _min.Models.Architect(sysDriver, stats);

            actPanel = sysDriver.getPanel(panelId, false);
            
            DataColumnCollection cols = stats.ColumnTypes[actPanel.tableName];

            PanelName.Text = actPanel.panelName;
            
            _min.Models.Control control = actPanel.controls.Where(x => x is NavTableControl || x is TreeControl).First();

            FKs = stats.foreignKeys(actPanel.tableName);

            List<string> colNames = (from DataColumn col in cols select col.ColumnName).ToList<string>();
            
            DisplayCols.SetOptions(colNames);
            DisplayCols.SetIncludedOptions(control.displayColumns);
            
            string[] possibleAcitons = { UserAction.Insert.ToString(), UserAction.View.ToString(), 
                                           UserAction.Delete.ToString() };
            AllowedActions.DataSource = possibleAcitons;
            AllowedActions.DataBind();
            List<UserAction> originalActions = (control is NavTableControl) ? 
                ((NavTableControl)control).actions : ((TreeControl)control).actions;
            foreach(_min.Models.Control simpleControl in actPanel.controls){
                if(simpleControl == control) continue;
                originalActions.Add(simpleControl.action);
            }
            foreach (ListItem item in AllowedActions.Items) { 
                if(originalActions.Contains((UserAction)Enum.Parse(typeof(UserAction), item.Text))) 
                    item.Selected = true;
            }

            hierarchy = stats.SelfRefFKStrict(actPanel.tableName);
            string[] CTypeOptions = hierarchy == null ? new string[] {"Navigation Table"} : 
                new string[] {"Navigation Table", "Navigation Tree"};
            NavControlType.DataSource = CTypeOptions;
            NavControlType.DataBind();
            if (control is TreeControl) NavControlType.SelectedIndex = 1; else NavControlType.SelectedIndex = 0;

            BackButton.PostBackUrl = BackButton.GetRouteUrl("ArchitectShowRoute", new { projectName = projectName });

        }

            

        


        protected void Page_Load(object sender, EventArgs e)
        {

            
        }

        protected void Page_LoadComplete(object sender, EventArgs e) {

            //Session["Architecture"] = sysDriver.MainPanel;
        }

        protected void SaveButton_Click(object sender, EventArgs e)
        {


            string panelName = PanelName.Text;
            List<string> displayCols = (from ListItem li in DisplayCols.IncludedItems select li.Text).ToList<string>();
            List<UserAction> actions = new List<UserAction>();
            foreach(ListItem item in AllowedActions.Items){
                if(item.Selected) actions.Add((UserAction)Enum.Parse(typeof(UserAction), item.Text));
            }

            ValidationResult.Items.Clear();
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
                _min.Models.Control c;
                List<_min.Models.Control> controls = new List<_min.Models.Control>();

                if (NavControlType.SelectedValue.EndsWith("Table"))
                {
                    List<FK> neededFKs = (from FK fk in FKs where displayCols.Contains(fk.myColumn) select fk).ToList<FK>();
                    c = new NavTableControl(actPanel.panelId, new System.Data.DataTable(), stats.PKs[actPanel.tableName],
                        neededFKs, actions);
                    c.displayColumns = displayCols;
                }
                else {
                    c = new TreeControl(actPanel.panelId, new HierarchyNavTable(), stats.PKs[actPanel.tableName][0], 
                        hierarchy.refColumn, displayCols[0], actions);
                }
                controls.Add(c);
                
                if (actions.Contains(UserAction.Insert))
                {
                    controls.Add(new _min.Models.Control(actPanel.panelId, "Insert", UserAction.Insert));
                    actions.Remove(UserAction.Insert);
                }

                foreach (_min.Models.Control listedControl in controls) {
                    listedControl.targetPanelId = actPanel.controls[0].targetPanelId;
                }

                MPanel resPanel = new MPanel(actPanel.tableName, actPanel.panelId, 
                    c is TreeControl ? PanelTypes.NavTree : PanelTypes.NavTable, new List<MPanel>(), 
                    new List<Field>(), controls, actPanel.PKColNames, null, actPanel.parent);
                resPanel.panelName = panelName;

                actPanel = resPanel;

                sysDriver.StartTransaction();
                sysDriver.updatePanel(actPanel);
                Session.Clear();
                sysDriver.CommitTransaction();
                ValidationResult.Items.Add("Saved");
            }

        }

    }

}