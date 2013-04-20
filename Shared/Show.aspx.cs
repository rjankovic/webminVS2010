using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using _min.Common;
using _min.Models;

using _min.Navigation;
using CE = _min.Common.Environment;
using CC = _min.Common.Constants;
using MPanel = _min.Models.Panel;

namespace _min.Shared
{
    public partial class Show : System.Web.UI.Page
    {

        //ISystemDriver sysDriver;
        //IStats stats;
        //_min.Models.Architect architect;
        DataTable dbLog;
        //_min.Models.Panel basePanel;
        _min.Models.Panel activePanel;
        Navigator navigator;
        //IWebDriver webDriver;
        ValidationSummary validationSummary;
        bool noSessionForActPanel = false;
        MinMaster mm;

        /// <summary>
        /// Initializes basic environment - Project, SystemDriver (contains architecture - all the panels linked in a tree), architect, stats, webDriver;
        /// sets the panel PK (if present), creates basic dropdown menu and lets the WebControls of the wohole rest of the page be created
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Init(object sender, EventArgs e)
        {
            mm = (MinMaster)Master;


            navigator = new Navigator(Page);


            // whether in a specific panel or not, we need a menu
            MenuEventHandler menuHandler = navigator.MenuHandle;
            ((TreeControl)mm.SysDriver.MainPanel.controls[0]).ToUControl(MainPanel, navigator.MenuHandler);

            // and addtitional options besides it for the architect
            if (CE.GlobalState == GlobalState.Architect)
            {
                LinkButton editMenuLink = new LinkButton();
                editMenuLink.PostBackUrl = editMenuLink.GetRouteUrl("ArchitectEditMenuRoute", new { projectName = CE.project.Name });
                editMenuLink.Text = "Edit menu structure";
                editMenuLink.CausesValidation = false;
                MainPanel.Controls.Add(editMenuLink);

                LinkButton editPanelsLink = new LinkButton();
                editPanelsLink.PostBackUrl = editPanelsLink.GetRouteUrl("ArchitectEditPanelsRoute", new { projectName = CE.project.Name });
                editPanelsLink.Text = "Edit panels structure";
                editPanelsLink.CausesValidation = false;
                MainPanel.Controls.Add(editPanelsLink);
            }

            // get the active panel, if exists
            if (Page.RouteData.Values.ContainsKey("panelId") && Page.RouteData.Values["panelId"].ToString() == "0")
                Page.RouteData.Values.Remove("panelId");
            if (Page.RouteData.Values.ContainsKey("panelId"))
            {
                // the basic panel
                if (Page.IsPostBack && Session[CC.SESSION_ACTIVE_PANEL] is MPanel)
                {
                    activePanel = (MPanel)Session[CC.SESSION_ACTIVE_PANEL];
                }
                else
                {
                    noSessionForActPanel = true;
                    activePanel = mm.SysDriver.Panels[Int32.Parse(Page.RouteData.Values["panelId"].ToString())];
                }

                // panels is focused to a certain item (row in table)
                if (Request.QueryString.Count > 0)
                {
                    SetRoutedPKForPanel(activePanel, Request.QueryString);
                }

                // which action on this panel leads where
                var controlTargetPanels = from _min.Models.Control c in activePanel.controls
                                          select c.ActionsDicitionary;

                Dictionary<UserAction, int> currentPanelActionPanels = new Dictionary<UserAction, int>();

                foreach (var x in controlTargetPanels)
                {
                    foreach (KeyValuePair<UserAction, int> item in x)
                    {
                        if (!currentPanelActionPanels.ContainsKey(item.Key))     // should be done differently
                            currentPanelActionPanels.Add(item.Key, item.Value);
                    }
                }

                navigator.setCurrentTableActionPanels(currentPanelActionPanels);

                CreateWebControlsForPanel(activePanel, MainPanel);

                // architect gets some extra controls for editing the proposal
                if (CE.GlobalState == GlobalState.Architect)
                {
                    if (activePanel.type == PanelTypes.Editable)
                    {
                        LinkButton editEditableLink = new LinkButton();
                        editEditableLink.PostBackUrl = editEditableLink.GetRouteUrl("ArchitectEditEditableRoute",
                            new { projectName = mm.ProjectName, panelid = Page.RouteData.Values["panelId"] });
                        editEditableLink.Text = "Edit panel structure";
                        editEditableLink.CausesValidation = false;
                        MainPanel.Controls.Add(editEditableLink);
                    }
                    else
                    {
                        LinkButton editNavLink = new LinkButton();
                        editNavLink.PostBackUrl = editNavLink.GetRouteUrl("ArchitectEditNavRoute",
                            new { projectName = CE.project.Name, panelid = Page.RouteData.Values["panelId"] });
                        editNavLink.Text = "Edit panel structure";
                        editNavLink.CausesValidation = false;
                        MainPanel.Controls.Add(editNavLink);
                    }
                }
            }



        }


        /// <summary>
        /// decodes the serailized panel Dataitem PK and sets the panel to it
        /// </summary>
        /// <param name="panel">active panel</param>
        /// <param name="queryString">URL-encoded PK</param>
        void SetRoutedPKForPanel(_min.Models.Panel panel, System.Collections.Specialized.NameValueCollection queryString)
        {
            DataRow row = mm.WebDriver.PKColRowFormat(panel);
            for (int i = 0; i < queryString.Count; i++)
            {
                string decoded = Server.UrlDecode(queryString[i]);
                if (row.Table.Columns[i].DataType == typeof(int))
                    row[i] = Int32.Parse(decoded);
                else
                    row[i] = decoded;
            }
            panel.PK = row;
        }


