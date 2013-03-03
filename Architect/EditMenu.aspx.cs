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
    public partial class EditMenu : System.Web.UI.Page
    {
        ISystemDriver sysDriver;
        IWebDriver webDriver;
        TreeBuilderControl tbc;


        protected void Page_Init(object sender, EventArgs e)
        {

            //_min.Common.Environment.GlobalState = GlobalState.Architect;

            //if (!Page.IsPostBack && !Page.RouteData.Values.ContainsKey("panelId"))
            //    Session.Clear();
            _min.Models.Panel architecture = null;
            if (Session["Architecture"] is _min.Models.Panel)
            {
                architecture = (MPanel)Session["Architecture"];
            }

            Dictionary<UserAction, int> currentPanelActionPanels = new Dictionary<UserAction, int>();
            string projectName = Page.RouteData.Values["projectName"] as string;

            sysDriver = new SystemDriverMySql(ConfigurationManager.ConnectionStrings["LocalMySqlServer"].ConnectionString);
            _min.Common.Environment.project = sysDriver.getProject(projectName);


            string WebDbName = Regex.Match(CE.project.connstringWeb, ".*Database=\"?([^\";]+)\"?.*").Groups[1].Value;
            webDriver = new WebDriverMySql(CE.project.connstringWeb);
            
            
            sysDriver.InitArchitecture(architecture);
            Session["Architecture"] = sysDriver.MainPanel;

            tbc = new _min.Controls.TreeBuilderControl();
            tbc.ID = "TBC";
            tbc.SetInitialState(((TreeControl)sysDriver.MainPanel.controls[0]).storedHierarchyData, sysDriver.MainPanel);
            MainPanel.Controls.Add(tbc);

            Button SaveButton = new Button();
            SaveButton.Text = "Save";
            SaveButton.Click += OnSaveButtonClicked;
            MainPanel.Controls.Add(SaveButton);

            LinkButton BackButton = new LinkButton();
            BackButton.Text = "Back";
            BackButton.PostBackUrl = BackButton.GetRouteUrl("ArchitectShowRoute", new { projectName = projectName });
            MainPanel.Controls.Add(BackButton);

        }

        protected void OnSaveButtonClicked(object sender, EventArgs e) {
            TreeControl tc = ((TreeControl)(sysDriver.MainPanel.controls[0]));
            tc.storedHierarchyDataSet.Relations.Clear();
            tc.storedHierarchyDataSet.Tables.Clear();
            tc.storedHierarchyDataSet.Tables.Add(tbc.Hierarchy);
            tc.storedHierarchyData = (HierarchyNavTable)tc.storedHierarchyDataSet.Tables[0];
            sysDriver.StartTransaction();
            sysDriver.RewriteControlDefinitions(sysDriver.MainPanel, false);
            sysDriver.CommitTransaction();
            tc.storedHierarchyData.DataSet.Relations.Add("Hierarchy",
                tc.storedHierarchyData.Columns["Id"], tc.storedHierarchyData.Columns["ParentId"], false);
            //Session.Clear();
            Response.RedirectToRoute("ArchitectShowRoute", new { projectName = Page.RouteData.Values["projectName"] });
        }



        protected void Page_Load(object sender, EventArgs e)
        {

            
        }

        protected void Page_LoadComplete(object sender, EventArgs e) {

            //Session["Architecture"] = sysDriver.MainPanel;
        }

    }

}