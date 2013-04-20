﻿using System;
using System.Collections.Generic;
using System.Linq;
using _min.Interfaces;
using System.Data;
using System.IO;
using _min.Common;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Web.Security;
using CC = _min.Common.Constants;
using CE = _min.Common.Environment;


namespace _min.Models
{
    /// <summary>
    /// Manages project architecture storage / retrieval, but does not maintain project versioning
    /// </summary>
    class SystemDriverMySql : BaseDriverMySql, ISystemDriver
    {
        private DbDeployableMySql dbe = new DbDeployableMySql();
        public Panel MainPanel { get; private set; }
        public Dictionary<int, Panel> Panels { get; private set; }

        
        public SystemDriverMySql(string connstring, DataTable logTable = null, bool writeLog = false)
            : base(connstring, logTable, writeLog)
        {
            Panels = new Dictionary<int, Panel>();
        }


        public void SetArchitecture(Panel mainPanel) {
            MainPanel = mainPanel;
            FlattenPanel(MainPanel);
        }
        
        // use only through FullProject load, if there is no panel in session or project version has changed
        #region Deserialization

        /// <summary>
        /// loads the whole project from database (in 3 queries)
        /// </summary>
        public void FullProjectLoad() {
            BeginTransaction();
            FK control_panel = new FK("controls", "id_panel", "panels", "id_panel", null);
            FK field_panel = new FK("fields", "id_panel", "panels", "id_panel", null);
            DataTable panels = fetchAll("SELECT * FROM ", dbe.Table("panels"), "WHERE id_project =", CE.project.Id);
            DataTable controls = fetchAll("SELECT controls.* FROM ", dbe.Table("controls"), dbe.Join(control_panel), "WHERE id_project =", CE.project.Id);
            DataTable fields = fetchAll("SELECT fields.* FROM ", dbe.Table("fields"), dbe.Join(field_panel), "WHERE id_project =", CE.project.Id);
            CommitTransaction();
            panels.TableName = "panels";
            controls.TableName = "controls";
            fields.TableName = "fields";
            DataSet ds = new DataSet();
            
            panels.PrimaryKey = new DataColumn[] { panels.Columns["id_panel"] };
            controls.PrimaryKey = new DataColumn[] { panels.Columns["id_control"] };
            fields.PrimaryKey = new DataColumn[] { panels.Columns["id_field"] };

            ds.Tables.Add(panels);
            ds.Tables.Add(controls);
            ds.Tables.Add(fields);
            
            ds.Relations.Add(new DataRelation(CC.SYSDRIVER_FK_CONTROL_PANEL, ds.Tables["panels"].Columns["id_panel"], ds.Tables["controls"].Columns["id_panel"], true));
            ds.Relations.Add(new DataRelation(CC.SYSDRIVER_FK_FIELD_PANEL, ds.Tables["panels"].Columns["id_panel"], ds.Tables["fields"].Columns["id_panel"], true));
            ds.Relations.Add(new DataRelation(CC.SYSDRIVER_FK_PANEL_PARENT, ds.Tables["panels"].Columns["id_panel"], ds.Tables["panels"].Columns["id_parent"], true));
            DataSet2Architecture(ds);
        }

        /// <summary>
        /// deserializes the dataset into MainPanel and Panels
        /// </summary>
        /// <param name="ds">the dataset from FullProjectLoad</param>
        private void DataSet2Architecture(DataSet ds) {
            DataRow[] rootRows = ds.Tables["panels"].Select("id_parent IS NULL");
            if (rootRows.Length != 1) throw new DataException("There must be one and only one root panel for each project");
            MainPanel = DataSet2Panel(ds, (int)(rootRows[0]["id_panel"]));
            FlattenPanel(MainPanel);
        }

