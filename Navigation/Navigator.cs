using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Text;
using System.Web.UI;

using System.Web.UI.WebControls;
using _min.Common;
using CE = _min.Common.Environment;

namespace _min.Navigation
{
    public class Navigator
    {
        private readonly System.Web.UI.Page Page;
        private Dictionary<UserAction, int> currentTableActionPanels;
        public readonly MenuEventHandler MenuHandler;
        public readonly GridViewCommandEventHandler GridViewCommandHandler;
        public readonly CommandEventHandler CommandHandler;
        public readonly CommandEventHandler TreeHandler;


        public Navigator(Page page, Dictionary<UserAction, int> currentPanelActionPanels) {
            this.Page = page;
            this.currentTableActionPanels = currentPanelActionPanels;

            MenuHandler = MenuHandle;
            GridViewCommandHandler = GridCommandHandle;
            CommandHandler = ActionCommandHandle;
            TreeHandler = TreeHandle;
        }

        public void MenuHandle(object sender, MenuEventArgs e) {
            //Dictionary<string, object> routeData = new Dictionary<string, object>();
            //System.Web.Routing.RouteData routeData = new System.Web.Routing.RouteData();
            //routeData.Add("panelId", e.Item.Value);
            //response.End();
            Page.Response.RedirectToRoute(CE.GlobalState == GlobalState.Architect ? "ArchitectShowPanelDefaultRoute"
                : "AdministerBrowsePanelDefaultRoute", new { panelId = e.Item.Value });
        }

        public void TreeHandle(object sender, CommandEventArgs e) {
            int navId = (int)(e.CommandArgument);

            UserAction action = (UserAction)Enum.Parse(typeof(UserAction), e.CommandName.Substring(1));
            string routeUrl = Page.GetRouteUrl(CE.GlobalState == GlobalState.Architect
                ? "ArchitectShowPanelRoute" : "AdministerBrowsePanelRoute",
                new
                {
                    panelId = currentTableActionPanels[currentTableActionPanels.ContainsKey(action)?action:UserAction.Multiple],
                    action = e.CommandName
                });
            string queryString = "?IKP0=" + navId;
            Page.Response.Redirect(routeUrl + queryString);
        }

        public void GridCommandHandle(object sender, GridViewCommandEventArgs e) {
            
            GridView grid = (GridView)sender;       // must be fired from a gridview and a gridview only ! (is there another way??)
            int selectedIndex = ((GridViewRow)((WebControl)(e.CommandSource)).NamingContainer).DataItemIndex;
            //grid.DataKeys[selectedIndex].Values
            //Dictionary<string, object> data = new Dictionary<string, object>();
            string command = e.CommandName.Substring(1);
            UserAction action = (UserAction)Enum.Parse(typeof(UserAction), command);
            
            string routeUrl = Page.GetRouteUrl(CE.GlobalState == GlobalState.Architect 
                    ? "ArchitectShowPanelRoute" : "AdministerBrowsePanelRoute",
                    new { panelId = currentTableActionPanels[action], action = command} );
            string queryString = DataKey2Url(grid.DataKeys[selectedIndex]);
            Page.Response.Redirect(routeUrl + queryString);
                //new { panelId = currentTableActionPanels[action], action = e.CommandName, 
                //    itemKey = (grid.DataKeys[selectedIndex].Values) } );
            //}
            //data.Add("itemKey", grid.DataKeys[].Values);
            //response.End();
            //response.RedirectToRoute(CE.GlobalState == GlobalState.Architect ? "ArchitectShowPanelSpecRoute" : "ProductionShowPanelSpecRoute",
            //    data);
        }

        private string DataKey2Url(DataKey key) {
            var parts = from object p in key.Values.Values select p.ToString();
            List<string> lParts = new List<string>();
            StringBuilder sb = new StringBuilder("?");
            bool first = true;
            int i = 0;
            foreach (string s in parts){
                if(!first) sb.Append("&");
                first = false;
                // item key part
                sb.Append("IKP" + i.ToString() + "=" + HttpUtility.UrlEncode(s));
                i++;
            }
            return sb.ToString();
        }
        
        public void ActionCommandHandle(object sender, CommandEventArgs e) {    // buttons must fire Commands !
            //response.End();
            
            UserAction action = (UserAction)Enum.Parse(typeof(UserAction), e.CommandName.Substring(1));
            System.Web.Routing.RouteData routeData = new System.Web.Routing.RouteData();
            routeData.DataTokens.Add("action", action);
            routeData.DataTokens.Add("panelId", currentTableActionPanels[action]);
            if(e.CommandArgument.ToString() != ""){   // TODO ... but this should not happen (buttons are in edit panels or as "Isnert")
                routeData.DataTokens.Add("itemKey", e.CommandArgument);
                Page.Response.RedirectToRoute(CE.GlobalState == GlobalState.Architect ? "ArchitectShowPanelSpecRoute" : "AdministerBrowsePanelSpecRoute",
                    new { action = action, panelId = currentTableActionPanels[action], itemKey = e.CommandArgument } );
            }
            else Page.Response.RedirectToRoute(CE.GlobalState == GlobalState.Architect ?
              "ArchitectShowPanelRoute" : "AdministerBrowsePanelRoute",
              new { action = action, panelId = currentTableActionPanels[action] });
        }
    }
}