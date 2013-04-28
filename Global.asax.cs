using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Routing;

namespace _min
{
    public class Global : System.Web.HttpApplication
    {

        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup

            RouteTable.Routes.MapPageRoute("DefaultRoute", "", "~/Default.aspx");
            
            RouteTable.Routes.MapPageRoute("AccountLoginRoute", "account/login", "~/Account/Login.aspx");
            RouteTable.Routes.MapPageRoute("AccountRegisterRoute", "account/register", "~/Account/Register.aspx");
            RouteTable.Routes.MapPageRoute("AccountChpswdRoute", "account/change-password", 
                "~/Account/ChangePassword.aspx");
            RouteTable.Routes.MapPageRoute("AccountRepswdRoute", "account/password-recovery", 
                "~/Account/PasswordRecovery.aspx");
            RouteTable.Routes.MapPageRoute("AccountPswdsuccRoute", "account/password-changed", 
                "~/Account/ChangePasswordSuccess.aspx");


            RouteTable.Routes.MapPageRoute("UsersRoute", "sys/users", "~/Sys/Users.aspx");
            RouteTable.Routes.MapPageRoute("ProjectsRoute", "sys/projects", "~/Sys/Projects.aspx");
            RouteTable.Routes.MapPageRoute("ProjectDetailRoute", "sys/projects/{projectId}", "~/Sys/ProjectDetail.aspx");

            RouteTable.Routes.MapPageRoute("ArchitectInitRoute", "architect/init/{projectName}", "~/Architect/InitProposal.aspx");
            RouteTable.Routes.MapPageRoute("ArchitectShowRoute", "architect/show/{projectName}", "~/Shared/Show.aspx");
            RouteTable.Routes.MapPageRoute("ArchitectShowPanelDefaultRoute", "architect/show/{projectName}/{panelId}", "~/Shared/Show.aspx");
            RouteTable.Routes.MapPageRoute("ArchitectShowPanelRoute", "architect/show/{projectName}/{panelId}/{action}", "~/Shared/Show.aspx");
            

            RouteTable.Routes.MapPageRoute("ArchitectEditMenuRoute", "architect/editMenu/{projectName}", 
                "~/Architect/EditMenu.aspx");
            RouteTable.Routes.MapPageRoute("ArchitectEditEditableRoute", "architect/editEditable/{projectName}/{panelId}", 
                "~/Architect/EditEditable.aspx");
            RouteTable.Routes.MapPageRoute("ArchitectEditNavRoute", "architect/editNav/{projectName}/{panelId}",
                "~/Architect/EditNav.aspx");
            RouteTable.Routes.MapPageRoute("ArchitectEditPanelsRoute", "architect/editPanels/{projectName}",
                "~/Architect/EditPanels.aspx");
            
            RouteTable.Routes.MapPageRoute("AdministerBrowseRoute", "admin/{projectName}", "~/Shared/Show.aspx");
            RouteTable.Routes.MapPageRoute("AdministerBrowsePanelDefaultRoute", "admin/{projectName}/{panelId}", "~/Shared/Show.aspx");
            RouteTable.Routes.MapPageRoute("AdministerBrowsePanelPagingRoute", "admin/{projectName}/{panelId}/{page}", "~/Shared/Show.aspx");
            RouteTable.Routes.MapPageRoute("AdministerBrowsePanelRoute", "admin/{projectName}/{panelId}/{action}", "~/Shared/Show.aspx");


            RouteTable.Routes.MapPageRoute("LockoutRoute", "lockout/{message}", "~/Error/Lockout.aspx");

        }

        void Application_End(object sender, EventArgs e)
        {
            //  Code that runs on application shutdown
        }

        void Application_Error(object sender, EventArgs e)
        {
            // Code that runs when an unhandled error occurs

        }

        void Session_Start(object sender, EventArgs e)
        {
            // Code that runs when a new session is started

        }

        void Session_End(object sender, EventArgs e)
        {
            // Code that runs when a session ends. 
            // Note: The Session_End event is raised only when the sessionstate mode
            // is set to InProc in the Web.config file. If session mode is set to StateServer 
            // or SQLServer, the event is not raised.

        }

    }
}
