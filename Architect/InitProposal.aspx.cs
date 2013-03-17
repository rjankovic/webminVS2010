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
                case 0:
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
                case 1:
                    List<M2NMapping> mappings = stats.findMappings();       // cache on stats side
                    MappingsChoiceRepeater.DataSource = mappings;
                    MappingsChoiceRepeater.DataBind();
                    Session["mappings"] = mappings;
                    break;
            }

        }

        protected void InitProposalWizard_FinishButtonClick(object sender, WizardNavigationEventArgs e)
        {
            WaitLabel.Visible = true;

            CheckBox ItemCheckBox;
            List<M2NMapping> mappingsList = (List<M2NMapping>)Session["mappings"];
            int i = 0;
            List<M2NMapping> finMappingsList = new List<M2NMapping>();
            foreach (RepeaterItem item in MappingsChoiceRepeater.Items)
            {
                ItemCheckBox = (CheckBox)item.FindControl("ItemCheckBox");
                if (ItemCheckBox.Checked)
                    finMappingsList.Add(mappingsList[i]);
                i++;
            }
            architect.mappings = finMappingsList;
            architect.hierarchies = (List<string>)Session["goodHierarchies"];
            _min.Models.Panel proposal = architect.propose();       // saved within proposing
            //sysDriver.AddPanel(proposal);
            WaitLabel.Text = "Done...?";
        }

    }
}