using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using _min.Common;
using _min.Interfaces;
using _min.Models;
using System.Configuration;
using System.Text.RegularExpressions;

using _min.Navigation;
using CC = _min.Common.Constants;
using CE = _min.Common.Environment;
using MPanel = _min.Models.Panel;

namespace _min_t7.Architect
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        ISystemDriver sysDriver;
        IStats stats;
        _min.Models.Architect architect;
        DataTable dbLog;
        _min.Models.Panel basePanel;
        _min.Models.Panel activePanel;
        Navigator navigator;


        protected void Page_Init(object sender, EventArgs e)
        {

            //_min.Common.Environment.GlobalState = GlobalState.Architect;

            Dictionary<UserAction, int> currentPanelActionPanels = new Dictionary<UserAction, int>();
            string projectName = Page.RouteData.Values["projectName"] as string;
            sysDriver = new SystemDriverMySql(ConfigurationManager.ConnectionStrings["LocalMySqlServer"].ConnectionString);
            _min.Common.Environment.project = sysDriver.getProject(projectName);
            string WebDbName = Regex.Match(CE.project.connstringWeb, ".*Database=\"?([^\";]+)\"?.*").Groups[1].Value;
            stats = new StatsMySql(CE.project.connstringIS, WebDbName);
            architect = new _min.Models.Architect(sysDriver, stats);


            if (!Page.IsPostBack)
            {


                if (!sysDriver.ProposalExists())
                {
                    Response.RedirectToRoute("ArchitectInitRoute", RouteData);
                    Response.End();
                    //return;
                }

                basePanel = sysDriver.GetBasePanel();
                Session["basePanel"] = basePanel;
                //Session["sysDriver"] = sysDriver;
                //Session["stats"] = stats;
                //Session["architect"] = architect;
                
                
                //currentPanelActionPanels = 
                //    RouteData.Values.ContainsKey("panelId") ? sysDriver.GetPanelActionPanels(Int32.Parse(RouteData.Values["panelId"].ToString())) : null;
                //Session["currentPanelActionPanels"] = currentPanelActionPanels;
                //navigator = new Navigator(Response, currentPanelActionPanels);

                //baseMenu.ID = "BaseMenu";
                //baseMenu.EnableViewState = true;
                
                // TODO: bind hanlers!!
                
                //MainPanel.Controls.Add(basePanel.controls[0].ToUControl());
                if (Page.RouteData.Values.ContainsKey("panelId"))
                {
                    int panelId = Int32.Parse(Page.RouteData.Values["panelId"].ToString());
                    activePanel = sysDriver.getPanel(panelId, false);
                    Session["activePanel"] = activePanel;
                    currentPanelActionPanels = new Dictionary<UserAction, int>();
                    var controlTargetPanels = from _min.Models.Control c in activePanel.controls 
                                              select new { action = c.action, targetId = c.targetPanelId };
                    foreach (var x in controlTargetPanels) {
                        // TODO: targetPanelId should be specified for every control - this cast should not be here
                        currentPanelActionPanels.Add(x.action, (int)x.targetId);
                    }
                    Session["currentPanelActionPanels"] = currentPanelActionPanels;
                    //CreateWebControlsForPanel(activePanel, MainPanel);
                }

            }
            else
            {
                //stats = (IStats)Session["stats"];
                //sysDriver = (ISystemDriver)Session["sysDriver"];
                //architect = (_min.Models.Architect)Session["architect"];
                basePanel = (_min.Models.Panel)Session["basePanel"];
                currentPanelActionPanels = (Dictionary<UserAction, int>)Session["currentPanelActionpanels"];
                activePanel = (_min.Models.Panel)Session["activePanel"];
            }
            
            navigator = new Navigator(Response, currentPanelActionPanels);

            MenuEventHandler menuHandler = navigator.MenuHandle;
            Menu baseMenu = (Menu)((TreeControl)basePanel.controls[0]).ToUControl(navigator.MenuHandler);
            MainPanel.Controls.Add(baseMenu);
                
            if (Page.RouteData.Values.ContainsKey("panelId"))
            {
                CreateWebControlsForPanel(activePanel, MainPanel);
            }

        }

        void CreateWebControlsForPanel(MPanel activePanel, System.Web.UI.WebControls.Panel containerPanel){
            List<AjaxControlToolkit.ExtenderControlBase> extenders = new List<AjaxControlToolkit.ExtenderControlBase>();
            if (activePanel.type == PanelTypes.Editable) {
                Table tbl = new Table();
                foreach (Field f in activePanel.fields)
                {
                    if (f.type == FieldTypes.Holder) throw new NotImplementedException("Holder fields not yet supported in UI");
                    TableRow row = new TableRow();
                    TableCell captionCell = new TableCell();
                    Label caption = new Label();
                    caption.Text = f.caption;
                    captionCell.Controls.Add(caption);
                    row.Cells.Add(captionCell);

                    TableCell fieldCell = new TableCell();
                    fieldCell.Controls.Add(f.ToUControl(extenders));      // TODO: check
                    row.Cells.Add(fieldCell);
                    tbl.Rows.Add(row);
                }
                MainPanel.Controls.Add(tbl);
            }

            foreach (_min.Models.Control control in activePanel.controls) {
                if (control.data is DataTable && control.data.Rows.Count > 0) {     // first condition SHOULD be enough!
                    if(control is TreeControl)
                        throw new NotImplementedException();
                    else        // it is a mere gridview of a summary panel
                    containerPanel.Controls.Add(control.ToUControl(navigator.GridCommandHandle));
                }
                else    // a simple Button or alike 
                    containerPanel.Controls.Add(control.ToUControl((CommandEventHandler)UserActionCommandHandler));
                                                                    // not GridViewCommandEventHandler
            }

            foreach (AjaxControlToolkit.ExtenderControlBase extender in extenders) {
                MainPanel.Controls.Add(extender);
            }
        }

        private void UserActionCommandHandler(object sender, CommandEventArgs e) {
            if (_min.Common.Environment.GlobalState == GlobalState.Production) { 
                // do the action
            }
            navigator.ActionCommandHandle(sender, e);
        }
    }

}