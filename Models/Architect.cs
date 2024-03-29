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
    /// <summary>
    /// creates initial proposal of administration interface based on the database charactersitics retriveved by StatsDriver and saves them using SystemDriver
    /// can also check correctness of a given proposal against the database
    /// </summary>
    public class Architect
    {
        private ISystemDriver systemDriver;
        private IStats stats;
        public List<M2NMapping> mappings;
        public List<string> hierarchies;
        public List<string> excludedTables = new List<string>();


        public Architect(ISystemDriver system, IStats stats)
        {
            this.stats = stats;
            this.systemDriver = system;
        }
        /// <summary>
        /// propose the editable panel for a table, if table will probably not be self-editable
        /// (such as an M2N mapping), return null
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns>Panel (or null)</returns>
        public Panel proposeForTable(string tableName)
        {
            // don`t care for indexes for now

            DataColumnCollection cols = stats.ColumnTypes[tableName];
            List<FK> FKs = stats.FKs[tableName];
            List<string> PKCols = stats.PKs[tableName];

            List<IField> fields = new List<IField>();
            

            foreach (M2NMapping mapping in mappings)    // find mappings related to this table
            {
                if (mapping.myTable != tableName) continue;
                List<string> displayColOrder = stats.ColumnsToDisplay[mapping.refTable];
                mapping.displayColumn = displayColOrder[0];
                fields.Add(new M2NMappingField(mapping, "Mapping " + mapping.myTable + " to " + mapping.refTable));
            }

            List<IColumnFieldFactory> factories = (List<IColumnFieldFactory>)(System.Web.HttpContext.Current.Application["ColumnFieldFactories"]);
            foreach (DataColumn col in cols) {
                if (col.AutoIncrement) continue;
                IColumnFieldFactory leadingFactory = (from f in factories where f.CanHandle(col) && !(f is ICustomizableColumnFieldFactory) 
                                                      orderby f.Specificity descending select f).FirstOrDefault();
                if (leadingFactory == null && !col.AllowDBNull)
                    return null;
                IColumnField field = leadingFactory.Create(col);
                field.Required = !col.AllowDBNull;
                field.Unique = col.Unique;
                fields.Add(field);
            }

            PropertyCollection controlProps = new PropertyCollection();
            PropertyCollection viewProps = new PropertyCollection();
            string panelName = tableName + " Editation";
            
            List<Control> controls = new List<Control>();

            // all the controls
            controls.Add(new Control(0, UserAction.Insert.ToString(), UserAction.Insert));
            controls.Add(new Control(0, UserAction.Update.ToString(), UserAction.Update));
            controls.Add(new Control(0, UserAction.Update.ToString(), UserAction.Delete));

            List<Control> controlsAsControl = new List<Control>(controls);

            Panel res = new Panel(tableName, 0, PanelTypes.Editable,
                new List<Panel>(), fields, controlsAsControl, PKCols);
            return res;
        }


        /// <summary>
        /// gets all FKs that refer back to their own table and filters those suitalbe for NavTree navigaion, otherwise adds an error message
        /// </summary>
        /// <returns>For each such FK Pair&lt;suitable(true/false), description/reason&gt;</returns>
        public Dictionary<string, KeyValuePair<bool, string>> CheckHierarchies()
        {
            List<FK> selfRefFK = stats.SelfRefFKs();
            Dictionary<string, List<string>> PKs = stats.PKs;
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
        public Panel proposeSummaryPanel(string tableName)
        {
            DataColumnCollection cols = stats.ColumnTypes[tableName];
            List<FK> FKs = stats.FKs[tableName];
            // a table with more than one self-referential FK is improbable
            List<FK> selfRefs = new List<FK>(from FK in FKs where FK.myTable == FK.refTable select FK as FK);
            List<string> PKCols = stats.PKs[tableName];
            FK selfRefFK = null;
            // strict hierarchy structure validation - not nice => no Tree
            if (hierarchies.Contains(tableName))
                selfRefFK = selfRefs.First();
            List<string> displayColOrder = stats.ColumnsToDisplay[tableName];
            
            Control control;
            DataTable controlTab = new DataTable();

            List<Control> Controls = new List<Control>();
            if (selfRefFK != null)  // this will be a NavTree
            {
                Panel res = new Panel(tableName, 0, PanelTypes.NavTree, new List<Panel>(), new List<IField>(),
                    new List<Control>(), PKCols);
                res.displayAccessRights = 1;
                control = new TreeControl(0, new HierarchyNavTable(), PKCols[0], selfRefFK.myColumn,
                     displayColOrder[0], new List<UserAction> { UserAction.View });
                Controls.Add(control);
                control = new Control(0, null, PKCols, UserAction.Insert);
                Controls.Add(control);
                
                res.AddControls(Controls);
                return res;
            }
            else
            {       // a simple NavTable
                Panel res = new Panel(tableName, 0, PanelTypes.NavTable, new List<Panel>(), new List<IField>(),
                    new List<Control>(), PKCols);
                res.displayAccessRights = 1;
                List<UserAction> actions = new List<UserAction>(new UserAction[] { UserAction.View, UserAction.Delete });
                List<string> displayColumns = displayColOrder.GetRange(0, Math.Min(displayColOrder.Count, 4));
                List<FK> neededFKs = (from FK fk in FKs where displayColumns.Contains(fk.myColumn) select fk).ToList();

                control = new NavTableControl(0, null, PKCols, neededFKs, actions);

                control.displayColumns = displayColumns;
                Controls.Add(control);
                control = new Control(0, null, PKCols, UserAction.Insert);
                Controls.Add(control);

                res.AddControls(Controls);
                return res;
            }
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
            systemDriver.BeginTransaction();

            List<string> tables = stats.Tables;
            List<Panel> baseChildren = new List<Panel>();

            HierarchyNavTable basePanelHierarchy = new HierarchyNavTable();
            foreach (string tableName in tables)
            {
                if (excludedTables.Contains(tableName)) continue;
                //Notice(this, new ArchitectNoticeEventArgs("Exploring table \"" + tableName + "\"..."));
                Panel editPanel = proposeForTable(tableName);
                if (editPanel != null)
                {      // editable panel available - add summary panel
                    Panel summaryPanel = proposeSummaryPanel(tableName);

                    foreach (Control c in summaryPanel.controls)        // simlified for now
                        c.targetPanel = editPanel;
                    foreach (Control c in editPanel.controls)
                        c.targetPanel = summaryPanel;

                    editPanel.panelName = "Editation of " + tableName;
                    summaryPanel.panelName = "Summary of " + tableName;
                    baseChildren.Add(editPanel);
                    baseChildren.Add(summaryPanel);

                    HierarchyRow tableRow = (HierarchyRow)basePanelHierarchy.NewRow();
                    HierarchyRow tableEditRow = (HierarchyRow)basePanelHierarchy.NewRow();
                    HierarchyRow tableSummaryRow = (HierarchyRow)basePanelHierarchy.NewRow();
                    tableRow.ParentId = null;
                    tableSummaryRow.ParentId = tableRow.Id;
                    tableEditRow.ParentId = tableRow.Id;
                    tableRow.NavId = null;
                    
                    tableRow.Caption = tableName;
                    tableEditRow.Caption = "Add";
                    tableSummaryRow.Caption = "Browse";

                    basePanelHierarchy.Add(tableRow);
                    basePanelHierarchy.Add(tableEditRow);
                    basePanelHierarchy.Add(tableSummaryRow);
                }
            }
            
            Panel basePanel = new Panel(null, 0, PanelTypes.MenuDrop,
                baseChildren, null, null, null);
            basePanel.panelName = "Main page";
            basePanel.isBaseNavPanel = true;
            systemDriver.AddPanel(basePanel);

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

            
            List<Control> addedList = new List<Control>();
            addedList.Add(basePanelTreeControl);
            basePanel.AddControls(addedList);
            systemDriver.AddControl(basePanelTreeControl);

            systemDriver.IncreaseVersionNumber();
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
        /// <returns>true if no errors were found, true otherwise</returns>
        public bool checkPanelProposal(Panel proposalPanel, out List<string> errorMsgs, Dictionary<DataColumn, Dictionary<string, object>> customs)
        // non-recursive checking after the initial check - after panel modification
        {
            errorMsgs = new List<string>();

            DataColumnCollection cols = stats.ColumnTypes[proposalPanel.tableName];

            List<FK> FKs = stats.FKs[proposalPanel.tableName];
            List<M2NMapping> mappings = stats.Mappings[proposalPanel.tableName];

            bool good = true;
            
            if (proposalPanel.type == PanelTypes.Editable)
            // this is indeed the only panelType containing fields => only editable
            {
                foreach (IField field in proposalPanel.fields)
                {
                    if(!(field is IColumnField)) continue;
                    IColumnField cf = (IColumnField)field;
                    string messageBeginning = "Column " + cf.ColumnName + " managed by field " + cf.Caption + " ";
                    if (!(cols.Contains(cf.ColumnName)))
                    {
                        errorMsgs.Add(messageBeginning + "does not exist in table");
                        good = false;
                    }
                    else
                    {
                        if (!(cf is FKField))     // NavTable won`t be edited in the panel
                        {
                            if (cols[cf.ColumnName].AllowDBNull == false &&
                                !cols[cf.ColumnName].AutoIncrement &&
                                !(cf is CheckboxField)&&
                                !cf.Required)
                            {
                                errorMsgs.Add(messageBeginning
                                    + "cannot be set to null, but the coresponding field is not required");
                                good = false;
                            }


                            /*
                            if ((r.Contains(ValidationRules.Date) || r.Contains(ValidationRules.DateTime))
                                // TODO do not handle these MySQL issues in here
                                && !(cols[field.column].DataType == typeof(DateTime) || cols[field.column].DataType == typeof(MySql.Data.Types.MySqlDateTime)))
                            {
                                errorMsgs.Add(messageBeginning +
                                    "is not a date / datetime, thus cannot be edited as a date");
                                good = false;
                            }
                            */
                            /*
                            DataColumn fieldColumn = cols[field.Column];
                            if (field.type != FieldTypes.ShortText)
                            {
                                if (field.type == FieldTypes.Text && fieldColumn.DataType != typeof(string))
                                {
                                    errorMsgs.Add(messageBeginning + "- only text columns can be edited in a text editor");
                                    good = false;
                                }
                            }
                            if (field.caption == null || field.caption == "")
                            {
                                errorMsgs.Add(messageBeginning + " must have a caption");
                                good = false;
                            }
                            if ((cols[field.Column].DataType == typeof(int)
                                || cols[field.Column].DataType == typeof(long)
                                || cols[field.Column].DataType == typeof(short)) && !field.validationRules.Contains(ValidationRules.Ordinal))
                            {
                                errorMsgs.Add(messageBeginning + " is of type " + cols[field.Column].DataType.ToString() +
                                    ", but is not restrained to ordinal values by a validation rule");
                                good = false;
                            }

                            if ((cols[field.Column].DataType == typeof(decimal)
                                 || cols[field.Column].DataType == typeof(double)
                                || cols[field.Column].DataType == typeof(float)) && !field.validationRules.Contains(ValidationRules.Decimal))
                            {
                                errorMsgs.Add(messageBeginning + " is of type " + cols[field.Column].DataType.ToString() +
                                    ", but is not restrained to numeric values by a validation rule");
                                good = false;
                            }
                            if (cols[field.Column].Unique && !field.validationRules.Contains(ValidationRules.Unique)) {
                                errorMsgs.Add(messageBeginning + " is constrained to be unique, but the corresponding field does not have "
                                    + "the \"Unique\" validation rule set.");
                                good = false;
                            }
                             */ 
                        }
                    }
                }
                IEnumerable<string> requiredColsMissing = from DataColumn col in stats.ColumnTypes[proposalPanel.tableName]
                                                          where col.AllowDBNull == false && col.DefaultValue == null &&
                                                          !proposalPanel.fields.Exists(x => x is IColumnField && ((IColumnField)x).ColumnName == col.ColumnName)
                                                          && (!customs.ContainsKey(col) || (bool)customs[col]["required"] == false)
                                                          select col.ColumnName;
                // if the oclum is contained in customs, only the custom factory validation will be left so this validation does not need to be called
                // again

                foreach (string missingCol in requiredColsMissing)
                {
                    good = false;
                    errorMsgs.Add("Column " + missingCol + " cannot be NULL and has no default value." +
                        " It must therefore be included in the panel");
                }
                if (proposalPanel.panelName == "")
                {
                    errorMsgs.Add("Does this panel not deserve a name?");
                    good = false;
                }

                if (proposalPanel.controls.Count == 0)
                {
                    errorMsgs.Add("A panel with no controls would be useless...");
                }
            }
            else throw new Exception("Validation-non editable panel as an editable one.");
            
            
            return good;
        }

    }

}