        /// <summary>
        /// deserializes a specific panel with controls, fields and child panels
        /// </summary>
        /// <param name="ds">the dataset from FullProjectLoad</param>
        private Panel DataSet2Panel(DataSet ds, int panelId) {
            List<Field> fields = DataSet2PanelFields(ds, panelId);
            List<Control> controls = DataSet2PanelControls(ds, panelId);
            DataRow panelRow = ds.Tables["panels"].Rows.Find(panelId);
            Panel res = DeserializePanel(panelRow["content"] as string);
            res.panelId = panelId;

            List<Panel> children = new List<Panel>();
            DataRow[] childRows = panelRow.GetChildRows(CC.SYSDRIVER_FK_PANEL_PARENT);
            foreach (DataRow childRow in childRows) { 
                children.Add(DataSet2Panel(ds, (int)(childRow["id_panel"])));
            }

            res.AddChildren(children);
            res.AddFields(fields);
            res.AddControls(controls);
            return res;
        }

        /// <summary>
        /// deserializes a panel`s controls
        /// </summary>
        /// <param name="ds">the dataset from FullProjectLoad</param>
        private List<Control> DataSet2PanelControls(DataSet ds, int panelId) {
            DataRow panelRow = ds.Tables["panels"].Rows.Find(panelId);
            DataRow[] controlRows = panelRow.GetChildRows(CC.SYSDRIVER_FK_CONTROL_PANEL);
            List<Control> res = new List<Control>();
            Control c;
            foreach (DataRow controlRow in controlRows) {
                c = DeserializeControl(controlRow["content"] as string);
                c.controlId = (int)(controlRow["id_control"]);
                res.Add(c);
            }
            return res;
        }

        /// <summary>
        /// deserializes a panel`s fields
        /// </summary>
        /// <param name="ds">the dataset from FullProjectLoad</param>
        private List<Field> DataSet2PanelFields(DataSet ds, int panelId) {
            DataRow panelRow = ds.Tables["panels"].Rows.Find(panelId);
            DataRow[] fieldRows = panelRow.GetChildRows(CC.SYSDRIVER_FK_FIELD_PANEL);
            List<Field> res = new List<Field>();
            Field f;
            foreach (DataRow fieldRow in fieldRows)
            {
                f = DeserializeField(fieldRow["content"] as string);
                f.fieldId = (int)(fieldRow["id_field"]);
                res.Add(f);
            }
            return res;
        }

        private Panel DeserializePanel(string s){
            DataContractSerializer serializer = new DataContractSerializer(typeof(Panel));
            Panel result = ((Panel)(serializer.ReadObject(Functions.GenerateStreamFromString(s))));
            result.InitAfterDeserialization();
            return result;
        }

        /// <summary>
        /// deserializes a Control, distinguishing TreeControls and creating hierarchy Relations in their sored DataTables
        /// </summary>
        /// <param name="s">serialized control</param>
        /// <returns>the deserialized Control object</returns>
        private Control DeserializeControl(string s) { 
                DataContractSerializer serializer = new DataContractSerializer(typeof(Control));
                Control res = (Control)(serializer.ReadObject(Functions.GenerateStreamFromString(s)));
                if (res is TreeControl)
                {
                    TreeControl c2 = res as TreeControl;
                    c2.storedHierarchyData = new HierarchyNavTable();
                    if (c2.storedHierarchyDataSet.Tables.Count > 0)
                    {
                        c2.storedHierarchyData.Merge(c2.storedHierarchyDataSet.Tables[0]);
                        c2.storedHierarchyDataSet.Tables.Add(c2.storedHierarchyData);
                        c2.storedHierarchyDataSet.Relations.Clear();
                        c2.storedHierarchyData.DataSet.Relations.Add(CC.CONTROL_HIERARCHY_RELATION,
                        c2.storedHierarchyData.Columns["Id"], c2.storedHierarchyData.Columns["ParentId"], true);
                    }
                }

            return res;
        }

        private Field DeserializeField(string s) {
            DataContractSerializer serializer = new DataContractSerializer(typeof(Field));
            Field f = (Field)(serializer.ReadObject(Functions.GenerateStreamFromString(s)));
            return f;
        }

        
        private void FlattenPanel(Panel parentPanel) {
            Panels[parentPanel.panelId] = parentPanel;
            foreach (Panel p in parentPanel.children)
                FlattenPanel(p);
        }