        void CreateWebControlsForPanel(MPanel activePanel, System.Web.UI.WebControls.Panel containerPanel)
        {
            List<AjaxControlToolkit.ExtenderControlBase> extenders = new List<AjaxControlToolkit.ExtenderControlBase>();
            List<BaseValidator> validators = new List<BaseValidator>();



            // no data in active panel => have to fill it
            if (!Page.IsPostBack || noSessionForActPanel)
            {
                if (CE.GlobalState == GlobalState.Architect)
                {
                    mm.WebDriver.FillPanelFKOptionsArchitect(activePanel);
                    if (activePanel.PK != null || activePanel.type != PanelTypes.Editable)
                        mm.WebDriver.FillPanelArchitect(activePanel);
                }
                else
                {
                    mm.WebDriver.FillPanelFKOptions(activePanel);
                    if (activePanel.PK != null || activePanel.type != PanelTypes.Editable)
                        mm.WebDriver.FillPanel(activePanel);
                }
                /*
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
                        // this basically cannot happen
                        throw new Exception("Unknown global application state (Proposal/Production).");
                    }
                }
                else if (activePanel.type == PanelTypes.Editable) 
                {
                    if (CE.GlobalState == GlobalState.Administer)
                        webDriver.FillPanelFKOptions(activePanel);
                    else webDriver.FillPanelFKOptionsArchitect(activePanel);
                }
                 */
            }

            if (activePanel.type == PanelTypes.Editable)
            {
                // create a table with fields and captions tod display
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
                    System.Web.UI.Control c = f.ToUControl(extenders);
                    validators.AddRange(f.GetValidator());

                    foreach (BaseValidator v in validators)
                    {
                        this.Form.Controls.Add(v);
                    }

                    fieldCell.Controls.Add(c);
                    row.Cells.Add(fieldCell);
                    tbl.Rows.Add(row);
                }

                MainPanel.Controls.Add(tbl);
            }

            // add the Constrols
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

            // finally the extenders of fields
            foreach (AjaxControlToolkit.ExtenderControlBase extender in extenders)
            {
                extender.EnableClientState = false;
                MainPanel.Controls.Add(extender);
            }

            foreach (BaseValidator validator in validators)
            {
                MainPanel.Controls.Add(validator);
            }

            validationSummary = new ValidationSummary();
            validationSummary.BorderWidth = 1;
            MainPanel.Controls.Add(validationSummary);


            // set the webcontrols from the stored value (initially null)
            foreach (Field f in activePanel.fields)
            {
                f.SetControlData();
            }

        }

        // TODO ...
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Page.IsPostBack && activePanel != null) // retrieve changes from webcontrols and save them to inner value
            {
                foreach (Field f in activePanel.fields)
                {
                    f.RetrieveData();
                }
            }
            if (activePanel != null)
            {

                activePanel.RetrieveDataFromFields();
            }
            Response.Cache.SetCacheability(HttpCacheability.NoCache);       // because of back button issues
            Response.Cache.SetExpires(DateTime.Now.AddSeconds(-1));
            Response.Cache.SetNoStore();
            Response.AppendHeader("Pragma", "no-cache");


        }


        private void UserActionCommandHandler(object sender, CommandEventArgs e)
        {
            bool valid = true;
            UserAction action = (UserAction)Enum.Parse(typeof(UserAction), e.CommandName.Substring(1));
            if (CE.GlobalState == GlobalState.Administer)
            {
                switch (action)
                {

                    case UserAction.Insert:
                        if (activePanel.type != PanelTypes.Editable)        // insert button under NavTable, should be handled differently
                            break;
                        mm.WebDriver.InsertIntoPanel(activePanel);
                        break;
                    case UserAction.Update:
                        try
                        {
                            mm.WebDriver.UpdatePanel(activePanel);
                        }
                        catch (ConstraintException ce)
                        {        // unique column constraint exception from db
                            valid = false;
                            _min.Common.ValidationError.Display(ce.Message, Page);
                        }
                        break;
                    case UserAction.Delete:
                        try
                        {
                            mm.WebDriver.DeleteFromPanel(activePanel);
                        }
                        catch (ConstraintException ce)
                        {        // unique column constraint exception from db
                            valid = false;
                            _min.Common.ValidationError.Display(ce.Message, Page);
                        }
                        break;
                    default:
                        throw new NotImplementedException("Unexpected user action type.");
                }
            }

            if (valid) navigator.ActionCommandHandle(sender, e);
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
                    GridView grid = (GridView)sender;       // must be fired from a gridview and a gridview only ! (is there another way??)
                    int selectedIndex = ((GridViewRow)((WebControl)(e.CommandSource)).NamingContainer).DataItemIndex;
                    var KeyValues = grid.DataKeys[selectedIndex].Values.Values;
                    DataTable keyTable = new DataTable();
                    foreach (string colName in activePanel.PKColNames)
                    {
                        keyTable.Columns.Add(colName);
                    }
                    DataRow r = keyTable.NewRow();
                    int i = 0;
                    foreach (var colValue in KeyValues)
                    {
                        r[i++] = colValue;
                    }
                    activePanel.PK = r;
                    mm.WebDriver.DeleteFromPanel(activePanel);
                    activePanel.PK = null;
                }

            }
        }
    }
}