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

    /// <summary>
    /// handles the redirections when GridView, Button and Menu events are fired
    /// </summary>
    public class Navigator
    {
        private readonly System.Web.UI.Page Page;
        private Dictionary<UserAction, int> currentTableActionPanels;
        
        // these handlers are assigned to WebControl events (mostly in Show.aspx.cz)
        public readonly MenuEventHandler MenuHandler;
        public readonly GridViewCommandEventHandler GridViewCommandHandler;
        public readonly CommandEventHandler CommandHandler;
        public readonly CommandEventHandler TreeHandler;

        public Navigator(Page page) {
            this.Page = page;
            
            MenuHandler = MenuHandle;
            GridViewCommandHandler = GridCommandHandle;
            CommandHandler = ActionCommandHandle;
            TreeHandler = TreeHandle;
        }

        /// <summary>
        /// tell the navigtator, from the current panel, which action will be redirected to which panel.
        /// </summary>
        /// <param name="actions"></param>
        public void setCurrentTableActionPanels(Dictionary<UserAction, int> actions) {
            currentTableActionPanels = actions;
        }

        public void MenuHandle(object sender, MenuEventArgs e) {
            if (e.Item.Value == string.Empty) return; // this is an unbound menu item (why not)
            Page.Response.RedirectToRoute(CE.GlobalState == GlobalState.Architect ? "ArchitectShowPanelDefaultRoute"
                : "AdministerBrowsePanelDefaultRoute", new { panelId = e.Item.Value });
        }

        /// <summary>
        /// for NavTrees...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TreeHandle(object sender, CommandEventArgs e) {
            int navId = (int)(e.CommandArgument);
            // the commad name is prefixed with a "_" so that it doesn`t collide with the predefined .NET command names and doesn`t fire
            // specialized events
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
            string command = e.CommandName.Substring(1);
            UserAction action = (UserAction)Enum.Parse(typeof(UserAction), command);
            
            string routeUrl = Page.GetRouteUrl(CE.GlobalState == GlobalState.Architect 
                    ? "ArchitectShowPanelRoute" : "AdministerBrowsePanelRoute",
                    new { panelId = currentTableActionPanels[action], action = command} );

            string queryString = "";   // for the architect
            if(CE.GlobalState == GlobalState.Administer) queryString = DataKey2Url(grid.DataKeys[selectedIndex]);
            Page.Response.Redirect(routeUrl + queryString);
        }

        /// <summary>
        /// encodes the url parts of the primary key thet shall be (after redirect) assigned to a newly created Panel;
        /// the PK parts are saved into the query string as PK0=...,PK1=..., . . ., this is a convention shared with the the decoder
        /// (Show.SetRoutedPKForPanel).
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string DataKey2Url(DataKey key) {
            // the PK pats could not be appended route to form a /-separated string - ASP.NET doesn`t allow special characters before the '?' mark in the URL.
            var parts = from object p in key.Values.Values select p.ToString();
            List<string> lParts = new List<string>();
            StringBuilder sb = new StringBuilder("?");
            bool first = true;
            int i = 0;
            foreach (string s in parts){
                if(!first) sb.Append("&");
                first = false;
                sb.Append("IKP" + i.ToString() + "=" + HttpUtility.UrlEncode(s));
                i++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// for buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ActionCommandHandle(object sender, CommandEventArgs e) {
            UserAction action = (UserAction)Enum.Parse(typeof(UserAction), e.CommandName.Substring(1));
            if(e.CommandArgument.ToString() != ""){
                Page.Response.RedirectToRoute(CE.GlobalState == GlobalState.Architect ? "ArchitectShowPanelSpecRoute" : "AdministerBrowsePanelSpecRoute",
                    new { action = action, panelId = currentTableActionPanels[action], itemKey = e.CommandArgument } );
            }   // Insert / Editpanel button (process and send the user back)
            else Page.Response.RedirectToRoute(CE.GlobalState == GlobalState.Architect ?
              "ArchitectShowPanelRoute" : "AdministerBrowsePanelRoute",
              new { action = action, panelId = currentTableActionPanels[action] });
        }
    }
}