        #endregion

        public void IncreaseVersionNumber() {
            query("UPDATE", dbe.Table("projects"), "SET `version` = `version` + 1");
        }


        /*
        public int SetLock(int? idPanel = null, string type = "EditLock") {
            int userID = (int)(Membership.GetUser().ProviderUserKey);

        }
        */

        // not used for now
        public void LogUserAction(System.Data.DataRow data)
        {
            query("INSERT INTO log_users", dbe.InsVals(data));
        }

        /// <summary>
        /// Saves the panel to the system database, but doesn`t save the filds / controls (so that they can be saved in "next wave", after the panel ID is known
        /// and the the control target panel can be bound to the new panel via this ID
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="recursive"></param>
        private void AddPanelOnly(Panel panel, bool recursive = true) {
            Dictionary<string, object> insertVals = new Dictionary<string, object>();

            insertVals["content"] = panel.Serialize();
            insertVals["id_project"] = CE.project.Id;
            if (panel.parent != null)
                insertVals["id_parent"] = panel.parent.panelId;
            if (!IsInTransaction)
            {
                BeginTransaction();
                query("INSERT INTO panels ", dbe.InsVals(insertVals));
                panel.SetCreationId(LastId());
                CommitTransaction();
            }
            else
            {
                query("INSERT INTO panels ", dbe.InsVals(insertVals));
                panel.SetCreationId(LastId());
            }
            if (recursive) foreach (Panel child in panel.children)
                    AddPanelOnly(child);
        }

        /// <summary>
        /// Saves the controls within a panel and/or its childrent to the database; this is done after the control target panel ID has been set
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="recursive"></param>
        private void AddPanelControlsOnly(Panel panel, bool recursive = true) {
            foreach (Control control in panel.controls)
            {
                control.RefreshPanelId();
                AddControl(control);
            }
            if (recursive) foreach (Panel child in panel.children)
                    AddPanelControlsOnly(child);
        }

        /// <summary>
        /// Saves the fields within a panels and/or its children to the system database
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="recursive"></param>
        private void AddPanelFieldsOnly(Panel panel, bool recursive = true)
        // not relly needed (unike AddPanelControlsOnly), just for the sake of unified pattern
        {

            foreach (Field field in panel.fields)
            {
                field.RefreshPanelId();
                AddField(field);
            }
            if(recursive) foreach (Panel child in panel.children)
                        AddPanelFieldsOnly(child);
        }

        
        // all the panel`s controls will be bound to their target panel
        /// <summary>
        /// Sets targetPanelId property of the given panel controls to the targetPanel`s id so that this can be serialized and saved to database.
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="recursive"></param>
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
        
        /// <summary>
        /// Saves a new panel to the database (must have null ID, which will be changed to the new AI inserted in DB).
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="recursive"></param>
        public void AddPanel(Panel panel, bool recursive = true)
        {
            AddPanelOnly(panel, recursive);
            SetControlPanelBindings(panel, recursive);
            AddPanelFieldsOnly(panel, recursive);
            AddPanelControlsOnly(panel, recursive);
        }

        /// <summary>
        /// Saves an existing panel to the database, rewriting the original attributes and all the fields and controls (including those that haven`t changed),
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="recursive"></param>
        public void UpdatePanel(Panel panel, bool recursive = true)
        {
            Dictionary<string, object> updateVals = new Dictionary<string, object>();
            if (panel.parent != null)
                updateVals["id_parent"] = panel.parent.panelId;
            updateVals["content"] = panel.Serialize();
            query("UPDATE panels SET", dbe.UpdVals(updateVals), "WHERE id_panel = ", panel.panelId);
            query("DELETE FROM fields WHERE id_panel = ", panel.panelId);
            foreach (Field field in panel.fields)
            {
                AddField(field);
            }
            query("DELETE FROM controls WHERE id_panel = ", panel.panelId);
            foreach (Control control in panel.controls)
            {
                AddControl(control);
            }
            
            if (recursive) {
                foreach (Panel child in panel.children)
                    UpdatePanel(child, true);
            }
        }

