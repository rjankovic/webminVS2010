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

namespace _min.Architect
{
    /// <summary>
    /// edit main menu structure - just load the original menu and save it when requested, all the UI work is done by TreeBuilder
    /// </summary>
    public partial class EditMenu : System.Web.UI.Page
    {

        MinMaster mm;

        protected void Page_Init(object sender, EventArgs e)
        {

            _min.Common.Environment.GlobalState = GlobalState.Architect;

            mm = (MinMaster)Master;

            if (!Page.IsPostBack)
            {
                tbc.SetInitialState(((TreeControl)mm.SysDriver.MainPanel.controls[0]).storedHierarchyData, mm.SysDriver.MainPanel);
            }

            

        }

        protected void OnSaveButtonClicked(object sender, EventArgs e) {
            TreeControl tc = ((TreeControl)(mm.SysDriver.MainPanel.controls[0]));
            tc.storedHierarchyDataSet.Relations.Clear();
            tc.storedHierarchyDataSet.Tables.Clear();
            // so that they dont`t remain constrained by their original dataset and can be saved to the db and eliminated arbitrarily
            tbc.FreeTables();
            tc.storedHierarchyDataSet.Tables.Add(tbc.Hierarchy);
            tc.storedHierarchyData = (HierarchyNavTable)tc.storedHierarchyDataSet.Tables[0];
            mm.SysDriver.BeginTransaction();
            mm.SysDriver.UpdatePanel(mm.SysDriver.MainPanel, false);
            mm.SysDriver.CommitTransaction();
            tc.storedHierarchyData.DataSet.Relations.Add("Hierarchy",
                tc.storedHierarchyData.Columns["Id"], tc.storedHierarchyData.Columns["ParentId"], false);
            Response.RedirectToRoute("ArchitectShowRoute", new { projectName = Page.RouteData.Values["projectName"] });
        }

    }

}