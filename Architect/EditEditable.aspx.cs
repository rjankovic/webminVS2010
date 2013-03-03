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
using _min.Controls;
using System.Data;

namespace _min_t7.Architect
{
    public partial class EditEditable : System.Web.UI.Page
    {
        ISystemDriver sysDriver;
        IWebDriver webDriver;
        IStats stats;
        _min.Models.Architect architect;
        MPanel actPanel;
        List<FK> FKs;
        List<M2NMapping> mappings;
        

        protected void Page_Init(object sender, EventArgs e)
        {

            //_min.Common.Environment.GlobalState = GlobalState.Architect;

            //if (!Page.IsPostBack && !Page.RouteData.Values.ContainsKey("panelId"))
            //    Session.Clear();
            _min.Models.Panel architecture = null;
            if (Session["Architecture"] is _min.Models.Panel)
            {
                architecture = (MPanel)Session["Architecture"];
            }

            string projectName = Page.RouteData.Values["projectName"] as string;
            int panelId = Int32.Parse(Page.RouteData.Values["panelId"] as string);

            sysDriver = new SystemDriverMySql(ConfigurationManager.ConnectionStrings["LocalMySqlServer"].ConnectionString);
            _min.Common.Environment.project = sysDriver.getProject(projectName);

            string WebDbName = Regex.Match(CE.project.connstringWeb, ".*Database=\"?([^\";]+)\"?.*").Groups[1].Value;
            webDriver = new WebDriverMySql(CE.project.connstringWeb);

            stats = new StatsMySql(CE.project.connstringIS, WebDbName);

            architect = new _min.Models.Architect(sysDriver, stats);

            actPanel = sysDriver.getPanel(panelId, false);
            DataColumnCollection cols = stats.ColumnTypes[actPanel.tableName];

            string[] fieldTypes = new string[] { FieldTypes.Varchar.ToString(), FieldTypes.Bool.ToString(), 
                FieldTypes.Date.ToString(), FieldTypes.DateTime.ToString(), 
                FieldTypes.Decimal.ToString(), FieldTypes.Ordinal.ToString(), FieldTypes.Text.ToString()
                 };
            string[] FKtype = new string[] { FieldTypes.FK.ToString() };
            string[] mappingType = new string[] { FieldTypes.M2NMapping.ToString() };
            string[] validationRules = Enum.GetNames(typeof(ValidationRules));
            string[] requiredRule = new string[] { Enum.GetName(typeof(ValidationRules), ValidationRules.Required) };
            FKs = stats.foreignKeys(actPanel.tableName);
            mappings = new List<M2NMapping>();
            // stats will take care of the following...
            //if(stats.Mappings.ContainsKey(actPanel.tableName))
            mappings = stats.Mappings[actPanel.tableName];


            panelName.Text = actPanel.panelName;
            
            foreach (DataColumn col in cols) {          // std. fields (incl. FKs)

                Field f = actPanel.fields.Find(x => x.column == col.ColumnName);
                
                TableRow r = new TableRow();
                r.ID = col.ColumnName;

                TableCell nameCell = new TableCell();
                Label nameLabel = new Label();
                nameLabel.Text = col.ColumnName;
                nameCell.Controls.Add(nameLabel);
                r.Cells.Add(nameCell);

                TableCell presentCell = new TableCell();
                CheckBox present = new CheckBox();
                present.Checked = f != null;
                presentCell.Controls.Add(present);
                r.Cells.Add(presentCell);

                FK fk = FKs.Find(x => x.myColumn == col.ColumnName);
                
                TableCell typeCell = new TableCell();
                DropDownList dl = new DropDownList();
                if (fk == null)
                    dl.DataSource = fieldTypes;
                else
                    dl.DataSource = FKtype;
                dl.DataBind();
                typeCell.Controls.Add(dl);
                r.Cells.Add(typeCell);

                if (f != null) {
                    dl.SelectedIndex = dl.Items.IndexOf(dl.Items.FindByText(f.type.ToString()));
                }

                TableCell validCell = new TableCell();
                CheckBoxList vcbl = new CheckBoxList();
                if (fk == null)
                    vcbl.DataSource = validationRules;
                else
                    vcbl.DataSource = requiredRule;
                vcbl.DataBind();
                validCell.Controls.Add(vcbl);
                r.Cells.Add(validCell);

                if (f != null) {
                    foreach (ValidationRules rule in f.validationRules) {
                        vcbl.Items.FindByText(rule.ToString()).Selected = true;
                    }
                }

                TableCell captionCell = new TableCell();
                TextBox caption = new TextBox();
                captionCell.Controls.Add(caption);
                r.Cells.Add(captionCell);

                if (f != null) {
                    caption.Text = f.caption;
                }
                
                tbl.Rows.Add(r);
            }

            
            foreach (M2NMapping mapping in mappings) {
                M2NMappingField f = actPanel.fields.Find(
                    x => x is M2NMappingField && ((M2NMappingField)x).Mapping.myColumn == mapping.myColumn) as M2NMappingField;

                TableRow r = new TableRow();

                TableCell nameCell = new TableCell();
                Label nameLabel = new Label();
                nameLabel.Text = mapping.myTable + " to " + mapping.refTable + " via " + mapping.mapTable;
                nameCell.Controls.Add(nameLabel);
                r.Cells.Add(nameCell);

                TableCell presentCell = new TableCell();
                CheckBox present = new CheckBox();
                present.Checked = f != null;
                presentCell.Controls.Add(present);
                r.Cells.Add(presentCell);

                TableCell typeCell = new TableCell();
                DropDownList dl = new DropDownList();
                dl.DataSource = mappingType;
                dl.DataBind();
                typeCell.Controls.Add(dl);
                r.Cells.Add(typeCell);


                TableCell validCell = new TableCell();  // leave it empty
                r.Cells.Add(validCell);

               
                TableCell captionCell = new TableCell();
                TextBox caption = new TextBox();
                captionCell.Controls.Add(caption);
                r.Cells.Add(captionCell);

                if (f != null)
                {
                    caption.Text = f.caption;
                }
                

                mappingsTbl.Rows.Add(r);
            }


            string[] actionTypes = new string[] { UserAction.Insert.ToString(), 
                UserAction.Update.ToString(), 
                UserAction.Delete.ToString() };                       // controls
            List<string> activeActions = (from _min.Models.Control control in actPanel.controls 
                                          select Enum.GetName(typeof(UserAction), control.action)).ToList<string>();

            allowedActions.DataSource = actionTypes;
            allowedActions.DataBind();
            foreach (ListItem item in allowedActions.Items)
                item.Selected = activeActions.Contains(item.Value);
            
            
            backButton.PostBackUrl = backButton.GetRouteUrl("ArchitectShowRoute", new { projectName = projectName });
            

        }

        


