﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using _min.Interfaces;
using _min.Models;
using _min.Common;
using CC = _min.Common.Constants;
using CE = _min.Common.Environment;

namespace _min.Models
{
    public class ArchitectQuestionEventArgs : EventArgs
    {
        public string questionText { get; private set; }
        public Dictionary<string, object> options { get; private set; }

        public ArchitectQuestionEventArgs(string qText, Dictionary<string, object> options)
        {
            questionText = qText;
            this.options = options;
        }
    }

    public delegate void ArchitectQuestion(Architect sender, ArchitectQuestionEventArgs e);

    public class ArchitectureErrorEventArgs : EventArgs
    {
        public string message { get; private set; }
        public string tableName { get; private set; }
        public Panel panel { get; private set; }
        public Field field { get; private set; }
        public Control control { get; set; }

        public ArchitectureErrorEventArgs(string message, string tableName)
        {
            this.message = message;
            this.tableName = tableName;
            this.panel = null;
            this.field = null;
            this.control = null;
        }

        public ArchitectureErrorEventArgs(string message, Panel panel, Field field)
            : this(message, panel.tableName)
        {
            this.panel = panel;
            this.field = field;
        }

        public ArchitectureErrorEventArgs(string message, Panel panel, Control control)
            : this(message, panel.tableName)
        {
            this.panel = panel;
            this.control = control;
        }
    }

    public delegate void ArchitectureError(Architect sender, ArchitectureErrorEventArgs e);


    public class ArchitectWarningEventArgs : ArchitectureErrorEventArgs
    {
        public ArchitectWarningEventArgs(string message, string tableName)
            : base(message, tableName)
        { }
    }

    public delegate void ArchitectWarning(Architect sender, ArchitectWarningEventArgs e);


    public class ArchitectNoticeEventArgs : ArchitectureErrorEventArgs
    {
        public ArchitectNoticeEventArgs(string message, string tableName = null)
            : base(message, tableName)
        { }
    }

    public delegate void ArchitectNotice(Architect sender, ArchitectNoticeEventArgs e);


    public delegate Panel AsyncProposeCaller();

    public class Architect
    {
        public class ColumnDisplayComparer : IComparer<DataColumn>
        {

            public int Compare(DataColumn x, DataColumn y)
            {
                if (x == y || x.DataType == y.DataType) return 0;
                if (x == null) return 1;
                if (y == null) return -1;
                if (x.DataType == typeof(string) && y.DataType == typeof(string))
                    return y.MaxLength - x.MaxLength;
                if (x.DataType == typeof(string)) return -1;
                if (y.DataType == typeof(string)) return 1;
                if (x.DataType == typeof(DateTime)) return -1;
                if (x.DataType == typeof(DateTime)) return 1;
                if (x.DataType == typeof(int)) return -1;
                if (y.DataType == typeof(int)) return 1;
                return 0;
            }
        }

        public object questionAnswer;
        public event ArchitectQuestion Question;
        public event ArchitectureError Error;
        public event ArchitectWarning Warning;
        public event ArchitectNotice Notice;

        private ISystemDriver systemDriver;
        private IStats stats;
        public Dictionary<string, M2NMapping> mappings;
        public List<string> hierarchies;


        public Architect(ISystemDriver system, IStats stats)
        {
            this.stats = stats;
            this.systemDriver = system;
            questionAnswer = null;
        }

        private List<string> DisplayColOrder(string tableName)
        // the order in which columns will be displayed in summary tables & M2NMapping, covers all columns
        {
            DataColumnCollection cols = stats.columnTypes(tableName);

            List<string> res = new List<string>();
            List<DataColumn> colList = new List<DataColumn>(from DataColumn col in cols select col);
            ColumnDisplayComparer comparer = new ColumnDisplayComparer();
            colList.Sort(comparer);
            return new List<string>(from col in colList select col.ColumnName);
        }

