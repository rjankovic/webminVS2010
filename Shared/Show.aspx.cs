using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using _min.Common;
using _min.Models;
using System.Text;

using _min.Interfaces;
using _min.Navigation;
using CE = _min.Common.Environment;
using CC = _min.Common.Constants;
using MPanel = _min.Models.Panel;
using WC = System.Web.UI.WebControls;

namespace _min.Shared
{
    public partial class Show : System.Web.UI.Page
    {

        //ISystemDriver sysDriver;
        //IStats stats;
        //_min.Models.Architect architect;
        DataTable dbLog;
        //_min.Models.Panel basePanel;
        _min.Models.Panel activePanel = null;
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
            /**/
            MenuEventHandler menuHandler = navigator.MenuHandle;
            ((TreeControl)mm.SysDriver.MainPanel.controls[0]).ToUControl(MainPanel, navigator.MenuHandler);
            /**/

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
                    Session[CC.SESSION_ACTIVE_PANEL] = null;
                    activePanel = mm.SysDriver.Panels[Int32.Parse(Page.RouteData.Values["panelId"].ToString())];
                }

                if (activePanel.isBaseNavPanel)
                {
                    Response.RedirectToRoute(
                        CE.GlobalState == GlobalState.Architect ? "ArchitectShowRoute" : "AdministerBrowseRoute",
                        new { projectName = CE.project.Name });
                    return;
                }