        protected void Page_Load(object sender, EventArgs e)
        {

            
        }

        protected void Page_LoadComplete(object sender, EventArgs e) {

            //Session["Architecture"] = sysDriver.MainPanel;
        }

        protected void SaveButton_Click(object sender, EventArgs e)
        {
            List<Field> fields = new List<Field>();
            int i = 1;

            foreach (DataColumn col in stats.ColumnTypes[actPanel.tableName])
            {       // standard fields

                TableRow r = tbl.Rows[i++];
                if (!((CheckBox)r.Cells[1].Controls[0]).Checked)
                    continue;
                // label, present, type, valid, caption

                FieldTypes type = (FieldTypes)Enum.Parse(typeof(FieldTypes), 
                    ((DropDownList)r.Cells[2].Controls[0]).SelectedValue);

                List<ValidationRules> rules = new List<ValidationRules>();
                CheckBoxList checkBoxList = (CheckBoxList)r.Cells[3].Controls[0];
                foreach (ListItem item in checkBoxList.Items)
                {
                    if (item.Selected)
                        rules.Add((ValidationRules)Enum.Parse(typeof(ValidationRules), item.Value));
                }

                string caption = ((TextBox)r.Cells[4].Controls[0]).Text;
                if (caption == "") caption = null;

                Field newField;
                if (type == FieldTypes.FK)
                    newField = new FKField(0, col.ColumnName, actPanel.panelId, FKs.Find(x => x.myColumn == col.ColumnName), caption);
                else
                    newField = new Field(0, col.ColumnName, type, actPanel.panelId, caption);
                newField.validationRules = rules;
                fields.Add(newField);
            }

            i = 1;
            foreach (M2NMapping mapping in mappings)
            {                // mappings
                TableRow r = mappingsTbl.Rows[i++];
                // label, present, type (mappingType), valid (req?), caption
                if (!((CheckBox)r.Cells[1].Controls[0]).Checked)
                    continue;

                // must be mappingType...

                List<ValidationRules> rules = new List<ValidationRules>();
                // no validation for a mapping

                string caption = ((TextBox)r.Cells[4].Controls[0]).Text;
                

                M2NMappingField m2nf = new M2NMappingField(0, null, 0, mapping, caption);
                fields.Add(m2nf);
            }

            List<_min.Models.Control> controls = new List<_min.Models.Control>();           // controls
            
            foreach (ListItem item in allowedActions.Items)
            {
                if (item.Selected)
                {
                    _min.Models.Control c = new _min.Models.Control(0, item.Value,
                        (UserAction)Enum.Parse(typeof(UserAction), item.Value));
                    c.targetPanel = actPanel.controls[0].targetPanel;
                    
                    c.targetPanelId = actPanel.controls[0].targetPanelId;
                    
                    controls.Add(c);
                }
            }

            MPanel resPanel = new MPanel(actPanel.tableName, actPanel.panelId, PanelTypes.Editable, new List<MPanel>(),
                fields, controls, actPanel.PKColNames, null, actPanel.parent);
            resPanel.panelName = panelName.Text;

            List<string> errorMsgs;
            bool valid = architect.checkPanelProposal(resPanel, out errorMsgs);
            //BulletedList validationResult = new BulletedList();
            validationResult.Items.Clear();
            if (valid)
            {
                validationResult.Items.Add(new ListItem("Valid"));

                actPanel = resPanel;
                sysDriver.StartTransaction();
                sysDriver.updatePanel(actPanel);
                Session.Clear();
                sysDriver.CommitTransaction();
                validationResult.Items.Add(new ListItem("Saved"));
            }
            else
            {
                foreach (string s in errorMsgs)
                {
                    validationResult.Items.Add(new ListItem(s));
                }
            }

        }

    }

}