        public Panel getArchitectureInPanel()
        {
            return systemDriver.getArchitectureInPanel();
        }
        /// <summary>
        /// propose the editable panel for a table, if table will probably not be self-editable
        /// (such as an M2N mapping), return null
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns>Panel (or null)</returns>
        public Panel proposeForTable(string tableName)
        {
            // dont care for indexes for now

            DataColumnCollection cols = stats.columnTypes(tableName);
            List<FK> FKs = stats.foreignKeys(tableName);
            List<string> PKCols = stats.primaryKeyCols(tableName);

            if (cols.Count == 2 && FKs.Count == 2) return null; // seems like mapping table
            // FK ~> mapping ?
            List<Field> fields = new List<Field>();
            List<ValidationRules> validation = new List<ValidationRules>();
            if (mappings.ContainsKey(tableName))
            {
                M2NMapping mapping = mappings[tableName];
                // no potentional field from cols is removed by this, though
                List<string> displayColOrder = DisplayColOrder(mapping.refTable);
                mapping.displayColumn = displayColOrder[0];
                fields.Add(new M2NMappingField(0, mapping.myColumn, 0, mapping));

            }

            // standard FKs
            foreach (FK actFK in FKs)
            {
                validation = new List<ValidationRules>();
                //PropertyCollection validation = new PropertyCollection();
                if (!cols[actFK.myColumn].AllowDBNull) validation.Add(ValidationRules.Required);
                List<string> displayColOrder = DisplayColOrder(actFK.refTable);
                actFK.displayColumn = displayColOrder[0];
                fields.Add(new FKField(0, actFK.myColumn, 0, actFK));

                cols.Remove(actFK.myColumn);    // will be edited as a foreign key

            }

            // editable fields in the order as defined in table; don`t edit AI
            foreach (DataColumn col in cols)
            {
                //PropertyCollection validation = new PropertyCollection();

                validation = new List<ValidationRules>();
                if (!col.ExtendedProperties.ContainsKey(CC.COLUMN_EDITABLE)) continue;
                if (!col.AllowDBNull) validation.Add(ValidationRules.Required);
                FieldTypes fieldType;  // default => standard textBox

                if (col.DataType == typeof(string))
                {
                    if (col.MaxLength <= 255) fieldType = FieldTypes.Varchar;
                    else fieldType = FieldTypes.Text;
                }
                else if (col.DataType == typeof(int) || col.DataType == typeof(long) || col.DataType == typeof(short))
                {
                    fieldType = FieldTypes.Ordinal;
                    validation.Add(ValidationRules.Ordinal);
                }
                else if (col.DataType == typeof(float) || col.DataType == typeof(double))
                {
                    fieldType = FieldTypes.Decimal;
                    validation.Add(ValidationRules.Decimal);
                }
                else if (col.DataType == typeof(bool))
                {
                    fieldType = FieldTypes.Bool;
                }
                else if (col.DataType == typeof(DateTime))
                {
                    if (col.ExtendedProperties.ContainsKey(CC.FIELD_DATE_ONLY))
                        fieldType = FieldTypes.Date;
                    // should DATETIME, BUT DATETIME is usually used for date only...or is it?
                    else fieldType = FieldTypes.Date;
                    validation.Add(ValidationRules.Date);
                }
                else if (col.DataType == typeof(Enum))
                {
                    // cannot happen, since column properties are taken from Stats
                    if (!col.ExtendedProperties.ContainsKey(CC.COLUMN_ENUM_VALUES))
                        throw new Exception("Missing enum options for field " + col.ColumnName);
                    fieldType = FieldTypes.Enum;
                }
                else
                {
                    throw new Exception("Unrecognised column type " + col.DataType.ToString());
                }
                fields.Add(new Field(0, col.ColumnName, fieldType,
                    0));  // don`t add any properties, just copy from Stat
            }
            fields.OrderBy(x => ((int)(x.position)));
            // setup controls as properies
            PropertyCollection controlProps = new PropertyCollection();
            PropertyCollection viewProps = new PropertyCollection();
            string panelName = tableName + " Editation";
            //viewProps.Add(CC.PANEL_NAME, tableName + " Editation");

            List<Control> controls = new List<Control>();

            /*
            string actionName = UserAction.View.ToString();
            controlProps.Add(actionName, actionName);
            controlProps.Add(actionName + CC.CONTROL_ACCESS_LEVEL_REQUIRED_SUFFIX, 1);

            actionName = UserAction.Insert.ToString();
            controlProps.Add(actionName, actionName);
            controlProps.Add(actionName + CC.CONTROL_ACCESS_LEVEL_REQUIRED_SUFFIX, 3);
            
            actionName = UserAction.Update.ToString();
            controlProps.Add(actionName, actionName);
            controlProps.Add(actionName + CC.CONTROL_ACCESS_LEVEL_REQUIRED_SUFFIX, 5);

            actionName = UserAction.Delete.ToString();
            controlProps.Add(actionName, actionName);
            controlProps.Add(actionName + CC.CONTROL_ACCESS_LEVEL_REQUIRED_SUFFIX, 5);
            */
            controls.Add(new Control(0, UserAction.Insert.ToString(), UserAction.Insert));
            controls.Add(new Control(0, UserAction.Update.ToString(), UserAction.Update));
            /*
            foreach (string actName in Enum.GetNames(typeof(UserAction)))
            {
                controls.Add(new Control(0, actName, (UserAction)Enum.Parse(typeof(UserAction), actName)));
            }*/
            List<Control> controlsAsControl = new List<Control>(controls);

            //set additional properties
            // the order of fields in edit form (if defined), if doesn`t cover all editable columns, display the remaining
            // at the begining, non-editable columns are skipped
            //viewProps[CC.PANEL_DISPLAY_COLUMN_ORDER] = String.Join(",", DisplayColOrder(tableName));

            Panel res = new Panel(tableName, 0, PanelTypes.Editable,
                new List<Panel>(), fields, controlsAsControl, PKCols);
            return res;
        }