                // panels is focused to a certain item (row in table)
                if (Request.QueryString.Count > 0)
                {
                    SetRoutedPKForPanel(activePanel, Request.QueryString);
                }
                else 
                {
                    activePanel.PK = null;
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

                CreatePanelHeading(MainPanel);

                CreateWebControlsForPanel(activePanel, MainPanel);

            }
            else {
                CreatePanelHeading(MainPanel);
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


        void CreatePanelHeading(WC.WebControl container) {
            WC.Panel heading = new WC.Panel();

            if (activePanel is MPanel)
            {
                Label panelName = new Label();
                panelName.CssClass = "panelHeading";
                panelName.Text = activePanel.panelName;
                heading.Controls.Add(panelName);
            }
            if (CE.GlobalState == GlobalState.Architect)
            {
                LinkButton editMenuLink = new LinkButton();
                editMenuLink.PostBackUrl = editMenuLink.GetRouteUrl("ArchitectEditMenuRoute", new { projectName = CE.project.Name });
                editMenuLink.Text = "Edit menu structure";
                editMenuLink.CausesValidation = false;
                heading.Controls.Add(editMenuLink);

                LinkButton editPanelsLink = new LinkButton();
                editPanelsLink.PostBackUrl = editPanelsLink.GetRouteUrl("ArchitectEditPanelsRoute", new { projectName = CE.project.Name });
                editPanelsLink.Text = "Edit panels structure";
                editPanelsLink.CausesValidation = false;
                heading.Controls.Add(editPanelsLink);

                if (activePanel is MPanel)
                {

                    if (activePanel.type == PanelTypes.Editable)
                    {
                        LinkButton editEditableLink = new LinkButton();
                        editEditableLink.PostBackUrl = editEditableLink.GetRouteUrl("ArchitectEditEditableRoute",
                            new { projectName = mm.ProjectName, panelid = Page.RouteData.Values["panelId"] });
                        editEditableLink.Text = "Edit panel structure";
                        editEditableLink.CausesValidation = false;
                        heading.Controls.Add(editEditableLink);
                    }
                    else
                    {
                        LinkButton editNavLink = new LinkButton();
                        editNavLink.PostBackUrl = editNavLink.GetRouteUrl("ArchitectEditNavRoute",
                            new { projectName = CE.project.Name, panelid = Page.RouteData.Values["panelId"] });
                        editNavLink.Text = "Edit panel structure";
                        editNavLink.CausesValidation = false;
                        heading.Controls.Add(editNavLink);
                    }
                }

                LinkButton reproposeLink = new LinkButton();
                reproposeLink.CausesValidation = false;
                reproposeLink.Click += Repropose_Click;
                reproposeLink.Text = "Repropose";
                reproposeLink.OnClientClick = "return confirm('Think twice')";
                heading.Controls.Add(reproposeLink);
            }

            heading.CssClass = "panelHeading";
            if (heading.Controls.Count > 0)
            {
                container.Controls.Add(heading);
                WC.Panel clear = new WC.Panel();
                clear.CssClass = "clear";
                container.Controls.Add(clear);
            }
        }



        void CreateWebControlsForPanel(MPanel activePanel, System.Web.UI.WebControls.Panel containerPanel)
        {
            List<BaseValidator> validators = new List<BaseValidator>();

            validationSummary = new ValidationSummary();
            validationSummary.BorderWidth = 0;
            MainPanel.Controls.Add(validationSummary);
            
            // no data in active panel => have to fill it
            if (!Page.IsPostBack || noSessionForActPanel)
            {
                if (CE.GlobalState == GlobalState.Architect)
                {
                    foreach (IField field in activePanel.fields) {
                        field.InventData();
                    }
                    foreach (_min.Models.Control c in activePanel.controls)
                    {
                        c.InventData();
                    }
                }
                else
                {
                    mm.WebDriver.FillPanelFKOptions(activePanel);
                    if (activePanel.PK != null || activePanel.type != PanelTypes.Editable)
                        try
                        {
                            mm.WebDriver.FillPanel(activePanel);
                        }
                        catch (WebDriverDataModificationException me) {
                            // the row with the corresponding PK had been deleted in the meantime
                            _min.Common.ValidationError.Display(me.Message, Page);
                        }
                }
                 
            }

            if (activePanel.type == PanelTypes.Editable)
            {
                // create a table with fields and captions tod display
                Table tbl = new Table();
                tbl.CssClass = "formTbl";

                foreach (IField f in activePanel.fields)
                {
                    if (activePanel.PK == null && !Page.IsPostBack) f.Data = null;
                    //if (f.type == FieldTypes.Holder) throw new NotImplementedException("Holder fields not yet supported in UI");
                    TableRow row = new TableRow();
                    TableCell captionCell = new TableCell();
                    Label caption = new Label();
                    caption.Text = f.Caption;
                    captionCell.Controls.Add(caption);
                    row.Cells.Add(captionCell);

                    TableCell fieldCell = new TableCell();
                    System.Web.UI.WebControls.WebControl c = f.MyControl;
                    validators.AddRange(f.GetValidators());

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
                    ntc.ToUControl(containerPanel, new GridViewCommandEventHandler(GridCommandEventHandler), GridView_PageIndexChanging, GridView_Sorting);

                }
                else    // a simple Button or alike 
                {
                    if ((control.action == UserAction.Update || control.action == UserAction.Delete) && Page.Request.QueryString.Count == 0)
                        continue;
                    control.ToUControl(containerPanel, (CommandEventHandler)UserActionCommandHandler);
                }
                // not GridViewCommandEventHandler
            }

            
            foreach (BaseValidator validator in validators)
            {
                MainPanel.Controls.Add(validator);
            }


            // set the webcontrols from the stored value (initially null)
            foreach (_min.Interfaces.IField f in activePanel.fields)
            {
                f.FillData();
            }

        }


        protected void Page_Load(object sender, EventArgs e)
        {
            if (Page.IsPostBack && activePanel != null) // retrieve changes from webcontrols and save them to inner value
            {
                foreach (_min.Interfaces.IField f in activePanel.fields)
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
            Response.AppendHeader("Pragma", "no-cache");        // TODO in release


        }

        protected void Page_LoadComplete(object sender, EventArgs e) {
            Session[CC.SESSION_ACTIVE_PANEL] = activePanel;
        }


        private bool ServerValidate(MPanel panel) {
            bool valid = true;
            foreach (_min.Interfaces.IField f in panel.fields) {
                f.Validate();
                if(f.ErrorMessage != null){
                    valid = false;
                    _min.Common.ValidationError.Display(f.ErrorMessage, Page);
                }
            }
            return valid;
        }

        private void UserActionCommandHandler(object sender, CommandEventArgs e)
        {
            
            if (CE.GlobalState == GlobalState.Administer && mm.SysDriver.LockOwner(CE.project.Id, LockTypes.AdminLock) != null)
            {
                AdminLockAlert();
                return;
            }
            bool valid = true;
            UserAction action = (UserAction)Enum.Parse(typeof(UserAction), e.CommandName.Substring(1));
            if (CE.GlobalState == GlobalState.Administer)
            {
                try
                {
                    switch (action)
                    {

                        case UserAction.Insert:
                            if (activePanel.type != PanelTypes.Editable)        // insert button under NavTable, should be handled differently
                                break;
                            if (ServerValidate(activePanel))
                                mm.WebDriver.InsertIntoPanel(activePanel);
                            else valid = false;
                            break;
                        case UserAction.Update:
                            if (ServerValidate(activePanel))
                                mm.WebDriver.UpdatePanel(activePanel);
                            else valid = false;
                            break;
                        case UserAction.Delete:
                            mm.WebDriver.DeleteFromPanel(activePanel);
                            break;
                        default:
                            throw new NotImplementedException("Unexpected user action type.");
                    }
                }
                catch (ConstraintException ce)
                {    
                    // unique column constraint exception from db
                    valid = false;
                    _min.Common.ValidationError.Display(ce.Message, Page);
                }
                catch (WebDriverDataModificationException me)
                {
                    // data changed while edited
                    valid = false;
                    _min.Common.ValidationError.Display(me.Message, Page);
                }
            }

            if (valid) navigator.ActionCommandHandle(sender, e);
        }


        private void GridCommandEventHandler(object sender, GridViewCommandEventArgs e)
        {
            if (CE.GlobalState == GlobalState.Administer && mm.SysDriver.LockOwner(CE.project.Id, LockTypes.AdminLock) != null)
            {
                AdminLockAlert();
                return;
            }
            if (e.CommandName == "Page" || e.CommandName == "Sort") return;
            if ((UserAction)Enum.Parse(typeof(UserAction), (e.CommandName.Substring(1))) != UserAction.Delete)
            {
                    navigator.GridViewCommandHandler(sender, e);
            }
            else
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

                // so that the GridView will get refreshed
                Session[CC.SESSION_ACTIVE_PANEL] = null;
                Response.Redirect(Request.RawUrl);
            }
        }

        private void AdminLockAlert() {
            ScriptManager.RegisterStartupScript(Page, this.GetType(), "myScript",
                    "alert('The website administration structure is being changed currently. "
                            + "Please save your data elsewhere if neccessary and come back later.');", true);        
        }

        protected void GridView_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridView gv = (GridView)sender;
            DataView dataView = gv.DataSource as DataView;
            dataView.Sort = ViewState["currentGridViewSort"] as string;


            gv.DataSource = dataView;
            gv.PageIndex = e.NewPageIndex;
            gv.DataBind();
            LinkButton lb;
            foreach (TableCell c in gv.HeaderRow.Cells) {
                if (c.Controls.Count == 0) continue;
                lb = (LinkButton)(c.Controls[0]);
                string colName = lb.Text.Substring(0, lb.Text.Length - 3);
                if(dataView.Sort.StartsWith(colName))
                    lb.Text = colName + " [" + ((dataView.Sort.EndsWith("ASC")) ? "&#x25B2;" : "&#x25BC;") + "]";

            }
        }

