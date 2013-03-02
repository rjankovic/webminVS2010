using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Routing;

namespace _min_t7
{
    public class Global : System.Web.HttpApplication
    {

        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            RouteTable.Routes.MapPageRoute("LoginRoute", "login", "~/Account/Login.aspx");
            RouteTable.Routes.MapPageRoute("UsersRoute", "sys/users", "~/Sys/Users.aspx");
            RouteTable.Routes.MapPageRoute("ProjectsRoute", "sys/projects", "~/Sys/Projects.aspx");

            RouteTable.Routes.MapPageRoute("ArchitectInitRoute", "architect/init/{projectName}", "~/Architect/InitProposal.aspx");
            RouteTable.Routes.MapPageRoute("ArchitectShowRoute", "architect/show/{projectName}", "~/Architect/Show.aspx");
            RouteTable.Routes.MapPageRoute("ArchitectShowPanelDefaultRoute", "architect/show/{projectName}/{panelId}", "~/Architect/Show.aspx");
            RouteTable.Routes.MapPageRoute("ArchitectShowPanelRoute", "architect/show/{projectName}/{panelId}/{action}", "~/Architect/Show.aspx");
            RouteTable.Routes.MapPageRoute("ArchitectShowPanelSpecRoute", "architect/show/{projectName}/{panelId}/{action}/{itemKey}", 
                "~/Architect/Show.aspx");

            RouteTable.Routes.MapPageRoute("ArchitectEditMenuRoute", "architect/editMenu/{projectName}", "~/Architect/EditMenu.aspx");
            
            RouteTable.Routes.MapPageRoute("ProductionShowRoute", "show/{projectName}", "~/Production/Show.aspx");
            RouteTable.Routes.MapPageRoute("ProductionShowPanelDefaultRoute", "show/{projectName}/{panelId}", "~/Prouction/Show.aspx");
            RouteTable.Routes.MapPageRoute("ProductionShowPanelRoute", "show/{projectName}/{panelId}/{action}", "~/Prouction/Show.aspx");
            RouteTable.Routes.MapPageRoute("ProductionShowPanelSpecRoute", "show/{projectName}/{panelId}/{action}/{itemKey}", 
                "~/Prouction/Show.aspx");

            
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
