using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;

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
        public readonly CommandEventHandler TreeHandler;


        public Navigator(HttpResponse response, Dictionary<UserAction, int> currentPanelActionPanels) {
            this.response = response;
            this.currentTableActionPanels = currentPanelActionPanels;

            MenuHandler = MenuHandle;
            GridViewCommandHandler = GridCommandHandle;
            CommandHandler = ActionCommandHandle;
            TreeHandler = TreeHandle;
        }

        public void MenuHandle(object sender, MenuEventArgs e) {
            Dictionary<string, object> routeData = new Dictionary<string, object>();
            //System.Web.Routing.RouteData routeData = new System.Web.Routing.RouteData();
            routeData.Add("panelId", e.Item.Value);
            //response.End();
            response.RedirectToRoute(CE.GlobalState == GlobalState.Architect ? "ArchitectShowPanelDefaultRoute"
                : "ProductionShowPanelDefaultRoute", new { panelId = e.Item.Value });
        }

        public void TreeHandle(object sender, CommandEventArgs e) {
            int navId = (int)(e.CommandArgument);

            UserAction action = (UserAction)Enum.Parse(typeof(UserAction), e.CommandName.Substring(1));
            response.RedirectToRoute(CE.GlobalState == GlobalState.Architect
                ? "ArchitectShowPanelSpecRoute" : "ProductionShowPanelSpecRoute",
                new
                {
                    
                    panelId = currentTableActionPanels[currentTableActionPanels.ContainsKey(action)?action:UserAction.Multiple],
                    action = e.CommandName,
                    itemKey = navId    // trees must have single-column int PKs
                });
        }

        public void GridCommandHandle(object sender, GridViewCommandEventArgs e) {
            
            GridView grid = (GridView)sender;       // must be fired from a gridview and a gridview only ! (is there another way??)
            int selectedIndex = ((GridViewRow)((WebControl)(e.CommandSource)).NamingContainer).DataItemIndex;
            //grid.DataKeys[selectedIndex].Values
            //Dictionary<string, object> data = new Dictionary<string, object>();
            string command = e.CommandName.Substring(1);
            UserAction action = (UserAction)Enum.Parse(typeof(UserAction), command);
            RouteValueDictionary data = new RouteValueDictionary();
            
            data.Add("panelId", currentTableActionPanels[currentTableActionPanels.ContainsKey(action)?action:UserAction.Multiple]);
            data.Add("action", command);
            //object[]

                //Serializer 
            data.Add("itemKey", grid.DataKeys[selectedIndex].Values.Values);
            //if (selectedIndex != -1)
            //{
                //data.Add("itemKey", grid.DataKeys[selectedIndex]);

                response.RedirectToRoute(CE.GlobalState == GlobalState.Architect 
                    ? "ArchitectShowPanelSpecRoute" : "ProductionShowPanelSpecRoute",
                    new { panelId = currentTableActionPanels[action], action = e.CommandName, 
                        itemKey = DataKey2Url(grid.DataKeys[selectedIndex]) } );
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
            foreach (string s in parts) lParts.Add(s);

            return string.Join("/", lParts.ToArray());
        }


        
        public void ActionCommandHandle(object sender, CommandEventArgs e) {    // buttons must fire Commands !
            //response.End();
            
            UserAction action = (UserAction)Enum.Parse(typeof(UserAction), e.CommandName);
            System.Web.Routing.RouteData routeData = new System.Web.Routing.RouteData();
            routeData.DataTokens.Add("action", action);
            routeData.DataTokens.Add("panelId", currentTableActionPanels[action]);
            if(e.CommandArgument.ToString() != ""){   // TODO ...
                routeData.DataTokens.Add("itemKey", e.CommandArgument);
                response.RedirectToRoute(CE.GlobalState == GlobalState.Architect ? "ArchitectShowPanelSpecRoute" : "ProductionShowPanelSpecRoute",
                    new { action = action, panelId = currentTableActionPanels[action], itemKey = e.CommandArgument } );
            }
            else response.RedirectToRoute(CE.GlobalState == GlobalState.Architect ?
              "ArchitectShowPanelRoute" : "ProductionShowPanelRoute",
              new { action = action, panelId = currentTableActionPanels[action] });
        }
    }
}