        public Dictionary<string, KeyValuePair<bool, string>> CheckHierarchies()
        {
            List<FK> selfRefFK = stats.selfRefFKs();
            Dictionary<string, List<string>> PKs = stats.GlobalPKs();
            var tableSRFKs = from fk in selfRefFK group fk by fk.myTable into groups select new { Key = groups.Key, Value = groups };
            Dictionary<string, KeyValuePair<bool, string>> res = new Dictionary<string, KeyValuePair<bool, string>>();

            foreach (var x in tableSRFKs)
            {
                if (PKs[x.Key].Count != 1)
                    res[x.Key] = new KeyValuePair<bool, string>(false, "This table does not have a single-column PK.");
                else if (x.Value.Count() > 1)
                    res[x.Key] = new KeyValuePair<bool, string>(false, "Multiple self-referential columns?");
                else if (x.Value.First().refColumn != PKs[x.Key][0])
                    res[x.Key] = new KeyValuePair<bool, string>(false, "The self-referential column must refer to the PK.");
                else
                {
                    FK goodSRFK = x.Value.First();
                    res[x.Key] = new KeyValuePair<bool, string>(true, goodSRFK.myColumn + " refers to " + goodSRFK.refColumn +
                        " this table can be navigated through via a trre");
                }
            }
            return res;
        }

        /// <summary>
        /// Ignores mappings, displays first 4 columns in a classic NavTable 
        /// or the first display column in a NavTree if a self-referential FK is present.
        /// navtable includes edit and delete action buttons, second control - insert button
        /// </summary>
        /// <param name="tableName">string</param>
        /// <returns>Panel</returns>
        private Panel proposeSummaryPanel(string tableName)
        {
            DataColumnCollection cols = stats.columnTypes(tableName);
            List<FK> FKs = stats.foreignKeys(tableName);
            // a table with more than one self-referential FK is improbable
            List<FK> selfRefs = new List<FK>(from FK in FKs where FK.myTable == FK.refTable select FK as FK);
            List<string> PKCols = stats.primaryKeyCols(tableName);
            FK selfRefFK = null;
            // strict hierarchy structure validation - not nice => no Tree
            if (hierarchies.Contains(tableName))
                selfRefFK = selfRefs.First();
            List<string> displayColOrder = DisplayColOrder(tableName);
            /*
            PropertyCollection controlProps = new PropertyCollection();
            PropertyCollection displayProps = new PropertyCollection();
            */

            Panel res = new Panel(tableName, 0, PanelTypes.MenuDrop, new List<Panel>(), new List<Field>(),
                new List<Control>(), displayColOrder);
            res.displayAccessRights = 1;
            Control control;
            DataTable controlTab = new DataTable();

            List<Field> fields = new List<Field>();
            List<Control> Controls = new List<Control>();
            if (selfRefFK != null)
            {
                control = new TreeControl(0, new HierarchyNavTable(), PKCols[0], selfRefFK.refColumn, "Update", UserAction.Update);
                Controls.Add(control);
                control = new TreeControl(0, new HierarchyNavTable(), PKCols[0], selfRefFK.refColumn, "Delete", UserAction.Delete);
                control.independent = false;    // linkbuttons together with Update
                Controls.Add(control);
                /*
                controlProps.Add(CC.CONTROL_HIERARCHY_SELF_FK_COL, selfRefFK.myColumn);
                controlTab.Columns.Add(PKCols[0]);
                controlTab.Columns.Add(selfRefFK.myColumn);
                controlTab.Columns.Add(displayColOrder[0]);
                control = new TreeControl(controlTab, PKCols[0], selfRefFK.myColumn, displayColOrder[0], UserAction.Update);
                */
                res.type = PanelTypes.NavTree;
            }
            res.AddControls(Controls);
            res.AddFields(fields);
            return res;
        }



