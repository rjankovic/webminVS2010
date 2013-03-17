using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using _min.Common;
using _min.Interfaces;
using _min.Models;
using System.Configuration;
using System.Text.RegularExpressions;

using _min.Navigation;
using CC = _min.Common.Constants;
using CE = _min.Common.Environment;
using MPanel = _min.Models.Panel;
using _min.Controls;

namespace _min_t7.Architect
{
    public partial class EditPanels : System.Web.UI.Page
    {
        ISystemDriver sysDriver;
        Dictionary<string, List<M2NMapping>> mappings;
        DataTable summary;
        CE.Project project;
        string webDbName;

        protected void Page_Init(object sender, EventArgs e)
        {

            _min.Common.Environment.GlobalState = GlobalState.Architect;

            //if (!Page.IsPostBack && !Page.RouteData.Values.ContainsKey("panelId"))
            //    Session.Clear();
            sysDriver = new SystemDriverMySql(ConfigurationManager.ConnectionStrings["LocalMySqlServer"].ConnectionString);
                
            if (!(Session["Summary"] is DataTable))
            {
                string projectName = Page.RouteData.Values["projectName"] as string;
                project = sysDriver.getProject(projectName);
                Session["Project"] = _min.Common.Environment.project;

                webDbName = Regex.Match(CE.project.connstringWeb, ".*Database=\"?([^\";]+)\"?.*").Groups[1].Value;
                Session["WebDbName"] = webDbName;
                IStats stats = new StatsMySql(CE.project.connstringIS, webDbName);
                Session["Mappings"] = mappings = stats.Mappings;


                sysDriver.InitArchitecture();
                HierarchyNavTable baseNavTable = ((TreeControl)(sysDriver.MainPanel.controls[0])).storedHierarchyData;

                List<string> tables = stats.Tables;

                summary = new DataTable();
                summary.Columns.Add("TableName", typeof(string));
                summary.Columns.Add("Independent", typeof(bool));
                summary.Columns.Add("HasPanels", typeof(bool));
                summary.Columns.Add("Reachable", typeof(bool));
                foreach (string tableName in stats.Tables)
                {
                    DataRow r = summary.NewRow();
                    r["TableName"] = tableName;
                    r["Independent"] = !(stats.PKs[tableName].Any(pkCol => stats.FKs[tableName].Any(fk => fk.myColumn == pkCol)));

                    List<MPanel> tablePanels = (from MPanel p in sysDriver.Panels.Values where p.tableName == tableName select p).ToList<MPanel>();
                    r["HasPanels"] = tablePanels.Count > 0;     // now surely equal to 2
                    r["Reachable"] = false;
                    if ((bool)(r["HasPanels"]))
                    {
                        r["Reachable"] = baseNavTable.Select("NavId IN (" + tablePanels[0].panelId + ", " + tablePanels[1].panelId + ")").Length > 0;
                    }
                    summary.Rows.Add(r);
                }

                Session["Summary"] = summary;
            }
            else {
                mappings = (Dictionary<string, List<M2NMapping>>)Session["Mappings"];
                summary = (DataTable)Session["Summary"];
                project = (CE.Project)Session["Project"];
                webDbName = (string)Session["WebDbName"];
            }



            if (!Page.IsPostBack)
            {
                TablesGrid.DataSource = summary;
                TablesGrid.DataBind();
                ResetActionClickablility();
            }

            BackButton.PostBackUrl = BackButton.GetRouteUrl("ArchitectShowRoute", new { projectName = project.name });

        }

        private void ResetActionClickablility() {
            for (int i = 0; i < summary.Rows.Count; i++)
            {        // disable add & remove where impossible
                if ((bool)summary.Rows[i]["Reachable"] || !(bool)summary.Rows[i]["HasPanels"])
                    ((LinkButton)(TablesGrid.Rows[i].Cells[1].Controls[0])).Enabled = false;
                if ((bool)summary.Rows[i]["HasPanels"] || !(bool)summary.Rows[i]["Independent"])
                    ((LinkButton)(TablesGrid.Rows[i].Cells[0].Controls[0])).Enabled = false;
            }
        }

        
        protected void Page_Load(object sender, EventArgs e)
        {

            
        }
        // after events
        protected void Page_LoadComplete(object sender, EventArgs e) {

            //Session["Architecture"] = sysDriver.MainPanel;
        }

