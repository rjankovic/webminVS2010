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

using CC = _min.Common.Constants;
using CE = _min.Common.Environment;

namespace _min_t7.Architect
{
    public partial class InitProposal : System.Web.UI.Page
    {
        ISystemDriver sysDriver;
        IStats stats;
        _min.Models.Architect architect;
        DataTable dbLog;

        protected void Page_Load(object sender, EventArgs e)
        {
            _min.Common.Environment.GlobalState = GlobalState.Architect;
            if (!Page.IsPostBack)
            {


                InitProposalWizard.ActiveStepIndex = 0;
                string projectName = Page.RouteData.Values["projectName"] as string;
                sysDriver = new SystemDriverMySql(ConfigurationManager.ConnectionStrings["LocalMySqlServer"].ConnectionString);
                _min.Common.Environment.project = sysDriver.getProject(projectName);
                string WebDbName = Regex.Match(CE.project.connstringWeb, ".*Database=\"?([^\";]+)\"?.*").Groups[1].Value;
                dbLog = new DataTable();
                stats = new StatsMySql(CE.project.connstringIS, WebDbName, dbLog, true);
                architect = new _min.Models.Architect(sysDriver, stats);
                Session["sysDriver"] = sysDriver;
                Session["stats"] = stats;
                Session["architect"] = architect;

            }
            else
            {
                stats = (IStats)Session["stats"];
                sysDriver = (ISystemDriver)Session["sysDriver"];
                architect = (_min.Models.Architect)Session["architect"];
            }



            if (InitProposalWizard.ActiveStepIndex == 0)
            {
                FirstProblemList.Items.Clear();
                List<string> PKless = stats.TablesMissingPK();
                if (PKless.Count > 0)
                {
                    foreach (string table in PKless)
                    {
                        FirstProblemList.Items.Add("Table " + table + " has no primary key defined." +
                            " If you continue, this table will be ommitted from the proposal. You may as well add the PK and check again");
                    }
                }
                else
                {
                    FirstProblemList.Items.Add("Seems OK...");
                }
                Session["PKless"] = PKless;
            }

        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            sysDriver.ProcessLogTable(stats.logTable);
        }

        protected void InitProposalWizard_NextButtonClick(object sender, WizardNavigationEventArgs e)
        {

            switch (InitProposalWizard.ActiveStepIndex)
            {
                case 0: // set the gridview
                    List<M2NMapping> mappings = stats.findMappings();
                    Session["mappings"] = mappings;

                    List<string> tables = stats.Tables;
                    DataTable tablesUsageSource = new DataTable();
                    tablesUsageSource.Columns.Add("TableName", typeof(string));
                    tablesUsageSource.Columns.Add("DirectEdit", typeof(bool));
                    foreach (string tblName in tables)
                    {
                        DataRow r = tablesUsageSource.NewRow();
                        r[0] = tblName;
                        r[1] = !mappings.Any(m => m.mapTable == tblName);
                        tablesUsageSource.Rows.Add(r);
                    }
                    TablesUsageGridView.DataSource = tablesUsageSource;
                    TablesUsageGridView.DataBind();
                    break;

                case 1:
                    // save data about TableUsage
                    Dictionary<string, string> displayColumnPreferences = new Dictionary<string, string>();
                    List<string> excludedTables = new List<string>();
                    int i = 0;
                    List<string> allTables = stats.Tables;
                    foreach (GridViewRow gr in TablesUsageGridView.Rows)
                    {
                        string tableName = allTables[i++];
                        if (!((CheckBox)(gr.FindControl("DirectEditCheck"))).Checked)
                            excludedTables.Add(tableName);
                        displayColumnPreferences[tableName] = ((DropDownList)(gr.FindControl("DisplayColumnDrop"))).SelectedValue;
                    }
                    Session["displayColumnPreferences"] = displayColumnPreferences;
                    Session["excludedTables"] = excludedTables;

                    // show hierarchies info
                    Dictionary<string, KeyValuePair<bool, string>> status = architect.CheckHierarchies();
                    ProblemListHierarchies.Items.Clear();
                    List<string> goodHierarchies = new List<string>();
                    foreach (string table in status.Keys)
                    {
                        ProblemListHierarchies.Items.Add(table + " : " + status[table].Value);
                        if (status[table].Key) goodHierarchies.Add(table);
                    }
                    Session["goodHierarchies"] = goodHierarchies;
                    break;
                case 2: // get ready for mappings editation

                    mappings = (List<M2NMapping>)Session["mappings"];      // cache on stats side
                    if (mappings == null) mappings = stats.findMappings();
                    MappingsChoiceRepeater.DataSource = mappings;
                    MappingsChoiceRepeater.DataBind();
                    break;
                case 3:
                    CheckBox ItemCheckBox;
                    List<M2NMapping> mappingList = (List<M2NMapping>)Session["mappings"];
                    List<M2NMapping> finMappingList = new List<M2NMapping>();
                    i = 0;
                    foreach (RepeaterItem item in MappingsChoiceRepeater.Items)
                    {
                        ItemCheckBox = (CheckBox)item.FindControl("ItemCheckBox");
                        if (ItemCheckBox.Checked)
                            finMappingList.Add(mappingList[i]);
                        i++;
                    }
                    Session["finMappingList"] = finMappingList;
                    break;
            }

        }

        protected void InitProposalWizard_FinishButtonClick(object sender, WizardNavigationEventArgs e)
        {
            List<string> excludedTables = (List<string>)Session["excludedTables"];
            Dictionary<string, string> displayColumnPreferences = (Dictionary<string, string>)Session["displayColumnPreferences"];
            List<M2NMapping> finMappingsList = (List<M2NMapping>)Session["finMappingList"];

            architect.mappings = finMappingsList;
            architect.excludedTables = excludedTables;
            stats.SetDisplayPreferences(displayColumnPreferences);
            architect.hierarchies = (List<string>)Session["goodHierarchies"];
            _min.Models.Panel proposal = architect.propose();       // saved within proposing
            //sysDriver.AddPanel(proposal);
            string projectName = Page.RouteData.Values["projectName"] as string;
            Response.RedirectToRoute("ArchitectShowRoute", new { projectName = projectName });
        }

        protected void TablesUsageGridView_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            CheckBox cb = (CheckBox)(e.Row.Cells[1].FindControl("DirectEditCheck"));
            cb.Checked = (bool)(((DataRowView)(e.Row.DataItem))[1]);
            DropDownList ddl = (DropDownList)(e.Row.Cells[2].FindControl("DisplayColumnDrop"));
            ddl.DataSource = stats.ColumnsToDisplay[((DataRowView)(e.Row.DataItem))[0] as string];
            ddl.DataBind();
        }

    }
}