        /// <summary>
        /// get both edit and summary panel proposal for editable tables 
        /// and create base Panel with MenuDrop field for each editable table
        /// with 2 children pointing to insert action and summary table view;
        /// also saves it to systemDB, because it needs the panelIDs to 
        /// build navigation upon them.
        /// The proposal should always pass proposal check.
        /// 
        /// Presenter must have sent a request banning others from initiating a proposal
        /// the whole proposal happens in a transaction
        /// </summary>
        /// <returns>Panel</returns>
        public Panel propose()
        {
            //Notice(this, new ArchitectNoticeEventArgs("Starting proposal..."));


            systemDriver.StartTransaction();

            List<string> tables = stats.TableList();
            List<Panel> baseChildren = new List<Panel>();

            HierarchyNavTable basePanelHierarchy = new HierarchyNavTable();
            foreach (string tableName in tables)
            {
                //Notice(this, new ArchitectNoticeEventArgs("Exploring table \"" + tableName + "\"..."));
                Panel editPanel = proposeForTable(tableName);
                if (editPanel != null)
                {      // editable panel available - add summary panel
                    //Notice(this, new ArchitectNoticeEventArgs("Table \"" + tableName + "\" will be editable."));
                    Panel summaryPanel = proposeSummaryPanel(tableName);

                    foreach (Control c in summaryPanel.controls)        // simlified for now
                        c.targetPanel = editPanel;
                    foreach (Control c in editPanel.controls)
                        c.targetPanel = summaryPanel;

                    //Notice(this, new ArchitectNoticeEventArgs("Proposed summary navigation panel for table \"" + tableName + "\"."));
                    baseChildren.Add(editPanel);
                    baseChildren.Add(summaryPanel);

                    HierarchyRow tableRow = (HierarchyRow)basePanelHierarchy.NewRow();
                    HierarchyRow tableEditRow = (HierarchyRow)basePanelHierarchy.NewRow();
                    HierarchyRow tableSummaryRow = (HierarchyRow)basePanelHierarchy.NewRow();
                    tableRow.ParentId = 0;
                    tableSummaryRow.ParentId = tableRow.Id;
                    tableEditRow.ParentId = tableRow.Id;
                    tableRow.NavId = 0;
                    //tableSummaryRow.NavId = summaryPanel.panelId;
                    //tableEditRow.NavId = editPanel.panelId;
                    tableRow.Caption = tableName;
                    tableEditRow.Caption = "Add";
                    tableSummaryRow.Caption = "Browse";

                    basePanelHierarchy.Add(tableRow);
                    basePanelHierarchy.Add(tableEditRow);
                    basePanelHierarchy.Add(tableSummaryRow);
                }
                else
                {
                    //Notice(this, new ArchitectNoticeEventArgs("Table \"" + tableName + "\" is probably NOT suitable for direct management."));
                }
            }
            //Notice(this, new ArchitectNoticeEventArgs("Creating navigation base with " +
            //    baseChildren.Count + " options (2 per table)."));

            Panel basePanel = new Panel(null, 0, PanelTypes.MenuDrop,
                baseChildren, null, null, null);
            basePanel.isBaseNavPanel = true;
            //systemDriver.query("TRUNCATE TABLE log_db");
            //Notice(this, new ArchitectNoticeEventArgs("Updating database..."));
            systemDriver.AddPanel(basePanel);

            SetControlPanelBindings(basePanel);

            TreeControl basePanelTreeControl = new TreeControl(basePanel.panelId, basePanelHierarchy,
                "Id", "ParentId", "Caption", UserAction.View);
            basePanelTreeControl.navThroughPanels = true;

            // navId for menu items
            for (int i = 0; i < basePanel.children.Count / 2; i++) // ad hoc beyond sense
            {
                basePanelHierarchy.Rows[i * 3]["NavId"] = basePanel.children[2 * i].panelId;
                basePanelHierarchy.Rows[i * 3 + 1]["NavId"] = basePanel.children[2 * i].panelId;
                basePanelHierarchy.Rows[i * 3 + 2]["NavId"] = basePanel.children[2 * i + 1].panelId;
            } 

            //AddControl! only.
            systemDriver.AddControl(basePanelTreeControl);
            systemDriver.RewriteControlDefinitions(basePanel);

            // now children have everything set, even parentid 
            // as the parent was inserted first, his id was set and they took it from the object

            List<Control> addedList = new List<Control>();
            addedList.Add(basePanelTreeControl);
            basePanel.AddControls(addedList);



            //systemDriver.updatePanel(basePanel, false); 
            // sholuld no longer be neccessay

            systemDriver.CommitTransaction();
            return basePanel;
        }