        protected void GridView_Sorting(object sender, GridViewSortEventArgs e)
        {
            GridView gv = (GridView)sender;
            DataView dataView = gv.DataSource as DataView;
            
            string columnName = e.SortExpression;
            // colIndex is the index of the visible column that was clicked
            int colIndex = dataView.Table.Columns[columnName].Ordinal + 1;
            // in addition, the view can have more columns than the displayed, 
            // but they were positioned at the end of the view table
            

            string currentSort = ViewState["currentGridViewSort"] as string;
            string se = e.SortExpression + " ASC";
            if (se == currentSort) se = e.SortExpression + " DESC";
            ViewState["currentGridViewSort"] = se;

            dataView.Sort = se;
            gv.DataSource = dataView;
            gv.PageIndex = 0;
            gv.DataBind();

            foreach (TableCell hc in gv.HeaderRow.Cells)
            {
                if (hc.Controls.Count == 0) continue;
                LinkButton lb = hc.Controls[0] as LinkButton;
                if (lb == null) continue;
                lb.Text = lb.Text.Substring(0, lb.Text.Length - 3) + "[-]";
            }

            if (se.EndsWith("DESC"))
            {
                ((LinkButton)(gv.HeaderRow.Cells[colIndex].Controls[0])).Text = columnName + " [&#x25BC;]";
            }
            else
            {
                ((LinkButton)(gv.HeaderRow.Cells[colIndex].Controls[0])).Text = columnName + " [&#x25B2;]";
            }
        }

        protected void Repropose_Click(object sender, EventArgs e) {
            Response.RedirectToRoute("ArchitectInitRoute", new { projectName = CE.project.Name });
        }

        public override void VerifyRenderingInServerForm(System.Web.UI.Control control)
        {
            // for the M2N
        }
    }
}