        /// <summary>
        /// Removes the given panel from the database.
        /// </summary>
        /// <param name="panel"></param>
        public void RemovePanel(Panel panel)
        {
            query("DELETE FROM panels WHERE id_panel = ", panel.panelId);
        }


        /// <summary>
        /// Saves a new field to the databse (must have null ID, which will be set to the AI from the database).
        /// </summary>
        /// <param name="field"></param>
        public void AddField(Field field) {   // fieldId = 0
            Dictionary<string, object> insertVals = new Dictionary<string,object>();
            
            insertVals["id_panel"] = field.panelId;
            insertVals["content"] = field.Serialize();
            if (!IsInTransaction)
            {
                BeginTransaction();
                query("INSERT INTO fields", dbe.InsVals(insertVals));
                field.SetCreationId(LastId());    // must be 0 in creation
                CommitTransaction();
            }
            else {
                query("INSERT INTO fields", dbe.InsVals(insertVals));
                field.SetCreationId(LastId());    // must be 0 in creation
            }


        }

        /// <summary>
        /// Rewrites the properties of a given field in the database.
        /// </summary>
        /// <param name="field"></param>
        private void UpdateField(Field field){
            MemoryStream ms = new MemoryStream();
            DataContractSerializer ser = new DataContractSerializer(typeof(Field));
            ser.WriteObject(ms, field);
            query("UPDATE fields SET content = ", Functions.StreamToString(ms), " WHERE id_field = ", field.fieldId);
            
        }

        public void RemoveField(Field field)
        {
            query("DELETE FROM fields WHERE id_field = ", field.fieldId);
        }

        /// <summary>
        /// Saves a new contol to the database (must have null ID, which will be set to the AI from the database).
        /// </summary>
        /// <param name="control"></param>
        public void AddControl(Control control)
        {   // fieldId = 0
            Dictionary<string, object> insertVals = new Dictionary<string, object>();

            insertVals["id_panel"] = control.panelId;
            insertVals["content"] = control.Serialize();
            if (!IsInTransaction)
            {
                BeginTransaction();
                query("INSERT INTO controls", dbe.InsVals(insertVals));
                control.SetCreationId(LastId());    // must be 0 in creation
                CommitTransaction();
            }
            else
            {
                query("INSERT INTO controls", dbe.InsVals(insertVals));
                control.SetCreationId(LastId());    // must be 0 in creation
            }


        }

        private void UpdateControl(Control control)
        {
            MemoryStream ms = new MemoryStream();
            DataContractSerializer ser = new DataContractSerializer(typeof(Control));
            ser.WriteObject(ms, control);
            query("UPDATE controls SET content = ", Functions.StreamToString(ms), " WHERE id_control = ", control.controlId);

        }

        public void RemoveControl(Control control)
        {
            query("DELETE FROM controls WHERE id_control = ", control.controlId);
        }
        
        /// <summary>
        /// Simply runs the constructor of the Project using the provided DataRow data object and returns the result
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private CE.Project ProjectFromDataRow(DataRow row) {
            CE.Project project = new CE.Project(
                (Int32)row["id_project"],
                (string)row["name"],
                (string)row["server_type"],
                (string)row["connstring_web"],
                (string)row["connstring_information_schema"],
                (Int32)row["version"]
                );
            return project;
        }

        public CE.Project GetProject(int projectId) { 
            DataRow row = fetch("SELECT * FROM projects WHERE id_project = ", projectId);
            return ProjectFromDataRow(row);
            }

        public CE.Project GetProject(string projectName) {
            DataRow row = fetch("SELECT * FROM projects WHERE `name` = '" + projectName + "'");
            return ProjectFromDataRow(row);
        }

