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

namespace _min_t7.Sys
{
    public partial class Projects : System.Web.UI.Page
    {
        DataTable projectsTable = new DataTable();
        ISystemDriver sysDriver = null;



        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                string connString = WebConfigurationManager.ConnectionStrings["LocalMySqlServer"].ConnectionString;
                sysDriver = new SystemDriverMySql(connString);
                projectsTable = sysDriver.GetProjects();
                
                Session["projectsTable"] = projectsTable;
                Session["sysDriver"] = sysDriver;
                ProjectsGrid.DataSource = projectsTable;
                ProjectsGrid.DataBind();

                InsertProjectDetailsView.DataSource = projectsTable;
                InsertProjectDetailsView.DataBind();
            }
            else {
                sysDriver = Session["sysDriver"] as ISystemDriver;
                projectsTable = Session["projectsTable"] as DataTable;
                ProjectsGrid.DataSource = projectsTable;
            }
            
            
        }

        protected void ProjectsGrid_RowEditing(object sender, GridViewEditEventArgs e)
        {
            ProjectsGrid.EditIndex = e.NewEditIndex;
            ProjectsGrid.DataBind();
        }

        private void RenameRoleAndUsers(string OldRoleName, string NewRoleName)
        {
            string[] users = Roles.GetUsersInRole(OldRoleName);
            Roles.CreateRole(NewRoleName);
            Roles.AddUsersToRole(users, NewRoleName);
            Roles.RemoveUsersFromRole(users, OldRoleName);
            Roles.DeleteRole(OldRoleName);
        }

        protected void ProjectsGrid_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict["name"] = ((TextBox)(ProjectsGrid.Rows[e.RowIndex].Cells[0].Controls[0])).Text;
            dict["connstring_web"] = ((TextBox)(ProjectsGrid.Rows[e.RowIndex].Cells[1].Controls[0])).Text;
            dict["connstring_information_schema"] = ((TextBox)(ProjectsGrid.Rows[e.RowIndex].Cells[2].Controls[0])).Text;
            dict["server_type"] = ((TextBox)(ProjectsGrid.Rows[e.RowIndex].Cells[3].Controls[0])).Text;


            RenameRoleAndUsers(Constants.ADMIN_PREFIX + e.OldValues["name"].ToString(), 
                Constants.ADMIN_PREFIX + e.NewValues["name"].ToString());
            RenameRoleAndUsers(Constants.ARCHITECT_PREFIX + e.OldValues["name"].ToString(),
                Constants.ARCHITECT_PREFIX + e.NewValues["name"].ToString());

            sysDriver.UpdateProject((int)(e.Keys[0]), dict);
            ProjectsGrid.EditIndex = -1;
            projectsTable = sysDriver.GetProjects();
            Session["projectsTable"] = projectsTable;
            ProjectsGrid.DataSource = projectsTable;
            ProjectsGrid.DataBind();

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

            sysDriver.InsertProject(dict);

            projectsTable = sysDriver.GetProjects();
            Session["projectsTable"] = projectsTable;
            ProjectsGrid.DataSource = projectsTable;
            ProjectsGrid.DataBind();
            InsertProjectDetailsView.DataSource = projectsTable;
            InsertProjectDetailsView.DataBind();

            // only add the newly created!
            foreach (DataRow r in projectsTable.Rows) { 
                if(!Roles.RoleExists(Constants.ADMIN_PREFIX + r["name"]))
                    Roles.CreateRole(Constants.ADMIN_PREFIX + r["name"]);
                if (!Roles.RoleExists(Constants.ARCHITECT_PREFIX + r["name"]))
                    Roles.CreateRole(Constants.ARCHITECT_PREFIX + r["name"]);
            }
        }

    }
}