        protected void TablesGrid_SelectedIndexChanged(object sender, EventArgs e)
        {
            //TablesGrid.SelectedRowStyle.BackColor = System.Drawing.Color.White;
            foreach (GridViewRow row in TablesGrid.Rows)
                row.BackColor = System.Drawing.Color.White;
            SaveButton.Enabled = true;
            int index = TablesGrid.SelectedIndex;
            if ((bool)summary.Rows[index]["HasPanels"])
            {   // remove
                TablesGrid.SelectedRow.BackColor = System.Drawing.Color.OrangeRed;
                MappingsLabel.Visible = false;
                MappingsCheck.Visible = false;
            }
            else {      // optiona for panel addition
                TablesGrid.SelectedRow.BackColor = System.Drawing.Color.LightGreen;
                MappingsLabel.Visible = true;
                MappingsCheck.DataSource = from M2NMapping mp in mappings[summary.Rows[index]["TableName"] as string] select 
                                               new { text = "Mapping to " + mp.refTable + " via " + mp.mapTable, mapTable = mp.mapTable };
                MappingsCheck.DataTextField = "text";
                MappingsCheck.DataValueField = "mapTable";
                MappingsCheck.DataBind();
            }
        }

        protected void SaveButton_Click(object sender, EventArgs e)
        {
            sysDriver.InitArchitecture();
            int index = TablesGrid.SelectedIndex;
            string tableName = summary.Rows[index]["TableName"] as string;
                
            if ((bool)summary.Rows[index]["HasPanels"])
            {   // remove
                IEnumerable<MPanel> toRemove = from MPanel p in sysDriver.Panels.Values where p.tableName == tableName select p;
                foreach (MPanel p in toRemove)
                    sysDriver.removePanel(p);
                summary.Rows[index]["HasPanels"] = false;
            }
            else
            {
                IStats stats = new StatsMySql(CE.project.connstringIS, webDbName);
                _min.Models.Architect arch = new _min.Models.Architect(sysDriver, stats);
                arch.mappings = mappings[tableName];
                arch.hierarchies = new List<string>();  // to speed it up, hierarchy nvigation can be set in panel customization
                MPanel editPanel = arch.proposeForTable(tableName);
                MPanel summaryPanel = arch.proposeSummaryPanel(tableName);


                summaryPanel.SetParentPanel(sysDriver.MainPanel);       // add to db
                editPanel.SetParentPanel(sysDriver.MainPanel);
                summaryPanel.panelName = "Summary of " + tableName;
                editPanel.panelName = "Editation of " + tableName;
                sysDriver.StartTransaction();
                sysDriver.AddPanel(summaryPanel, false);
                sysDriver.AddPanel(editPanel, false);
                foreach (_min.Models.Control c in summaryPanel.controls)    // simlified for now
                {
                    c.targetPanelId = editPanel.panelId;
                    c.targetPanel = editPanel;
                }
                foreach (_min.Models.Control c in editPanel.controls)
                {
                    c.targetPanelId = summaryPanel.panelId;
                    c.targetPanel = summaryPanel;
                }
                sysDriver.RewriteControlDefinitions(summaryPanel, false);
                sysDriver.RewriteControlDefinitions(editPanel, false);
                sysDriver.CommitTransaction();
                
                summary.Rows[index]["HasPanels"] = true;
            }
            TablesGrid.DataSource = summary;
            TablesGrid.DataBind();
            ResetActionClickablility();
            TablesGrid.SelectedRow.BackColor = System.Drawing.Color.White;
            TablesGrid.SelectedRowStyle.BackColor = System.Drawing.Color.White;
            TablesGrid.SelectedIndex = -1;
            SaveButton.Enabled = false;
        }



    }

}