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


namespace _min.Architect
{
    /// <summary>
    /// edit a panel of Editable tpee
    /// </summary>
    public partial class EditEditable : System.Web.UI.Page
    {
        
        MPanel actPanel;
        List<FK> FKs;
        List<M2NMapping> mappings;
        MinMaster mm;

        protected void Page_Init(object sender, EventArgs e)
        {
            mm = (MinMaster)Master;
            _min.Common.Environment.GlobalState = GlobalState.Architect;

            int panelId = Int32.Parse(Page.RouteData.Values["panelId"] as string);

            actPanel = mm.SysDriver.Panels[panelId];
            DataColumnCollection cols = mm.Stats.ColumnTypes[actPanel.tableName];

            // the field types - default and special subsets allowed for special data types - i.e. o foereign key cannot be edited via anything but
            // a FKFiels
            string[] fieldTypes = new string[] { FieldTypes.ShortText.ToString(), FieldTypes.Bool.ToString(), 
                FieldTypes.Date.ToString(), FieldTypes.DateTime.ToString(), FieldTypes.Text.ToString()
                 };
            string[] EnumType = new string[] { FieldTypes.Enum.ToString() };
            string[] FKtype = new string[] { FieldTypes.FK.ToString() };
            string[] mappingType = new string[] { FieldTypes.M2NMapping.ToString() };
            string[] validationRules = Enum.GetNames(typeof(ValidationRules));
            
            // replace required with empty field and push it to the top
            for (int i = 0; i < validationRules.Length; i++) {
                if (validationRules[i] == ValidationRules.Required.ToString())
                {
                    validationRules[i] = validationRules[0];
                    validationRules[i] = "";
                    break;
                }
            }
            // a  FKField can be only required - let referential integrity take care of the rest
            string[] requiredRule = new string[] { Enum.GetName(typeof(ValidationRules), ValidationRules.Required) };
            FKs = mm.Stats.FKs[actPanel.tableName];
            mappings = new List<M2NMapping>();
            mappings = mm.Stats.Mappings[actPanel.tableName];

            panelName.Text = actPanel.panelName;
            
            // create a datarow for each column, specifiing...
            foreach (DataColumn col in cols) {          // std. fields (incl. FKs)

                Field f = actPanel.fields.Find(x => x.column == col.ColumnName && !(x is M2NMappingField));
                
                TableRow r = new TableRow();
                r.ID = col.ColumnName;

                //...the name,...
                TableCell nameCell = new TableCell();
                Label nameLabel = new Label();
                nameLabel.Text = col.ColumnName;
                nameCell.Controls.Add(nameLabel);
                r.Cells.Add(nameCell);

                //...whether the column will be accessible to editation at all,...
                TableCell presentCell = new TableCell();
                CheckBox present = new CheckBox();
                present.Checked = f != null;
                presentCell.Controls.Add(present);
                r.Cells.Add(presentCell);

                FK fk = FKs.Find(x => x.myColumn == col.ColumnName);
                if(f != null && f is FKField) fk = ((FKField)f).FK;
                //...the FieldType,...
                TableCell typeCell = new TableCell();
                DropDownList dl = new DropDownList();
                if (f is EnumField)
                    dl.DataSource = EnumType;
                else if (fk == null)
                    dl.DataSource = fieldTypes;
                else
                    dl.DataSource = FKtype;
                dl.DataBind();
                typeCell.Controls.Add(dl);
                r.Cells.Add(typeCell);

                //...what column of the referred table to display in the dropdown 
                TableCell FKDisplayCell = new TableCell();
                // set default value if the field was originally present in the editation form
                if (fk != null)
                {
                    DropDownList fkddl = new DropDownList();
                    fkddl.DataSource = mm.Stats.ColumnsToDisplay[fk.refTable];
                    fkddl.DataBind();
                    if(f != null)
                        fkddl.SelectedIndex = fkddl.Items.IndexOf(fkddl.Items.FindByValue(((FKField)f).FK.displayColumn));
                    FKDisplayCell.Controls.Add(fkddl);
                    
                }
                r.Cells.Add(FKDisplayCell);

                if (f != null) {
                    dl.SelectedIndex = dl.Items.IndexOf(dl.Items.FindByText(f.type.ToString()));
                }
                // else set default baed on  datatype - could build a dictionary...


                //...the validation rules...
                TableCell validCell = new TableCell();

                CheckBox requiredCb = new CheckBox();
                Label requiredlabel = new Label();
                requiredlabel.Text = "required ";
                requiredCb.Checked = f is Field && f.validationRules.Contains(ValidationRules.Required);

                DropDownList vddl = new DropDownList();
                vddl.DataSource = validationRules;
                vddl.DataBind();
                if (fk == null)
                    vddl.DataSource = validationRules;
                else
                    vddl.DataSource = requiredRule;
                validCell.Controls.Add(requiredlabel);
                validCell.Controls.Add(requiredCb);
                
                if (fk == null)
                    validCell.Controls.Add(vddl);


                if (f != null)
                {
                    foreach (ValidationRules rule in f.validationRules)
                    {
                        if (rule == ValidationRules.Required) continue;
                        else
                        {
                            vddl.Items.FindByText(rule.ToString()).Selected = true;
                            break;
                        }
                    }
                }

                r.Cells.Add(validCell);

                //...and the caption
                TableCell captionCell = new TableCell();
                TextBox caption = new TextBox();
                captionCell.Controls.Add(caption);
                r.Cells.Add(captionCell);

                if (f != null) {
                    caption.Text = f.caption;
                }
                
                tbl.Rows.Add(r);
            }

            
            // mappings will get a similiar table, but some collumns (like validation) will just be left empty
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

                TableCell displayCell = new TableCell();
                DropDownList displayDrop = new DropDownList();
                displayDrop.DataSource = mm.Stats.ColumnsToDisplay[mapping.refTable];
                displayDrop.DataBind();
                if (f != null) { 
                    displayDrop.SelectedIndex = displayDrop.Items.IndexOf(displayDrop.Items.FindByValue(f.Mapping.displayColumn));
                }
                displayCell.Controls.Add(displayDrop);
                r.Cells.Add(displayCell);

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

            // what can be done with the panel
            string[] actionTypes = new string[] { UserAction.Insert.ToString(), 
                UserAction.Update.ToString(), 
                UserAction.Delete.ToString() };                       // controls
            List<string> activeActions = (from _min.Models.Control control in actPanel.controls 
                                          select Enum.GetName(typeof(UserAction), control.action)).ToList<string>();

            actions.SetOptions(new List<string>(actionTypes));
            actions.SetIncludedOptions(activeActions);
            
            
            backButton.PostBackUrl = backButton.GetRouteUrl("ArchitectShowRoute", new { projectName = mm.ProjectName });
            
        }

        