        /// <summary>
        /// checks if edited fields exist in webDB and their types are adequate, checks constraints on FKs and mapping tables,
        /// also checks if controls` dataTables` columns exist and everything that has to be inserted in DB is a required field,
        /// whether attributes don`t colide and every panel has something to display.
        /// As it goes through the Panel, it fires an ArchitectureError event.
        /// </summary>
        /// <param name="proposalPanel"></param>
        /// <param name="recursive">run itself on panel children</param>
        /// <returns>true if no errors found, true othervise</returns>
        public bool checkPanelProposal(Panel proposalPanel, bool recursive = true)
        // non-recursive checking after the initial check - after panel modification
        {
            string messageBeginning = "In panel " + proposalPanel.panelName +
                "of type " + proposalPanel.type + " for " + proposalPanel.tableName + ": ";

            DataColumnCollection cols = stats.columnTypes(proposalPanel.tableName);
            if (cols.Count == 0 && !proposalPanel.isBaseNavPanel)
            {
                Error(this, new ArchitectureErrorEventArgs(messageBeginning + "table not found or has 0 columns",
                    proposalPanel.tableName));
                return false;
            }

            List<FK> FKs = stats.foreignKeys(proposalPanel.tableName);

            bool good = true;
            if (proposalPanel.type == PanelTypes.Editable
                || proposalPanel.type == PanelTypes.NavTable)
            // this is indeed the only panelType containing fields
            {
                bool isNavTable = proposalPanel.type == PanelTypes.NavTable;
                foreach (Field field in proposalPanel.fields)
                {
                    if (field.type == FieldTypes.Holder)
                        continue;
                    if (!cols.Contains(field.column))
                    {
                        Error(this, new ArchitectureErrorEventArgs(messageBeginning + "the column " + field.column +
                            "managed by the field does not exist in table", proposalPanel, field));
                        good = false;
                    }
                    else
                    {
                        if (!(field is FKField) && !(field is M2NMappingField) && !isNavTable)     // NavTable won`t be edited in the panel
                        {
                            List<ValidationRules> r = field.validationRules;
                            if (cols[field.column].AllowDBNull == false && !r.Contains(ValidationRules.Required))
                            {
                                Error(this, new ArchitectureErrorEventArgs(messageBeginning + "the column " + field.column
                                    + " cannot be set to null, but the coresponding field is not required", proposalPanel, field));
                                good = false;
                            }

                            if ((r.Contains(ValidationRules.Decimal) || r.Contains(ValidationRules.Ordinal))
                                && !(typeof(long).IsAssignableFrom(cols[field.column].DataType)))
                            {
                                Error(this, new ArchitectureErrorEventArgs(messageBeginning + "the column " + field.column
                                + " is of type " + cols[field.column].DataType.ToString()
                                + ", thus cannot be edited as a decimalnumber", proposalPanel, field));
                                good = false;
                            }

                            if ((r.Contains(ValidationRules.Date) || r.Contains(ValidationRules.DateTime))
                                && !(cols[field.column].DataType == typeof(DateTime)))
                            {
                                Error(this, new ArchitectureErrorEventArgs(messageBeginning + "the column " + field.column
                                + " is not a date / datetime, thus cannot be edited as a date", proposalPanel, field));
                                good = false;
                            }
                        }
                        else if (field is M2NMappingField)
                        {
                            // just cannot occur in a NavTable, but just in case...
                            if (isNavTable) throw new Exception("Cannot display a M2NMapping in NavTable");
                            M2NMapping thisMapping = ((M2NMappingField)field).mapping;
                            if (!mappings.Values.Contains(thisMapping))
                            {
                                Error(this, new ArchitectureErrorEventArgs(messageBeginning + "the schema " +
                                    "does not define an usual M2NMapping batween tables " + thisMapping.myTable +
                                    " and " + thisMapping.refTable + " using " + thisMapping.mapTable +
                                    " as a map table", proposalPanel, field));
                                good = false;
                            }
                        }
                        else if (field is FKField)
                        {
                            FK fieldFK = ((FKField)field).fk;
                            if (!FKs.Contains(fieldFK))
                            {
                                Error(this, new ArchitectureErrorEventArgs(messageBeginning + "the column " + field.column
                                + " is not a foreign key representable by the FK field", proposalPanel, field));
                                good = false;
                            }
                        }
                    }
                }
            }

            // controls...

            // not ideal
            if (!proposalPanel.isBaseNavPanel && proposalPanel.controls.Any(x => x.targetPanelId == null || x.targetPanel == null))
            {
                Error(this, new ArchitectureErrorEventArgs(messageBeginning + "Each control must have a target panel set",
                    proposalPanel, proposalPanel.controls[0]));
                good = false;
            }

            if (proposalPanel.type == PanelTypes.NavTable
                || proposalPanel.type == PanelTypes.NavTree
                || proposalPanel.type == PanelTypes.MenuDrop)
            {
                if (proposalPanel.controls.Count == 0)
                {
                    Error(this, new ArchitectureErrorEventArgs(messageBeginning + "navTables, navTrees and drop menus"
                   + "must have at least one control", proposalPanel.tableName));
                }
                else
                {
                    if (proposalPanel.isBaseNavPanel)
                    {
                        if (proposalPanel.tableName != null)
                        {
                            Error(this, new ArchitectureErrorEventArgs(messageBeginning + "Panel that navigates through panels "
                                + "cannot have tableName set", proposalPanel.tableName));
                            good = false;
                        }
                    }
                }
            }

            // TODO & TODO & TODO (CONTROLS & OTHER PROPERTIES)
            // OR allow the admin-user take valid steps only (?)

            if (recursive && proposalPanel.children != null) foreach (Panel child in proposalPanel.children)
                {
                    good = good && checkPanelProposal(child, true);
                }
            return good;
        }

        public bool checkPanelProposal(int panelId, bool recursive = true)  // on first load / reload request; also in production
        {
            Panel proposalPanel = systemDriver.getPanel(panelId, true);
            return checkPanelProposal(proposalPanel, true);
        }

        public bool checkProposal()
        {
            Panel proposalPanel = systemDriver.getArchitectureInPanel();
            return checkPanelProposal(proposalPanel, true);
        }

        private void SetControlPanelBindings(Panel panel, bool recursive = true)
        {
            foreach (Control c in panel.controls)
            {
                if (c.targetPanelId != null)
                    throw new Exception("Target panel id already set!");
                if (c.targetPanel != null)
                {
                    c.targetPanelId = c.targetPanel.panelId;
                }
            }
            if (recursive)
                foreach (Panel p in panel.children)
                    SetControlPanelBindings(p);
        }

    }

}