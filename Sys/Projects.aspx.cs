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
        ISystemDriver sysDriver = null;



        protected void Page_Load(object sender, EventArgs e)
        {
            string connString = WebConfigurationManager.ConnectionStrings["LocalMySqlServer"].ConnectionString;
            sysDriver = new SystemDriverMySql(connString);
        }

        protected void Page_LoadComplete(object sender, EventArgs e) {
            projectsTable = sysDriver.GetProjects();
            ProjectsGrid.DataSource = projectsTable;
            ProjectsGrid.DataBind();
        }

        protected void ProjectsGrid_RowEditing(object sender, GridViewEditEventArgs e)
        {
            ProjectsGrid.EditIndex = e.NewEditIndex;
            ProjectsGrid.DataBind();
        }


        protected void ProjectsGrid_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict["name"] = ((TextBox)(ProjectsGrid.Rows[e.RowIndex].Cells[0].Controls[0])).Text;
            dict["connstring_web"] = ((TextBox)(ProjectsGrid.Rows[e.RowIndex].Cells[1].Controls[0])).Text;
            dict["connstring_information_schema"] = ((TextBox)(ProjectsGrid.Rows[e.RowIndex].Cells[2].Controls[0])).Text;
            dict["server_type"] = ((TextBox)(ProjectsGrid.Rows[e.RowIndex].Cells[3].Controls[0])).Text;

            sysDriver.UpdateProject((int)(e.Keys[0]), dict);
            ProjectsGrid.EditIndex = -1;

        }

        protected void ProjectsGrid_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            ProjectsGrid.EditIndex = -1;
        }

        protected void InsertProjectDetailsView_ItemInserting(object sender, DetailsViewInsertEventArgs e)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (object o in e.Values.Keys) {
                dict[o.ToString()] = e.Values[o];
            }

            int proejctId = sysDriver.InsertProject(dict);
            int userid = (int)(Membership.GetUser().ProviderUserKey);
            sysDriver.SetUserRights(userid, proejctId, 1100);
            projectsTable = sysDriver.GetProjects();
            ProjectsGrid.DataSource = projectsTable;
            ProjectsGrid.DataBind();
            InsertProjectDetailsView.DataSource = projectsTable;
            InsertProjectDetailsView.DataBind();
        }

    }
}