        protected void SaveButton_Click(object sender, EventArgs e)
        {
            // extract the data for fields from the table
            List<Field> fields = new List<Field>();
            int i = 1;

            foreach (DataColumn col in mm.Stats.ColumnTypes[actPanel.tableName])
            {       // standard fields
                TableRow r = tbl.Rows[i++];
                if (!((CheckBox)r.Cells[1].Controls[0]).Checked)
                    continue;
                // label, present, type, valid, caption

                FieldTypes type = (FieldTypes)Enum.Parse(typeof(FieldTypes), 
                    ((DropDownList)r.Cells[2].Controls[0]).SelectedValue);

                // cell 3 is for FK display column dropList

                List<ValidationRules> rules = new List<ValidationRules>();
                CheckBox reqChb = (CheckBox)r.Cells[4].Controls[1];
                if(reqChb.Checked) rules.Add(ValidationRules.Required);
                if(r.Cells[4].Controls.Count == 3){
                    DropDownList ddl = (DropDownList)r.Cells[4].Controls[2];
                    if(ddl.SelectedValue != "")
                        rules.Add((ValidationRules)Enum.Parse(typeof(ValidationRules), ddl.SelectedValue));
                }
                
                string caption = ((TextBox)r.Cells[5].Controls[0]).Text;
                if (caption == "") caption = null;

                Field newField;
                if (type == FieldTypes.FK)
                {
                    FK fk = FKs.Find(x => x.myColumn == col.ColumnName);
                    fk.displayColumn = ((DropDownList)(r.Cells[3].Controls[0])).SelectedValue;
                    newField = new FKField(0, col.ColumnName, actPanel.panelId, FKs.Find(x => x.myColumn == col.ColumnName), caption);
                }
                else if (type == FieldTypes.Enum)
                {
                    newField = new EnumField(0, col.ColumnName, actPanel.panelId,
                        (List<string>)mm.Stats.ColumnTypes[actPanel.tableName][col.ColumnName].ExtendedProperties[CC.ENUM_COLUMN_VALUES],
                        caption);
                }
                else
                {
                    newField = new Field(0, col.ColumnName, type, actPanel.panelId, caption);
                }
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

                mapping.displayColumn = ((DropDownList)(r.Cells[3].Controls[0])).SelectedValue;

                string caption = ((TextBox)r.Cells[5].Controls[0]).Text;

                M2NMappingField m2nf = new M2NMappingField(0, null, 0, mapping, caption);
                fields.Add(m2nf);
            }

            // crate a control for each checked action
            List<_min.Models.Control> controls = new List<_min.Models.Control>();           // controls

            bool valid = true;
            List<string> errorMsgs = new List<string>();
            if (actions.RetrieveStringData().Count == 0) {
                valid = false;
                errorMsgs.Add("Choose at least one action for the panel, please.");
            }

            foreach (string actionString in actions.RetrieveStringData())
            {
                    _min.Models.Control c = new _min.Models.Control(0, actionString,
                        (UserAction)Enum.Parse(typeof(UserAction), actionString));
                    c.targetPanel = actPanel.controls[0].targetPanel;
                    
                    c.targetPanelId = actPanel.controls[0].targetPanelId;   // bad...really
                    
                    controls.Add(c);
            }

            MPanel resPanel = new MPanel(actPanel.tableName, actPanel.panelId, PanelTypes.Editable, new List<MPanel>(),
                fields, controls, actPanel.PKColNames, null, actPanel.parent);
            resPanel.panelName = panelName.Text;

            
            valid = valid && mm.Architect.checkPanelProposal(resPanel, out errorMsgs);
            
            // validate the Panel using Architect`s validator - don`t edit PKs, unique columns must have the constraint, must contain all collumns except Nullable
            // and AI and more rules
            validationResult.Items.Clear();
            if (valid)
            {
                validationResult.Items.Add(new ListItem("Valid"));

                actPanel = resPanel;
                mm.SysDriver.BeginTransaction();
                mm.SysDriver.UpdatePanel(actPanel);
                Session.Clear();
                mm.SysDriver.IncreaseVersionNumber();
                mm.SysDriver.CommitTransaction();
                
                validationResult.Items.Add(new ListItem("Saved"));
                Response.Redirect(Page.Request.RawUrl);
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