        public string[] GetProjectNameList() {
            DataTable resTable = fetchAll("SELECT name FROM projects");
            string[] res = new string[resTable.Rows.Count];
            for (int i = 0; i < resTable.Rows.Count; i++)
                res[i] = resTable.Rows[i]["name"] as string;
            return res;
        }
        /// <summary>
        /// full
        /// </summary>
        /// <returns></returns>
        public DataTable GetProjects() {
            DataTable res = fetchAll("SELECT * FROM projects");
            res.PrimaryKey = new DataColumn[] { res.Columns["id_project"] };
            return res;
        }

        public List<CE.Project> GetProjectObjects() {
            DataTable projects = GetProjects();
            List<CE.Project> projectsList = new List<CE.Project>();
            foreach (DataRow r in projects.Rows)
                projectsList.Add(ProjectFromDataRow(r));
            return projectsList;
        }

        public void UpdateProject(int id, Dictionary<string,object> data) {
            query("UPDATE projects SET ", dbe.UpdVals(data), " WHERE id_project = ", id); 
        }

        public int InsertProject(Dictionary<string, object> data) {
            query("INSERT INTO projects ", dbe.InsVals(data));
            return LastId();
        }

        public bool ProposalExists() {
            object res = fetchSingle("SELECT COUNT(*) FROM panels WHERE id_project = ", CE.project.Id);
            return (Convert.ToInt32(res) > 0);
        }

        public void ClearProposal() {
            query("DELETE FROM panels WHERE id_project = ", CE.project.Id);
        }

        public void ProcessLogTable() {
            ProcessLogTable(logTable);
        }

        /// <summary>
        /// Saves the logged user actions to the database and clears the log.
        /// </summary>
        /// <param name="data"></param>
        public void ProcessLogTable(DataTable data)
        {
            bool didWriteLog = writeLog;    // do not log the logging
            writeLog = false;

            if (!(data is DataTable)) throw new MissingMemberException("logTable is not a DataTable");
            foreach (DataRow row in data.Rows)
            {
                // replace string and numeric constants with "s?" and "n?"
                row["query"] = Regex.Replace(row["query"] as string, "([^a-zA-Z_])[0-9]+(.[0-9]+)?", "$1n?");
            }
            var queryGroups = (from r in data.AsEnumerable() group r by r["query"] into s  select new 
                { Query = s.Key as string, Count = s.Count(), MaxTime = s.Max(x => (int)x["time"]), 
                    TotalTime = s.Sum(x=> (int)x["time"])  });

            Dictionary<string, object> insertVals = new Dictionary<string, object>();
            insertVals.Add("query", "");
            foreach (var queryGroup in queryGroups) { 
                insertVals["query"] = queryGroup.Query;
                insertVals["count"] = queryGroup.Count;
                insertVals["total_time"] = queryGroup.TotalTime;
                insertVals["max_time"] = queryGroup.MaxTime;
                query("INSERT INTO log_db", dbe.InsVals(insertVals));
            }
            data.Clear();

            writeLog = didWriteLog;
        }

        public void SetUserRights(int user, int? project, int access) {
            Dictionary<string, object> insertVals = new Dictionary<string, object>{
                {"id_user", user},
                {"id_project", project},
                {"access", access}
            };
            query("REPLACE INTO ", dbe.Table("access_rights"), dbe.InsVals(insertVals));
            //Dictionary<string, object> updVals = new Dictionary<string, object> { { "access", access } };
            //query("UPDATE ", dbe.Table("access_rights"), dbe.UpdVals(updVals), "WHERE ", dbe.Col("id_project"), " = ", project, " AND `id_user` = ", user);
        
        }

        public int GetUserRights(int user, int? project) {
            int? rights;
            if (project == null)
            {
                rights = (int?)fetchSingle("SELECT", dbe.Col("access"), " FROM ", dbe.Table("access_rights"),
                    "WHERE `id_project` IS NULL AND `id_user` = ", user);
            }
            else
            {
                rights = (int?)fetchSingle("SELECT", dbe.Col("access"), " FROM ", dbe.Table("access_rights"),
                    "WHERE `id_project` = ", project, " AND `id_user` = ", user);
            }
            return rights ?? 0;
        }

    }
}
