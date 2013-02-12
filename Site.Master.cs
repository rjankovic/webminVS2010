using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Security;
using _min.Common;

namespace _min_t7
{
    public partial class SiteMaster : System.Web.UI.MasterPage
    {
        protected void Page_Init(object sender, EventArgs e) {
            if (!Page.IsPostBack)
            {
                MembershipUser u = null;
                try
                {
                    u = Membership.GetUser();
                }
                catch (ArgumentNullException e2)
                {
                    if (Request.Url.LocalPath != "/Account/Login.aspx")
                        Response.Redirect("~/login");
                    return;
                }


                if (u == null)
                {
                    if (Request.Url.LocalPath != "/Account/Login.aspx")
                        Response.Redirect("~/login");
                    return;
                }
                string[] roles = Roles.GetRolesForUser();

                // administration menu
                var administratorOf = from r in roles
                                      where r.IndexOf(Constants.ADMIN_PREFIX) == 0
                                      select
                                          r.Substring(Constants.ADMIN_PREFIX.Length) as string;
                MenuItem administerItem = new MenuItem("Administer", "admin");
                foreach (string site in administratorOf)
                {
                    administerItem.ChildItems.Add(new MenuItem(site, site, null, "~/admin/" + site));
                }
                NavigationMenu.Items.AddAt(0, administerItem);

                // architect menu
                var architectOf = from r in roles
                                  where r.IndexOf(Constants.ARCHITECT_PREFIX) == 0
                                  select
                                      r.Substring(Constants.ARCHITECT_PREFIX.Length) as string;
                MenuItem architectItem = new MenuItem("Architect", "architect");
                foreach (string site in architectOf)
                {
                    architectItem.ChildItems.Add(new MenuItem(site, site, null, "~/architect/show/" + Server.UrlEncode(site)));
                }
                NavigationMenu.Items.AddAt(1, architectItem);

                // user & projects management
                if (Roles.IsUserInRole("System Architect"))
                {
                    NavigationMenu.Items.Add(new MenuItem("Manage users", "users", null, "~/sys/users"));
                    NavigationMenu.Items.Add(new MenuItem("Manage projects", "projects", null, "~/sys/projects"));
                }
            }

        }

        protected void Page_Load(object sender, EventArgs e)
        {
            
        }
    }
}
