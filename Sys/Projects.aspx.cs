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

namespace _min.Sys
{
    public partial class Projects : System.Web.UI.Page
    {
        DataTable projectsTable = new DataTable();
        MinMaster mm;


        protected void Page_Load(object sender, EventArgs e)
        {
            mm = (MinMaster)Master;
            projectsTable = mm.SysDriver.GetProjects();
            ProjectsGrid.DataSource = projectsTable;
            ProjectsGrid.DataBind();
        }

        protected void InserButton_Click(object sender, EventArgs e)
        {
            Response.RedirectToRoute("ProjectDetailRoute", new { projectId = 0 });
        }

        protected void ProjectsGrid_RowEditing(object sender, GridViewEditEventArgs e)
        {
            Response.RedirectToRoute("ProjectDetailRoute", new { projectId = ProjectsGrid.DataKeys[e.NewEditIndex].Value });
        }


    }
}