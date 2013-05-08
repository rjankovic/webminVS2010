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

namespace _min.Architect
{
    public partial class InitProposal : System.Web.UI.Page
    {
        DataTable dbLog;
        MinMaster mm;
        List<M2NMapping> mappings;

        protected void Page_Load(object sender, EventArgs e)
        {
            mm = (MinMaster)Master;
            
            mm.SysDriver.ClearProposal();

            if (!Page.IsPostBack)
            {
                InitProposalWizard.ActiveStepIndex = 0;
            }


            if (InitProposalWizard.ActiveStepIndex == 0)
            {
                FirstProblemList.Items.Clear();
                List<string> PKless = mm.Stats.TablesMissingPK();
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
            //sysDriver.ProcessLogTable(stats.logTable);
        }

        protected void InitProposalWizard_NextButtonClick(object sender, WizardNavigationEventArgs e)
        {

            switch (InitProposalWizard.ActiveStepIndex)
            {
                case 0: // set the gridview
                    mappings = FlattenMappings(mm.Stats.Mappings);
                    Session["mappings"] = mappings;

                    List<string> tables = mm.Stats.Tables;
                    List<string> PKless = (List<string>)Session["PKless"];
                    DataTable tablesUsageSource = new DataTable();
                    tablesUsageSource.Columns.Add("TableName", typeof(string));
                    tablesUsageSource.Columns.Add("DirectEdit", typeof(bool));
                    foreach (string tblName in tables)
                    {
                        if (PKless.Contains(tblName)) continue;
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
                    List<string> allTables = mm.Stats.Tables;
                    PKless = (List<string>)Session["PKless"];
                    foreach (GridViewRow gr in TablesUsageGridView.Rows)
                    {
                        string tableName = allTables[i++];
                        while(PKless.Contains(tableName)) tableName = allTables[i++];

                        // we shall keep preferences only for the included tables 
                        // (and that is a subset of tables with a PK)
                        if (!((CheckBox)(gr.FindControl("DirectEditCheck"))).Checked)
                            excludedTables.Add(tableName);
                        else
                            displayColumnPreferences[tableName] = ((DropDownList)(gr.FindControl("DisplayColumnDrop"))).SelectedValue;
                    }
                    PKless = (List<string>)Session["PKless"];
                    foreach (string Pkl in PKless) {
                        excludedTables.Add(Pkl);
                    }
                    Session["displayColumnPreferences"] = displayColumnPreferences;
                    Session["excludedTables"] = excludedTables;

                    // show hierarchies info
                    Dictionary<string, KeyValuePair<bool, string>> status = mm.Architect.CheckHierarchies();
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
                    if (mappings == null) mappings = null; //!
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

            mm.Architect.mappings = finMappingsList;
            mm.Architect.excludedTables = excludedTables;
            mm.Stats.SetDisplayPreferences(displayColumnPreferences);
            mm.Architect.hierarchies = (List<string>)Session["goodHierarchies"];
            _min.Models.Panel proposal = mm.Architect.propose();       // saved within proposing
            //sysDriver.AddPanel(proposal);
            string projectName = Page.RouteData.Values["projectName"] as string;
            Response.RedirectToRoute("ArchitectShowRoute", new { projectName = CE.project.Name });
        }

        protected void TablesUsageGridView_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            CheckBox cb = (CheckBox)(e.Row.Cells[1].FindControl("DirectEditCheck"));
            cb.Checked = (bool)(((DataRowView)(e.Row.DataItem))[1]);
            DropDownList ddl = (DropDownList)(e.Row.Cells[2].FindControl("DisplayColumnDrop"));
            ddl.DataSource = mm.Stats.ColumnsToDisplay[((DataRowView)(e.Row.DataItem))[0] as string];
            ddl.DataBind();
        }

        List<M2NMapping> FlattenMappings(Dictionary<string, List<M2NMapping>> dict) {
            List<M2NMapping> res = new List<M2NMapping>();
            foreach (List<M2NMapping> l in dict.Values) {
                res.AddRange(l);
            }
            return res;
        }

    }
}