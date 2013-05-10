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
        MinMaster mm;
        
        protected void Page_Load(object sender, EventArgs e)
        {
            mm = (MinMaster)Master;
            
            if (!Page.IsPostBack)
            {
                MembershipUserCollection users = Membership.GetAllUsers();
                List<CE.Project> projects = mm.SysDriver.GetProjectObjects();
                object userId = (Membership.GetUser().ProviderUserKey);

                int globalAccess = mm.SysDriver.GetUserRights(userId, null);
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
                        if (mm.SysDriver.GetUserRights(userId, p.Id) < 1000) inaccessible.Add(p);
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
                permissions = mm.SysDriver.GetUserRights(user, project);
            }
            else {
                permissions = mm.SysDriver.GetUserRights(user, null);
            }

            
            AdministerCb.Checked = permissions % 100 / 10 == 1;      // 10 = administer
            ArchitectCb.Checked = permissions % 1000 / 100 == 1;    // 100 = architect
            PermitCb.Checked = permissions % 10000 / 1000 == 1;  // 1000 access rights
        }

        protected void PermissionsSubmit_Click(object sender, EventArgs e)
        {
            int? project;
            object userId;
            try
            {
                project = Int32.Parse(ProjectSelect.SelectedValue);
                userId = UserSelect.SelectedValue;
            }
            catch { return; }
            if(project == 0) project = null;

            int permissions = 0;
            int originalGlobal = mm.SysDriver.GetUserRights(userId, null);
            if (AdministerCb.Checked) permissions += 10;
            if (ArchitectCb.Checked) permissions += 100;
            if (PermitCb.Checked) permissions += 1000;
            if (originalGlobal >= 10000 && project == null) permissions += 10000;   // so that the project manager permission is kept for the one owner

            mm.SysDriver.SetUserRights(userId, project, permissions);
        }

    }
}