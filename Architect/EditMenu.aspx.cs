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


        protected void Page_Init(object sender, EventArgs e)
        {

            _min.Common.Environment.GlobalState = GlobalState.Architect;

            //if (!Page.IsPostBack && !Page.RouteData.Values.ContainsKey("panelId"))
            //    Session.Clear();
            

            string projectName = Page.RouteData.Values["projectName"] as string;

            sysDriver = new SystemDriverMySql(ConfigurationManager.ConnectionStrings["LocalMySqlServer"].ConnectionString);
            _min.Common.Environment.project = sysDriver.getProject(projectName);


            string WebDbName = Regex.Match(CE.project.connstringWeb, ".*Database=\"?([^\";]+)\"?.*").Groups[1].Value;
            webDriver = new WebDriverMySql(CE.project.connstringWeb);
            
            
            if (!Page.IsPostBack)
            {
                sysDriver.InitArchitecture();
                tbc.SetInitialState(((TreeControl)sysDriver.MainPanel.controls[0]).storedHierarchyData, sysDriver.MainPanel);
                
            }


        }

        protected void OnSaveButtonClicked(object sender, EventArgs e) {
            sysDriver.InitArchitectureBasePanel();
            TreeControl tc = ((TreeControl)(sysDriver.MainPanel.controls[0]));
            tc.storedHierarchyDataSet.Relations.Clear();
            tc.storedHierarchyDataSet.Tables.Clear();
            tbc.FreeTables();
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