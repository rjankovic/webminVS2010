using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using _min.Common;
using _min.Models;
using _min.Interfaces;
using System.Web.Configuration;
using System.Data;
using System.Web.Security;
using System.Text.RegularExpressions;
using CE = _min.Common.Environment;

namespace _min.Sys
{
    public partial class ProjectDetail : System.Web.UI.Page
    {
        DataTable projectsTable = new DataTable();
        CE.Project project = null;
        MinMaster mm;
        

        protected void Page_Load(object sender, EventArgs e)
        {

            mm = (MinMaster)Master;
            if(Page.RouteData.Values.ContainsKey("projectId") && Int32.Parse(Page.RouteData.Values["projectId"] as string) > 0){
                DeleteButton.Visible = true;
                DetailsView.DefaultMode = DetailsViewMode.Edit;
                int idProject = Int32.Parse(Page.RouteData.Values["projectId"] as string);
                project = mm.SysDriver.GetProject(idProject);
                if (!Page.IsPostBack)
                {
                    DetailsView.DataSource = new CE.Project[] { project };
                    DetailsView.DataBind();
                }
            }

        }

        protected void DetailsView_ItemCommand(object sender, DetailsViewCommandEventArgs e)
        {
            InfoList.Items.Clear();
            switch (e.CommandName) { 
                case "TestWeb":
                    try
                    {
                        IBaseDriver drv = null;
                        switch (mm.DbServer)
                        {
                            case DbServer.MySql:
                                drv = new BaseDriverMySql(((TextBox)(DetailsView.Rows[1].Cells[1].Controls[0])).Text);
                                break;
                            case DbServer.MsSql:
                                drv = new BaseDriverMsSql(((TextBox)(DetailsView.Rows[1].Cells[1].Controls[0])).Text);
                                break;
                        }
                        
                        drv.TestConnection();
                        InfoList.Items.Add("Connection successful");
                    }
                    catch (Exception ew) {
                        InfoList.Items.Add(ew.Message);
                    }
                    break;
                case "TestIS":
                    try
                    {
                        IBaseDriver drv = null;
                        switch (mm.DbServer)
                        {
                            case DbServer.MySql:
                                drv = new BaseDriverMySql(((TextBox)(DetailsView.Rows[3].Cells[1].Controls[0])).Text);
                                break;
                            case DbServer.MsSql:
                                drv = new BaseDriverMsSql(((TextBox)(DetailsView.Rows[3].Cells[1].Controls[0])).Text);
                                break;
                        }
                        drv.TestConnection();
                        InfoList.Items.Add("Connection successful");
                    }
                    catch (Exception ei)
                    {
                        InfoList.Items.Add(ei.Message);
                    }
                    break;
                case "Cancel":
                        Response.RedirectToRoute("ProjectsRoute");
                    break;
                case "Edit":
                    DetailsView.ChangeMode(DetailsViewMode.Edit);
                    break;
            }
        }


        private void BasicValidation(CE.Project newProject) { 
            if (newProject.Name == null)
                InfoList.Items.Add("Please enter the project name");
            else if(!Regex.IsMatch(newProject.Name, "^[a-zA-Z0-9]+$"))
                InfoList.Items.Add("The project name must consist exclusively of letters and digits");
            if(newProject.ConnstringIS == null)
                InfoList.Items.Add("Please enter the connection string through which the application can access the corresponding INFORMATION_SCHEMA.");
            if (newProject.ConnstringWeb == null)
                InfoList.Items.Add("Please enter the connection string through which the application can access the production database.");
        }

        protected void DetailsView_ItemInserting(object sender, DetailsViewInsertEventArgs e)
        {
            InfoList.Items.Clear();
                
            string name = e.Values["Name"] as string;
            string csis = e.Values["ConnstringIS"] as string;
            string cswb = e.Values["ConnstringWeb"] as string;

            CE.Project newProject = new CE.Project(0, name, mm.DbServer.ToString(), cswb, csis, 0);
            
            BasicValidation(newProject);
            if (InfoList.Items.Count > 0)
            {
                e.Cancel = true;
                return;
            }
            
            try
            {
                mm.SysDriver.InsertProject(newProject);
            }
            catch (ConstraintException ce){
                InfoList.Items.Add(ce.Message);
                e.Cancel = true;
                return;
            }
            Response.RedirectToRoute("ProjectsRoute");
        }

        protected void DetailsView_ItemUpdating(object sender, DetailsViewUpdateEventArgs e)
        {

            string name = e.NewValues["Name"] as string;
            string csis = e.NewValues["ConnstringIS"] as string;
            string cswb = e.NewValues["ConnstringWeb"] as string;
            CE.Project updatedProject = new CE.Project(project.Id, name, "MSSQL", cswb, csis, project.Version + 1);
            
            BasicValidation(updatedProject);
            if (InfoList.Items.Count > 0)
            {
                e.Cancel = true;
                return;
            }
            
            try
            {
                mm.SysDriver.UpdateProject(updatedProject);
            }
            catch (ConstraintException ce)
            {
                InfoList.Items.Add(ce.Message);
                e.Cancel = true;
                return;
            }
            InfoList.Items.Clear();
            InfoList.Items.Add("Project was successfully updated.");
        }

        protected void DeleteButton_Click(object sender, EventArgs e)
        {
            mm.SysDriver.DeleteProject(project.Id);
            Response.RedirectToRoute("ProjectsRoute");
        }



    }
}