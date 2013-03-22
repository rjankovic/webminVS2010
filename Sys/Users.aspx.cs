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

namespace _min_t7.Sys
{
    public partial class WebForm1 : System.Web.UI.Page
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            

            if (!Page.IsPostBack)
            {
                string connString = ConfigurationManager.ConnectionStrings["LocalMySqlServer"].ConnectionString;
                ISystemDriver sysDriver = new SystemDriverMySql(connString);
                
                MembershipUserCollection users = Membership.GetAllUsers();
                string[] projects = sysDriver.GetProjectNameList();

                AdministerCheckboxList.Enabled = false;
                ArchitectCheckboxList.Enabled = false;
                PermissionsSubmit.Enabled = false;

                foreach (string project in projects)
                {
                    AdministerCheckboxList.Items.Add(new ListItem(project, Constants.ADMIN_PREFIX + project));
                    ArchitectCheckboxList.Items.Add(new ListItem(project, Constants.ARCHITECT_PREFIX + project));
                }

                UserSelect.Items.Add("--choose a user--");
                foreach (MembershipUser user in users)
                {
                    UserSelect.Items.Add(user.UserName);
                }
            }
        }

        protected void UserSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (UserSelect.SelectedIndex != 0)
            {
                AdministerCheckboxList.Enabled = true;
                ArchitectCheckboxList.Enabled = true;
                PermissionsSubmit.Enabled = true;

                string userName = UserSelect.SelectedValue;
                foreach (ListItem item in AdministerCheckboxList.Items) { 
                    if(Roles.IsUserInRole(userName, item.Value))
                        item.Selected = true;
                    else
                        item.Selected = false;
                }
                foreach (ListItem item in ArchitectCheckboxList.Items) { 
                    if(Roles.IsUserInRole(userName, item.Value))
                        item.Selected = true;
                    else
                        item.Selected = false;
                }
            }
            else {
                AdministerCheckboxList.Enabled = false;
                ArchitectCheckboxList.Enabled = false;
                PermissionsSubmit.Enabled = false;
            }
        }

        protected void PermissionsSubmit_Click(object sender, EventArgs e)
        {
            string[] AddAdmin = 
                (from ListItem item in AdministerCheckboxList.Items where item.Selected && 
                     !Roles.IsUserInRole(UserSelect.SelectedValue, item.Value) select item.Value).ToList<string>().ToArray();
            string[] AddArchitect =
                (from ListItem item in ArchitectCheckboxList.Items where item.Selected && 
                     !Roles.IsUserInRole(UserSelect.SelectedValue, item.Value) select item.Value).ToList<string>().ToArray();
            string[] RemoveAdmin =
                (from ListItem item in AdministerCheckboxList.Items where !item.Selected && 
                     Roles.IsUserInRole(UserSelect.SelectedValue, item.Value) select item.Value).ToList<string>().ToArray();
            string[] RemoveArchitect =
                (from ListItem item in ArchitectCheckboxList.Items where !item.Selected && 
                     Roles.IsUserInRole(UserSelect.SelectedValue, item.Value) select item.Value).ToList<string>().ToArray();
            string[] user =  { UserSelect.SelectedValue };

            if (AddAdmin.Length > 0)
            Roles.AddUsersToRoles(user, AddAdmin);
            if (AddArchitect.Length > 0)
            Roles.AddUsersToRoles(user, AddArchitect);
            if (RemoveAdmin.Length > 0)
            Roles.RemoveUsersFromRoles(user, RemoveAdmin);
            if (RemoveArchitect.Length > 0)
            Roles.RemoveUsersFromRoles(user, RemoveArchitect);
        }
    }
}