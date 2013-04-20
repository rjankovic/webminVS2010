using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using _min.Common;
using _min.Interfaces;
using _min.Models;
using System.Configuration;
using System.Data;
using System.Web.Security;
using System.Web.UI.HtmlControls;
using CE = _min.Common.Environment;

namespace _min.Sys
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        ISystemDriver sysDriver;

        protected void Page_Load(object sender, EventArgs e)
        {

            string connString = ConfigurationManager.ConnectionStrings["LocalMySqlServer"].ConnectionString;
            sysDriver = new SystemDriverMySql(connString);

            if (!Page.IsPostBack)
            {
                MembershipUserCollection users = Membership.GetAllUsers();
                List<CE.Project> projects = sysDriver.GetProjectObjects();
                int userId = (int)(Membership.GetUser().ProviderUserKey);

                int globalAccess = sysDriver.GetUserRights(userId, null);
                if (globalAccess >= 1000)
                {
                    projects.Reverse();
                    projects.Add(new CE.Project(0, "All Projects", null, null, null, 0));
                    projects.Reverse();
                }
                else
                {
                    List<CE.Project> inaccessible = new List<CE.Project>();
                    foreach (CE.Project p in projects)
                    {
                        if (sysDriver.GetUserRights(userId, p.Id) < 1000) inaccessible.Add(p);
                    }
                    foreach (CE.Project p in inaccessible)
                        projects.Remove(p);
                }

                UserSelect.DataSource = users;
                UserSelect.DataValueField = "ProviderUserKey";
                UserSelect.DataTextField = "UserName";
                UserSelect.DataBind();
                ProjectSelect.DataSource = projects;
                ProjectSelect.DataValueField = "Id";
                ProjectSelect.DataTextField = "Name";
                ProjectSelect.DataBind();
                SetCheckboxes();
            }
        }

        protected void SomeSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetCheckboxes();
        }

        private void SetCheckboxes() {
            int project;
            int user;
            try
            {
                project = Int32.Parse(ProjectSelect.SelectedValue);
                user = Int32.Parse(UserSelect.SelectedValue);
            }
            catch
            { return; }
            int permissions;

            if (project != 0)
            {
                permissions = sysDriver.GetUserRights(user, project);
            }
            else {
                permissions = sysDriver.GetUserRights(user, null);
            }

            ListItemCollection lic = PermissionCheckboxList.Items;

            lic[0].Selected = permissions % 100 / 10 == 1;
            lic[1].Selected = permissions % 1000 / 100 == 1;
            lic[2].Selected = permissions / 1000 == 1;
        }

        protected void PermissionsSubmit_Click(object sender, EventArgs e)
        {
            int? project;
            int user;
            try
            {
                project = Int32.Parse(ProjectSelect.SelectedValue);
                user = Int32.Parse(UserSelect.SelectedValue);
            }
            catch { return; }
            if(project == 0) project = null;

            int permissions = 0;
            ListItemCollection lic = PermissionCheckboxList.Items;
            if (lic[0].Selected) permissions += 10;
            if (lic[1].Selected) permissions += 100;
            if (lic[2].Selected) permissions += 1000;

            sysDriver.SetUserRights(user, project, permissions);
        }

    }
}