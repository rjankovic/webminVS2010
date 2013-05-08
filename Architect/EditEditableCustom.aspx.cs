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
using WPanel = System.Web.UI.WebControls.Panel;
using _min.Controls;
using _min.Interfaces;


namespace _min.Architect
{
    public partial class EditEditableCustom : System.Web.UI.Page
    {
        Dictionary<DataColumn, Dictionary<string, object>> customs;   
        MPanel interPanel;
        MinMaster mm;
                

        protected void Page_Init(object sender, EventArgs e)
        {
            mm = (MinMaster)Master;
            //if (!Page.IsPostBack)
            //{
                interPanel = (MPanel)Session["interPanel"];
                customs = (Dictionary<DataColumn, Dictionary<string, object>>)Session["customs"];
                backButton.PostBackUrl = backButton.GetRouteUrl("ArchitectEditEditableRoute", new { projectName = mm.ProjectName, panelId = interPanel.panelId });
            //}
            Table customSettingsTbl = new Table();


            foreach (DataColumn customCol in customs.Keys) {
                TableRow captionRow = new TableRow();
                TableCell captionCell = new TableCell();
                captionCell.Text = customCol.ColumnName;
                captionRow.Cells.Add(captionCell);
                customSettingsTbl.Rows.Add(captionRow);

                TableRow settingsRow = new TableRow();
                TableCell settingsCell = new TableCell();
                WPanel container = new WPanel();
                var factory = (ICustomizableColumnFieldFactory)customs[customCol]["factory"];
                if(!Page.IsPostBack){
                    MPanel oldPanel = mm.SysDriver.Panels[interPanel.panelId];
                    IColumnField oldVersion = (IColumnField)(from f in oldPanel.fields 
                                                             where f.GetType() == factory.ProductionType 
                                                             && ((IColumnField)f).ColumnName == customCol.ColumnName 
                                                             select f).FirstOrDefault<IField>();
                    if (oldVersion != null)
                        factory.LoadProduct((IColumnField)oldVersion);
                }
                factory.ShowForm(container);
                settingsCell.Controls.Add(container);
                settingsRow.Controls.Add(settingsCell);
                customSettingsTbl.Rows.Add(settingsRow);
            }

            MainPanel.Controls.Add(customSettingsTbl);
             
            
        }

        protected void SaveButton_Click(object sender, EventArgs e)
        {
            bool valid = true;
            validationResult.Items.Clear();
            foreach (Dictionary<string, object> colSettings in customs.Values) {
                var factory = (ICustomizableColumnFieldFactory)colSettings["factory"];
                factory.ValidateForm();
                if(factory.ErrorMessage != null){
                    validationResult.Items.Add(factory.ErrorMessage);
                    valid = false;
                }
            }
            if (valid) {
                List<IField> customizedFields = new List<Interfaces.IField>();
                foreach (DataColumn col in customs.Keys) { 
                    var factory = (ICustomizableColumnFieldFactory)customs[col]["factory"];

                    IColumnField field = factory.Create(col);
                    field.Required = (bool)customs[col]["required"];
                    field.Unique = (bool)customs[col]["unique"];
                    field.Caption = (string)customs[col]["caption"];
                    customizedFields.Add(field);
                }
                interPanel.AddFields(customizedFields);

                validationResult.Items.Add(new ListItem("Valid"));

                mm.SysDriver.BeginTransaction();
                mm.SysDriver.UpdatePanel(interPanel);
                Session.Clear();
                mm.SysDriver.IncreaseVersionNumber();
                mm.SysDriver.CommitTransaction();

                validationResult.Items.Add(new ListItem("Saved"));
                Response.RedirectToRoute("ArchitectEditEditableRoute", new { projectName = mm.ProjectName, panelId = interPanel.panelId });
            }
        }

    }

}