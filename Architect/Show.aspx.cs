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
    public partial class Show : System.Web.UI.Page
    {
        ISystemDriver sysDriver;
        IStats stats;
        _min.Models.Architect architect;
        DataTable dbLog;
        _min.Models.Panel basePanel;
        _min.Models.Panel activePanel;
        Navigator navigator;
        IWebDriver webDriver;


        protected void Page_Init(object sender, EventArgs e)
        {
            if (Page.RouteData.Values.ContainsKey("panelId") && Page.RouteData.Values["panelId"].ToString() == "0")
                Page.RouteData.Values.Remove("panelId");

            //_min.Common.Environment.GlobalState = GlobalState.Architect;

            if (!Page.IsPostBack && !Page.RouteData.Values.ContainsKey("panelId"))
                Session.Clear();
                
            _min.Models.Panel architecture = null;
            if (Session["Architecture"] is _min.Models.Panel)
            {
                architecture = (MPanel)Session["Architecture"];
            }

            Dictionary<UserAction, int> currentPanelActionPanels = new Dictionary<UserAction, int>();
            string projectName = Page.RouteData.Values["projectName"] as string;

            sysDriver = new SystemDriverMySql(ConfigurationManager.ConnectionStrings["LocalMySqlServer"].ConnectionString);
            _min.Common.Environment.project = sysDriver.getProject(projectName);


            if (!sysDriver.ProposalExists())
            {
                Response.RedirectToRoute("ArchitectInitRoute", RouteData);
                Response.End();
                //return;
            }
            
            string WebDbName = Regex.Match(CE.project.connstringWeb, ".*Database=\"?([^\";]+)\"?.*").Groups[1].Value;
            stats = new StatsMySql(CE.project.connstringIS, WebDbName);
            webDriver = new WebDriverMySql(CE.project.connstringWeb);
            architect = new _min.Models.Architect(sysDriver, stats);

            
            sysDriver.InitArchitecture(architecture);
            Session["Architecture"] = sysDriver.MainPanel;

            if (Page.RouteData.Values.ContainsKey("panelId"))
            {
                activePanel = sysDriver.Panels[Int32.Parse(Page.RouteData.Values["panelId"].ToString())];
            }

            basePanel = sysDriver.MainPanel;
            if (!Page.IsPostBack)
            {



                //Session["basePanel"] = basePanel;
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
                    
                    if(RouteData.Values.ContainsKey("itemKey")){
                        SetRoutedPKForPanel(activePanel, RouteData.Values["itemKey"] as string);
                    }
                    //Session["activePanel"] = activePanel;
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
                //basePanel = (_min.Models.Panel)Session["basePanel"];
                currentPanelActionPanels = (Dictionary<UserAction, int>)Session["currentPanelActionpanels"];
                //activePanel = (_min.Models.Panel)Session["activePanel"];
            }
            
            navigator = new Navigator(Response, currentPanelActionPanels);

            MenuEventHandler menuHandler = navigator.MenuHandle;
            Menu baseMenu = (Menu)((TreeControl)basePanel.controls[0]).ToUControl(navigator.MenuHandler);
            baseMenu.ID = "baseMenu";
            baseMenu.EnableViewState = false;
            MainPanel.Controls.Add(baseMenu);
            LinkButton editMenuLink = new LinkButton();
            editMenuLink.PostBackUrl = editMenuLink.GetRouteUrl("ArchitectEditMenuRoute", new { projectName = projectName } );
            editMenuLink.Text = "Edit menu structure";
            editMenuLink.CausesValidation = false;
            MainPanel.Controls.Add(editMenuLink);

            if (Page.RouteData.Values.ContainsKey("panelId"))
            {
                CreateWebControlsForPanel(activePanel, MainPanel);
                if (activePanel.type == PanelTypes.Editable) {
                    LinkButton editEditableLink = new LinkButton();
                    editEditableLink.PostBackUrl = editMenuLink.GetRouteUrl("ArchitectEditEditableRoute", 
                        new { projectName = projectName, panelid = Page.RouteData.Values["panelId"] });
                    editEditableLink.Text = "Edit panel structure";
                    editEditableLink.CausesValidation = false;
                    MainPanel.Controls.Add(editEditableLink);
                }
            }
            else {
                /*
                _min.Controls.TreeBuilderControl tb = new _min.Controls.TreeBuilderControl();
                tb.ID = "TB1";
                tb.SetInitialState(((TreeControl)basePanel.controls[0]).storedHierarchyData, sysDriver.MainPanel);
                MainPanel.Controls.Add(tb);
                 */ 
            }

        }




        void SetRoutedPKForPanel(_min.Models.Panel panel, string PKval) {
            DataRow row = webDriver.PKColRowFormat(panel);
            string[] parts = PKval.Split('/');
            for (int i = 0; i < row.Table.Columns.Count; i++) {
                if (row.Table.Columns[i].DataType == typeof(int))
                    row[i] = Int32.Parse(parts[i]);
                else
                    row[i] = parts[i];
                }
            panel.PK = row;
        }

        void CreateWebControlsForPanel(MPanel activePanel, System.Web.UI.WebControls.Panel containerPanel){
            List<AjaxControlToolkit.ExtenderControlBase> extenders = new List<AjaxControlToolkit.ExtenderControlBase>();
            List<BaseValidator> validators = new List<BaseValidator>();
            if (!Page.IsPostBack)
            {
                if (activePanel.type == PanelTypes.NavTree
                    || activePanel.type == PanelTypes.NavTable
                    || RouteData.Values.ContainsKey("itemKey"))
                    webDriver.FillPanelArchitect(activePanel);
            }
            if (activePanel.type == PanelTypes.Editable) {
                //return;

                Table tbl = new Table();
                tbl.EnableViewState = false;
                //tbl.ID = "EditTbl";
                
                foreach (Field f in activePanel.fields)
                {
                    //continue;
                    if (f.type == FieldTypes.Holder) throw new NotImplementedException("Holder fields not yet supported in UI");
                    TableRow row = new TableRow();
                    TableCell captionCell = new TableCell();
                    Label caption = new Label();
                    caption.Text = f.caption;
                    captionCell.Controls.Add(caption);
                    row.Cells.Add(captionCell);

                    TableCell fieldCell = new TableCell();
                    System.Web.UI.Control c = f.ToUControl(extenders);
                    validators.AddRange(f.GetValidator());
                    
                    foreach (BaseValidator v in validators) {
                        this.Form.Controls.Add(v);
                        //MainPanel.Controls.Add(v);
                    }
                
                    c.EnableViewState = true;
                    fieldCell.Controls.Add(c);      // TODO: check
                    row.Cells.Add(fieldCell);
                    tbl.Rows.Add(row);
                }
                MainPanel.Controls.Add(tbl);
            }
            //if (activePanel.type == PanelTypes.Editable) return;
            

            foreach (_min.Models.Control control in activePanel.controls) {
                    if (control is TreeControl)
                    {
                        containerPanel.Controls.Add(((TreeControl)control).ToUControl(navigator.TreeHandler));
                    }
                    else if(control is NavTableControl){        // it is a mere gridview of a summary panel
                        containerPanel.Controls.Add(((NavTableControl)control).ToUControl(
                            new GridViewCommandEventHandler(GridCommandEventHandler)));
                }
                else    // a simple Button or alike 
                    containerPanel.Controls.Add(control.ToUControl((CommandEventHandler)UserActionCommandHandler));
                                                                    // not GridViewCommandEventHandler
            }
            //if (activePanel.type == PanelTypes.Editable) return;
            foreach (AjaxControlToolkit.ExtenderControlBase extender in extenders) {
                extender.EnableClientState = false;
                MainPanel.Controls.Add(extender);
            }

            foreach (BaseValidator validator in validators) {
                MainPanel.Controls.Add(validator);
            }

            ValidationSummary validationSummary = new ValidationSummary();
            validationSummary.BorderWidth = 1;
            MainPanel.Controls.Add(validationSummary);


            if (Page.RouteData.Values.ContainsKey("panelId"))
            {

                foreach (Field f in activePanel.fields)
                {
                    f.SetControlData();
                }
            }
            
        }


        protected void Page_Load(object sender, EventArgs e)
        {

            
        }

        protected void Page_LoadComplete(object sender, EventArgs e) {

            if (Page.RouteData.Values.ContainsKey("panelId"))
            {
                if (Page.IsPostBack)
                {
                    foreach (Field f in activePanel.fields)
                    {
                        f.RetrieveData();
                    }
                }

            }

            Session["activePanel"] = activePanel;
        }


        private void UserActionCommandHandler(object sender, CommandEventArgs e) {
            if (_min.Common.Environment.GlobalState == GlobalState.Production) { 
                // do the action
            }
            if (e.CommandName.Substring(1) != "Delete")
                navigator.ActionCommandHandle(sender, e);
        }

        protected override void LoadViewState(object savedState)
        {
            base.LoadViewState(savedState);
        }

        protected override void LoadControlState(object savedState)
        {
            base.LoadControlState(savedState);
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            System.Web.UI.Control c = Controls[0];
        }

        private void GridCommandEventHandler(object sender, GridViewCommandEventArgs e){
            if ((UserAction)Enum.Parse(typeof(UserAction), (e.CommandName.Substring(1))) != UserAction.Delete)
            {
                navigator.GridViewCommandHandler(sender, e);
            }
            else { 
            
            }
        }
    }

}