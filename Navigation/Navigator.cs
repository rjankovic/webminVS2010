using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Web.UI.WebControls;
using _min.Common;
using CE = _min.Common.Environment;

namespace _min.Navigation
{
    public class Navigator
    {
        private readonly HttpResponse response;
        private Dictionary<UserAction, int> currentTableActionPanels;
        public readonly MenuEventHandler MenuHandler;
        public readonly GridViewCommandEventHandler GridViewCommandHandler;
        public readonly CommandEventHandler CommandHandler;


        public Navigator(HttpResponse response, Dictionary<UserAction, int> currentPanelActionPanels) {
            this.response = response;
            this.currentTableActionPanels = currentPanelActionPanels;

            MenuHandler = MenuHandle;
            GridViewCommandHandler = GridCommandHandle;
            CommandHandler = ActionCommandHandle;
        }

        public void MenuHandle(object sender, MenuEventArgs e) {
            Dictionary<string, object> routeData = new Dictionary<string, object>();
            //System.Web.Routing.RouteData routeData = new System.Web.Routing.RouteData();
            routeData.Add("panelId", e.Item.Value);
            //response.End();
            response.RedirectToRoute(CE.GlobalState == GlobalState.Architect ? "ArchitectShowPanelDefaultRoute"
                : "ProductionShowPanelDefaultRoute", new { panelId = e.Item.Value });
        }

        public void GridCommandHandle(object sender, GridViewCommandEventArgs e) {
            GridView grid = (GridView)sender;       // must be fired from a gridview and a gridview only ! (is there another way??)
            System.Web.Routing.RouteData routeData = new System.Web.Routing.RouteData();
            UserAction action = (UserAction)Enum.Parse(typeof(UserAction), e.CommandName);
            routeData.DataTokens.Add("panelId", currentTableActionPanels[action]);
            routeData.DataTokens.Add("action", e.CommandName);
            if (grid.SelectedIndex != -1)
            {
                routeData.DataTokens.Add("itemKey", grid.SelectedDataKey);
                response.RedirectToRoute(CE.GlobalState == GlobalState.Architect ? "ArchitectShowPanelRoute" : "ProductionShowPanelRoute",
                routeData);
            }
            routeData.DataTokens.Add("itemKey", grid.SelectedDataKey.Values);
            //response.End();
            response.RedirectToRoute(CE.GlobalState == GlobalState.Architect ? "ArchitectShowPanelSpecRoute" : "ProductionShowPanelSpecRoute",
                routeData);
        }

        public void ActionCommandHandle(object sender, CommandEventArgs e) {    // buttons must fire Commands !
            //response.End();
            UserAction action = (UserAction)Enum.Parse(typeof(UserAction), e.CommandName);
            System.Web.Routing.RouteData routeData = new System.Web.Routing.RouteData();
            routeData.DataTokens.Add("action", action);
            routeData.DataTokens.Add("panelId", currentTableActionPanels[action]);
            if(e.CommandArgument != null){
                routeData.DataTokens.Add("itemKey", e.CommandArgument);
                response.RedirectToRoute(CE.GlobalState == GlobalState.Architect ? "ArchitectShowPanelSpecRoute" : "ProductionShowPanelSpecRoute",
                    routeData);
            } else response.RedirectToRoute(CE.GlobalState == GlobalState.Architect ? 
                "ArchitectShowPanelSpecRoute" : "ProductionShowPanelSpecRoute", routeData);
        }
    }
}