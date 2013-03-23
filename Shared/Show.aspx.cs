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

namespace _min_t7.Shared
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

            if (Page.IsPostBack) {
                activePanel = (MPanel)Session["activePanel"];
            }

            basePanel = sysDriver.MainPanel;
            //basePanel = sysDriver.MainPanel;

            if (Page.RouteData.Values.ContainsKey("panelId"))
            {
                int panelId = Int32.Parse(Page.RouteData.Values["panelId"].ToString());

                if (Request.QueryString.Count > 0)
                {
                    SetRoutedPKForPanel(activePanel, Request.QueryString);
                }
                //Session["activePanel"] = activePanel;
                currentPanelActionPanels = new Dictionary<UserAction, int>();
                var controlTargetPanels = from _min.Models.Control c in activePanel.controls
                                          select c.ActionsDicitionary;
                foreach (var x in controlTargetPanels)
                {
                    foreach (KeyValuePair<UserAction, int> item in x)
                    {
                        if(!currentPanelActionPanels.ContainsKey(item.Key))     // should be done differently
                            currentPanelActionPanels.Add(item.Key, item.Value);
                    }
                }

            }


            
            navigator = new Navigator(Page, currentPanelActionPanels);


            MenuEventHandler menuHandler = navigator.MenuHandle;
            ((TreeControl)basePanel.controls[0]).ToUControl(MainPanel, navigator.MenuHandler);
            //baseMenu.ID = "baseMenu";
            //baseMenu.EnableViewState = false;
            //MainPanel.Controls.Add(baseMenu);
            if (CE.GlobalState == GlobalState.Architect)
            {
                LinkButton editMenuLink = new LinkButton();
                editMenuLink.PostBackUrl = editMenuLink.GetRouteUrl("ArchitectEditMenuRoute", new { projectName = projectName });
                editMenuLink.Text = "Edit menu structure";
                editMenuLink.CausesValidation = false;
                MainPanel.Controls.Add(editMenuLink);

                LinkButton editPanelsLink = new LinkButton();
                editPanelsLink.PostBackUrl = editPanelsLink.GetRouteUrl("ArchitectEditPanelsRoute", new { projectName = projectName });
                editPanelsLink.Text = "Edit panels structure";
                editPanelsLink.CausesValidation = false;
                MainPanel.Controls.Add(editPanelsLink);
            }
            if (Page.RouteData.Values.ContainsKey("panelId"))
            {
                CreateWebControlsForPanel(activePanel, MainPanel);
                if (CE.GlobalState == GlobalState.Architect)
                {
                    if (activePanel.type == PanelTypes.Editable)
                    {
                        LinkButton editEditableLink = new LinkButton();
                        editEditableLink.PostBackUrl = editEditableLink.GetRouteUrl("ArchitectEditEditableRoute",
                            new { projectName = projectName, panelid = Page.RouteData.Values["panelId"] });
                        editEditableLink.Text = "Edit panel structure";
                        editEditableLink.CausesValidation = false;
                        MainPanel.Controls.Add(editEditableLink);
                    }
                    else
                    {
                        LinkButton editNavLink = new LinkButton();
                        editNavLink.PostBackUrl = editNavLink.GetRouteUrl("ArchitectEditNavRoute",
                            new { projectName = projectName, panelid = Page.RouteData.Values["panelId"] });
                        editNavLink.Text = "Edit panel structure";
                        editNavLink.CausesValidation = false;
                        MainPanel.Controls.Add(editNavLink);
                    }
                }
            }
            else
            {
                /*
                _min.Controls.TreeBuilderControl tb = new _min.Controls.TreeBuilderControl();
                tb.ID = "TB1";
                tb.SetInitialState(((TreeControl)basePanel.controls[0]).storedHierarchyData, sysDriver.MainPanel);
                MainPanel.Controls.Add(tb);
                 */
            }

        }




        void SetRoutedPKForPanel(_min.Models.Panel panel, System.Collections.Specialized.NameValueCollection queryString)
        {
            DataRow row = webDriver.PKColRowFormat(panel);
            for (int i = 0; i < queryString.Count; i++)
            {
                string decoded = Server.UrlDecode(queryString[i]);
                if (row.Table.Columns[i].DataType == typeof(int))
                    row[i] = Int32.Parse(decoded);
                else
                    row[i] = decoded;
            }
            /*
            if(panel.PK is DataRow)
                for (int i = 0; i < panel.PK.Table.Columns.Count; i++) { 
                    if(panel.PK[i] != row[i])
                        Page.Response.Redirect(Page.Request.Url.ToString(), true);
                }
             */ 
                panel.PK = row;
        }

        void CreateWebControlsForPanel(MPanel activePanel, System.Web.UI.WebControls.Panel containerPanel)
        {
            List<AjaxControlToolkit.ExtenderControlBase> extenders = new List<AjaxControlToolkit.ExtenderControlBase>();
            List<BaseValidator> validators = new List<BaseValidator>();
            if (!Page.IsPostBack)
            {
                if (activePanel.type == PanelTypes.NavTree
                    || activePanel.type == PanelTypes.NavTable
                    || Request.QueryString.Count > 0)
                {
                    if (CE.GlobalState == GlobalState.Architect)
                    {
                        webDriver.FillPanelFKOptionsArchitect(activePanel);
                        webDriver.FillPanelArchitect(activePanel);
                    }
                    else if (CE.GlobalState == GlobalState.Administer)
                    {
                        webDriver.FillPanelFKOptions(activePanel);
                        webDriver.FillPanel(activePanel);
                    }
                    else
                    {
                        //throw new Exception("WTF?");
                        throw new Exception("Unknown global application state (Proposal/Production).");
                    }
                }
                else if (activePanel.type == PanelTypes.Editable)   // Insert: fill the FKs and Mappings
                {
                    if (CE.GlobalState == GlobalState.Administer)
                        webDriver.FillPanelFKOptions(activePanel);
                    else webDriver.FillPanelFKOptionsArchitect(activePanel);
                }
            }
            if (activePanel.type == PanelTypes.Editable)
            {
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

                    foreach (BaseValidator v in validators)
                    {
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


            foreach (_min.Models.Control control in activePanel.controls)
            {
                if (control is TreeControl)
                {
                    ((TreeControl)control).ToUControl(containerPanel, navigator.TreeHandler);
                }
                else if (control is NavTableControl)
                {        // it is a mere gridview of a summary panel
                    NavTableControl ntc = (NavTableControl)control;
                    ntc.ToUControl(containerPanel, new GridViewCommandEventHandler(GridCommandEventHandler));
                    
                }
                else    // a simple Button or alike 
                {
                    if ((control.action == UserAction.Update || control.action == UserAction.Delete) && Page.Request.QueryString.Count == 0)
                        continue;
                    control.ToUControl(containerPanel, (CommandEventHandler)UserActionCommandHandler);
                    }
                // not GridViewCommandEventHandler
            }
            //if (activePanel.type == PanelTypes.Editable) return;
            foreach (AjaxControlToolkit.ExtenderControlBase extender in extenders)
            {
                extender.EnableClientState = false;
                MainPanel.Controls.Add(extender);
            }

            foreach (BaseValidator validator in validators)
            {
                MainPanel.Controls.Add(validator);
            }

            ValidationSummary validationSummary = new ValidationSummary();
            validationSummary.BorderWidth = 1;
            MainPanel.Controls.Add(validationSummary);

            // insert -> set values to null, but only for the first time (probably would work the same without this, but...)
            if (Page.Request.QueryString.Count == 0 && !Page.IsPostBack)        
            {
                foreach (Field f in activePanel.fields)
                {
                    f.value = null;
                }
            }
            // set the webcontrols from the stored value
            foreach (Field f in activePanel.fields)
            {
                f.SetControlData();
            }

        }


        protected void Page_Load(object sender, EventArgs e)
        {
            //if (Page.Request.QueryString.Count > 0)
            //{
                if (Page.IsPostBack && activePanel != null) // retrieve changes from webcontrols and save them to inner value
                {
                    foreach (Field f in activePanel.fields)
                    {
                        if (!(f is M2NMappingField))
                            f.RetrieveData();
                    }
                }
                if (activePanel != null) {

                    activePanel.RetrieveDataFromFields();
                }
            //}
                Response.Cache.SetCacheability(HttpCacheability.NoCache);       // because of back button issues
                Response.Cache.SetExpires(DateTime.Now.AddSeconds(-1));
                Response.Cache.SetNoStore();
                Response.AppendHeader("Pragma", "no-cache");


        }

        protected void Page_LoadComplete(object sender, EventArgs e)
        {
            if (Page.IsPostBack && activePanel != null)
            {
                bool reason = false;
                foreach (Field f in activePanel.fields)     // M2N must be retrieved after event handling - it uses event to change value
                {
                    if (f is M2NMappingField)
                    {
                        f.RetrieveData();
                        reason = true;
                    }
                }
                if (reason) activePanel.RetrieveDataFromFields();
            }
            Session["activePanel"] = activePanel;

        }

        private void UserActionCommandHandler(object sender, CommandEventArgs e)
        {
            UserAction action = (UserAction)Enum.Parse(typeof(UserAction), e.CommandName.Substring(1));
            if (CE.GlobalState == GlobalState.Administer)
            {
                switch (action)
                {
                    case UserAction.Insert:
                        if (activePanel.type != PanelTypes.Editable)        // insert button under NavTable, should be handled differently
                            break;
                        webDriver.insertPanel(activePanel);
                        break;
                    case UserAction.Update:
                        webDriver.updatePanel(activePanel);
                        break;
                    case UserAction.Delete:
                        webDriver.deletePanel(activePanel);
                        break;
                    default:
                        throw new NotImplementedException("Unexpected user action type.");
                }
            }

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
            // System.Web.UI.Control c = Controls[0]; hmm??
        }

        private void GridCommandEventHandler(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "Page") return;
            if ((UserAction)Enum.Parse(typeof(UserAction), (e.CommandName.Substring(1))) != UserAction.Delete)
            {
                navigator.GridViewCommandHandler(sender, e);
            }
            else
            {
                if (CE.GlobalState == GlobalState.Administer || true)
                {
                    /*Page.ClientScript.RegisterStartupScript(this.GetType(), "showVal", "alert('DO NOT!');", true);*/
                    GridView grid = (GridView)sender;       // must be fired from a gridview and a gridview only ! (is there another way??)
                    int selectedIndex = ((GridViewRow)((WebControl)(e.CommandSource)).NamingContainer).DataItemIndex;
                    var KeyValues = grid.DataKeys[selectedIndex].Values.Values;
                    DataTable keyTable = new DataTable();
                    foreach (string colName in activePanel.PKColNames) {
                        keyTable.Columns.Add(colName);
                    }
                    DataRow r = keyTable.NewRow();
                    int i = 0;
                    foreach (var colValue in KeyValues) {
                        r[i++] = colValue;
                    }
                    activePanel.PK = r;
                    webDriver.deletePanel(activePanel);
                    activePanel.PK = null;
                }

            }
        }
    }

}