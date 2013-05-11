using System;
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
    class SystemDriver : ISystemDriver
    {
        private DbDeployableFactory dbe = new DbDeployableFactory();
        public Panel MainPanel { get; private set; }
        public Dictionary<int, Panel> Panels { get; private set; }
        private IBaseDriver driver;

        private Dictionary<LockTypes, int> lockNumbers = new Dictionary<LockTypes,int>{
            {LockTypes.AdminLock, 10},
            {LockTypes.ArchitectLock, 100}
        };

        
        public SystemDriver(IBaseDriver baseDriver)
        {
            this.driver = baseDriver;
        }


        public void SetArchitecture(Panel mainPanel) {
            MainPanel = mainPanel;
            Panels = new Dictionary<int, Panel>();
            FlattenPanel(MainPanel);
        }
        
        // use only through FullProject load, if there is no panel in session or project version has changed
        #region Deserialization

        /// <summary>
        /// loads the whole project from database (in 3 queries)
        /// </summary>
        public void FullProjectLoad() {
            Panels = new Dictionary<int, Panel>();

            driver.BeginTransaction();
            FK control_panel = new FK("controls", "id_panel", "panels", "id_panel", null);
            FK field_panel = new FK("fields", "id_panel", "panels", "id_panel", null);
            DataTable panels = driver.fetchAll("SELECT * FROM ", dbe.Table("panels"), "WHERE id_project =", CE.project.Id);
            DataTable controls = driver.fetchAll("SELECT controls.* FROM ", dbe.Table("controls"), dbe.Join(control_panel), "WHERE id_project =", CE.project.Id);
            DataTable fields = driver.fetchAll("SELECT fields.* FROM ", dbe.Table("fields"), dbe.Join(field_panel), "WHERE id_project =", CE.project.Id);
            driver.CommitTransaction();
            
            panels.TableName = "panels";
            controls.TableName = "controls";
            fields.TableName = "fields";
            
            DataSet ds = new DataSet();
            /*
            panels.PrimaryKey = new DataColumn[] { panels.Columns["id_panel"] };
            controls.PrimaryKey = new DataColumn[] { panels.Columns["id_control"] };
            fields.PrimaryKey = new DataColumn[] { panels.Columns["id_field"] };
            */
            if(panels.DataSet is DataSet)
                panels.DataSet.Tables.Clear();
            if(controls.DataSet is DataSet)
                controls.DataSet.Tables.Clear();
            if(fields.DataSet is DataSet)
                fields.DataSet.Tables.Clear();
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
            List<IField> fields = DataSet2PanelFields(ds, panelId);
            List<Control> controls = DataSet2PanelControls(ds, panelId);
            DataRow panelRow = ds.Tables["panels"].AsEnumerable().Where(x => (Int32)x["id_panel"] == panelId).First();
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
            DataRow panelRow = ds.Tables["panels"].AsEnumerable().Where(x => (Int32)x["id_panel"] == panelId).First();
            DataRow[] controlRows = panelRow.GetChildRows(CC.SYSDRIVER_FK_CONTROL_PANEL);
            List<Control> res = new List<Control>();
            Control c;
            foreach (DataRow controlRow in controlRows) {
                c = DeserializeControl(controlRow["content"] as string, (int)(controlRow["id_control"]));
                res.Add(c);
            }
            return res;
        }

        /// <summary>
        /// deserializes a panel`s fields
        /// </summary>
        /// <param name="ds">the dataset from FullProjectLoad</param>
        private List<IField> DataSet2PanelFields(DataSet ds, int panelId) {
            DataRow panelRow = ds.Tables["panels"].AsEnumerable().Where(x => (Int32)x["id_panel"] == panelId).First();
            DataRow[] fieldRows = panelRow.GetChildRows(CC.SYSDRIVER_FK_FIELD_PANEL);
            List<IField> res = new List<IField>();
            IField f;
            foreach (DataRow fieldRow in fieldRows)
            {
                f = DeserializeField(fieldRow["content"] as string, (int)(fieldRow["id_field"]));
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
        private Control DeserializeControl(string s, int id) {
            if (s.Contains("storedHierarchyDataSet")) return new Control(0, "", UserAction.Delete);
                DataContractSerializer serializer = new DataContractSerializer(typeof(Control));
                Control res = (Control)(serializer.ReadObject(Functions.GenerateStreamFromString(s)));
                res.controlId = id;
                if (res is TreeControl)
                {
                    AssignSotredHierarchyToControl((TreeControl)res);
                    TreeControl tc = res as TreeControl;
                    if (tc.storedHierarchyData is HierarchyNavTable)
                    {
                        DataSet ds = new DataSet();
                        if (tc.storedHierarchyData.DataSet != null)
                        {
                            tc.storedHierarchyData.DataSet.Relations.Clear();
                            tc.storedHierarchyData.DataSet.Tables.Clear();
                        }
                        ds.Tables.Add(tc.storedHierarchyData);
                        tc.storedHierarchyData.ChildRelations.Add(new DataRelation(CC.CONTROL_HIERARCHY_RELATION,
                        tc.storedHierarchyData.Columns["Id"], tc.storedHierarchyData.Columns["ParentId"], true));
                    }
                }
                return res;
        }

        private IField DeserializeField(string s, int id) {
            DataContractSerializer serializer = new DataContractSerializer(typeof(FieldBase));
            IField f = (IField)(serializer.ReadObject(Functions.GenerateStreamFromString(s)));
            f.SetId(id);
            return f;
        }

        
        private void FlattenPanel(Panel parentPanel) {
            Panels[parentPanel.panelId] = parentPanel;
            foreach (Panel p in parentPanel.children)
                FlattenPanel(p);
        }

        #endregion

        public void IncreaseVersionNumber() {
            driver.query("UPDATE", dbe.Table("projects"), "SET ", dbe.Col("version"), " = ", dbe.Col("version"), " + 1");
        }


        /*
        public int SetLock(int? idPanel = null, string type = "EditLock") {
            int userID = (int)(Membership.GetUser().ProviderUserKey);

        }
        */

        // not used for now
        public void LogUserAction(System.Data.DataRow data)
        {
            driver.query("INSERT INTO",  dbe.Table("log_users"), dbe.InsVals(data));
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
            if (!driver.IsInTransaction)
            {
                driver.BeginTransaction();
                driver.query("INSERT INTO", dbe.Table("panels"), dbe.InsVals(insertVals));
                panel.SetCreationId(driver.LastId());
                driver.CommitTransaction();
            }
            else
            {
                driver.query("INSERT INTO", dbe.Table("panels"), dbe.InsVals(insertVals));
                panel.SetCreationId(driver.LastId());
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

            foreach (IField field in panel.fields)
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
        /// Adds the list of panels non-recursiverly; neccessary when editing panels - couples of panels must be
        /// first created both blank before they can bed given controls and fields (targetId...)
        /// </summary>
        /// <param name="panels"></param>
        public void AddPanels(List<Panel> panels) {
            foreach (Panel p in panels)
                AddPanelOnly(p, false);
            foreach (Panel p in panels)
                SetControlPanelBindings(p, false);
            foreach (Panel p in panels)
                AddPanelFieldsOnly(p, false);
            foreach (Panel p in panels)
                AddPanelControlsOnly(p, false);
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
             
            driver.query("UPDATE panels SET", dbe.UpdVals(updateVals), "WHERE ", dbe.Col("id_panel"), " = ", panel.panelId);

            driver.query("DELETE FROM", dbe.Table("fields"), "WHERE", dbe.Col("id_panel"), " = ", panel.panelId);
            foreach (IField field in panel.fields)
            {
                AddField(field);
            }
            driver.query("DELETE FROM ", dbe.Table("controls"), " WHERE ", dbe.Col("id_panel"), " = ", panel.panelId);
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
            driver.query("DELETE FROM ", dbe.Table("panels"), " WHERE ", dbe.Table("id_panel"), " = ", panel.panelId);
        }


        /// <summary>
        /// Saves a new field to the databse (must have null ID, which will be set to the AI from the database).
        /// </summary>
        /// <param name="field"></param>
        public void AddField(IField field) {   // fieldId = 0
            Dictionary<string, object> insertVals = new Dictionary<string,object>();
            
            insertVals["id_panel"] = field.PanelId;
            insertVals["content"] = field.Serialize();
            if (!driver.IsInTransaction)
            {
                driver.BeginTransaction();
                driver.query("INSERT INTO fields", dbe.InsVals(insertVals));
                field.SetId(driver.LastId());    // must be 0 in creation
                driver.CommitTransaction();
            }
            else {
                driver.query("INSERT INTO fields", dbe.InsVals(insertVals));
                field.SetId(driver.LastId());    // must be 0 in creation
            }


        }

        /// <summary>
        /// Rewrites the properties of a given field in the database.
        /// </summary>
        /// <param name="field"></param>
        private void UpdateField(IField field){
            driver.query("UPDATE fields SET content = ", field.Serialize(), " WHERE id_field = ", field.FieldId);
            
        }

        public void RemoveField(IField field)
        {
            driver.query("DELETE FROM fields WHERE id_field = ", field.FieldId);
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
            if (!driver.IsInTransaction)
            {
                bool wasInTrans = driver.IsInTransaction;
                if(!wasInTrans) driver.BeginTransaction();
                driver.query("INSERT INTO controls", dbe.InsVals(insertVals));
                control.SetCreationId(driver.LastId());    // must be 0 in creation
                if(!wasInTrans) driver.CommitTransaction();
            }
            else
            {
                driver.query("INSERT INTO controls", dbe.InsVals(insertVals));
                control.SetCreationId(driver.LastId());    // must be 0 in creation
            }
            if (control is TreeControl)
            {
                SaveStroedHierarchyOfControl((TreeControl)control);
                TreeControl tc = control as TreeControl;
                if (tc.storedHierarchyData is HierarchyNavTable)
                {
                    DataSet ds = new DataSet();
                    if (tc.storedHierarchyData.DataSet != null)
                    {
                        tc.storedHierarchyData.DataSet.Relations.Clear();
                        tc.storedHierarchyData.DataSet.Tables.Clear();
                    }
                    ds.Tables.Add(tc.storedHierarchyData);
                    tc.storedHierarchyData.ChildRelations.Add(new DataRelation(CC.CONTROL_HIERARCHY_RELATION,
                    tc.storedHierarchyData.Columns["Id"], tc.storedHierarchyData.Columns["ParentId"], true));
                }
            }


        }

        private void UpdateControl(Control control)
        {
            MemoryStream ms = new MemoryStream();
            DataContractSerializer ser = new DataContractSerializer(typeof(Control));
            ser.WriteObject(ms, control);
            driver.query("UPDATE controls SET content = ", Functions.StreamToString(ms), " WHERE id_control = ", control.controlId);
            if (control is TreeControl) {
                SaveStroedHierarchyOfControl((TreeControl)control);
            }
        }

        public void RemoveControl(Control control)
        {
            driver.query("DELETE FROM controls WHERE id_control = ", control.controlId);
            driver.query("DELETE FROM hierarchy_nav_tables WHERE id_control = ", control.controlId);
        }


        /// <summary>
        /// Saves the content of stored hierarchy of a TreeControl to the database
        /// </summary>
        /// <param name="control"></param>
        private void SaveStroedHierarchyOfControl(TreeControl control) { 
            if(control.controlId != null)
                driver.query("DELETE FROM ", dbe.Table("hierarchy_nav_tables"), "WHERE", dbe.Col("id_control"), " = ", control.controlId);
            bool wasInTran = driver.IsInTransaction;

            Dictionary<string, object> insVals = new Dictionary<string, object> { 
                { "id_control", control.controlId } 
            };
            
            if (!wasInTran)
                driver.BeginTransaction();
            foreach (HierarchyRow r in control.storedHierarchyData.Rows) {
                insVals["id_item"] = r.Id; 
                insVals["id_parent"] = r.ParentId;
                insVals["caption"] = r.Caption;
                insVals["id_nav"] = r.NavId;
                driver.query("INSERT INTO ", dbe.Table("hierarchy_nav_tables"), dbe.InsVals(insVals));
            }
            if (!wasInTran)
                driver.CommitTransaction();
        }

        /// <summary>
        /// Extrats the hierchy data for the given TreeControl and assigns it to it. If there is no
        /// data present, the data property of the control remains unchanged.
        /// </summary>
        /// <param name="control"></param>
        public void AssignSotredHierarchyToControl(TreeControl control) {
            if (control.controlId == null) return;

            List<IDbCol> cols = new List<IDbCol>();
            cols.Add(dbe.Col("id_item", "Id"));
            cols.Add(dbe.Col("id_parent", "ParentId"));
            cols.Add(dbe.Col("caption", "Caption"));
            cols.Add(dbe.Col("id_nav", "NavId"));
            DataTable tbl = driver.fetchAll("SELECT ", dbe.Cols(cols), 
                " FROM ", dbe.Table("hierarchy_nav_tables"), "WHERE  id_control = ", control.controlId);

            HierarchyNavTable resHierarchy = new HierarchyNavTable();
            resHierarchy.Merge(tbl);
            control.storedHierarchyData = resHierarchy;
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
                //(string)row["server_type"],
                "OneDay",
                (string)row["connstring_web"],
                (string)row["connstring_information_schema"],
                (Int32)(row["version"])
                );
            return project;
        }

        public CE.Project GetProject(int projectId) { 
            DataRow row = driver.fetch("SELECT * FROM projects WHERE id_project = ", projectId);
            return ProjectFromDataRow(row);
            }

        public CE.Project GetProject(string projectName) {
            DataRow row = driver.fetch("SELECT * FROM projects WHERE ", dbe.Col("name"), " = ", dbe.InObj(projectName));
            return ProjectFromDataRow(row);
        }

        public string[] GetProjectNameList() {
            DataTable resTable = driver.fetchAll("SELECT name FROM projects");
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
            DataTable res = driver.fetchAll("SELECT * FROM projects");
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

        public void UpdateProject(CE.Project project) {
            if (!driver.CheckUniqueness("projects", "name", project.Name, "id_project", project.Id))
            {
                throw new ConstraintException("The name of the project must be unique.");
            }

             Dictionary<string, object> updVals = new Dictionary<string, object>{
                {"name", project.Name},
                {"connstring_web", project.ConnstringWeb},
                {"connstring_information_schema", project.ConnstringIS}
             };

            driver.query("UPDATE projects SET ", dbe.UpdVals(updVals), " WHERE id_project = ", project.Id); 
        }

        public int InsertProject(CE.Project project) {
            if (!driver.CheckUniqueness("projects", "name", project.Name)) {
                throw new ConstraintException("The name of the project must be unique.");
            }
            Dictionary<string, object> insVals = new Dictionary<string, object>{
                {"name", project.Name},
                {"connstring_web", project.ConnstringWeb},
                {"connstring_information_schema", project.ConnstringIS},
                {"version", project.Version}
            };

            driver.BeginTransaction();
            driver.query("INSERT INTO projects ", dbe.InsVals(insVals));
            int id = driver.LastId();
            driver.CommitTransaction();
            return id;
        }

        public void DeleteProject(int projectId) {
            driver.query("DELETE FROM ", dbe.Table("projects"), "WHERE `id_project` = ", projectId);
        }

        public bool ProposalExists() {
            object res = driver.fetchSingle("SELECT COUNT(*) FROM panels WHERE id_project = ", CE.project.Id);
            return (Convert.ToInt32(res) > 0);
        }

        public void ClearProposal() {
            driver.query("DELETE FROM ", dbe.Table("panels"), " WHERE ", dbe.Col("id_project"), " = ", CE.project.Id);

        }

        public void ProcessLogTable() {
            ProcessLogTable(driver.logTable);
        }

        /// <summary>
        /// Saves the logged user actions to the database and clears the log.
        /// </summary>
        /// <param name="data"></param>
        public void ProcessLogTable(DataTable data)
        {
            bool didWriteLog = driver.WriteLog;    // do not log the logging
            driver.WriteLog = false;

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
                driver.query("INSERT INTO log_db", dbe.InsVals(insertVals));
            }
            data.Clear();

            driver.WriteLog = didWriteLog;
        }

        public void SetUserRights(object userId, int? project, int access) {
            Dictionary<string, object> insertVals = new Dictionary<string, object>{
                {"id_user", userId.ToString()},
                {"id_project", project},
                {"access", access}
            };
            driver.BeginTransaction();
            driver.query("DELETE FROM ", dbe.Table("access_rights"), "WHERE ", dbe.Col("id_user"), " = ", dbe.InObj(userId.ToString()), "AND", dbe.Col("id_project"),
                ((project == null) ? "IS NULL" : ("= " + (int)project))); 
            driver.query("INSERT INTO ", dbe.Table("access_rights"), dbe.InsVals(insertVals));
            driver.CommitTransaction();
            //Dictionary<string, object> updVals = new Dictionary<string, object> { { "access", access } };
            //driver.query("UPDATE ", dbe.Table("access_rights"), dbe.UpdVals(updVals), "WHERE ", dbe.Col("id_project"), " = ", project, " AND `id_user` = ", user);
        
        }

        public int GetUserRights(object userId, int? project) {
            int? rights;
            if (project == null)
            {
                rights = (int?)driver.fetchSingle("SELECT", dbe.Col("access"), " FROM ", dbe.Table("access_rights"),
                    "WHERE ", dbe.Col("id_project"), " IS NULL AND ", dbe.Col("id_user"), " = ", dbe.InObj(userId.ToString()));
            }
            else
            {
                rights = (int?)driver.fetchSingle("SELECT", dbe.Col("access"), " FROM ", dbe.Table("access_rights"),
                    "WHERE", dbe.Col("id_project"), " = ", dbe.InObj(project), " AND", dbe.Col("id_user"), " = ", dbe.InObj(userId.ToString()));
            }
            return rights ?? 0;
        }

        /// <summary>
        /// Gets the names of project that the user is administrator / architect of, IGNORING the
        /// global rights
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="adminOf"></param>
        /// <param name="architectOf"></param>
        public void UserMenuOptions(object userId, out List<string> adminOf, out List<string> architectOf) { 
            FK access_project = new FK("access_rights", "id_project", "projects", "id_project", null);
            DataTable admin = driver.fetchAll("SELECT", dbe.Col("projects", "name", null), "FROM", dbe.Table("access_rights"),
                dbe.Join(access_project), "WHERE", dbe.Col("id_user"), " = ", dbe.InObj(userId.ToString()), " AND " , dbe.Col("access_rights", "access", null), " % 100 >= 10");
            adminOf = (from DataRow r in admin.Rows select r[0] as string).ToList<string>();
            
            DataTable architect = driver.fetchAll("SELECT", dbe.Col("projects", "name", null), "FROM ", dbe.Table("access_rights"),
                dbe.Join(access_project), "WHERE", dbe.Col("id_user"), " = ", dbe.InObj(userId.ToString()), " AND ", dbe.Col("access_rights", "access", null), " % 1000 >= 100");
            architectOf = (from DataRow r in architect.Rows select r[0] as string).ToList<string>();
        }

        public void ReleaseLock(object userId, int project, LockTypes lockType) { 
            driver.query("DELETE FROM ", dbe.Table("locks"), "WHERE", dbe.Col("id_owner"), " = ", dbe.InObj(userId.ToString()), "AND ", dbe.Col("id_project"), " = ", 
                project, "AND ", dbe.Col("lock_type"), " = ", lockNumbers[lockType]); 
        }

        public bool TryGetLock(object userId, int project, LockTypes lockType) {
           Dictionary<string, object> insVals = new Dictionary<string,object>{
                {"id_project", project},
                {"id_owner", userId.ToString()},
                {"lock_type", lockNumbers[lockType]}
            };
            object owner = LockOwner(project, lockType);
            if (owner == null)
            {
                try
                {
                    driver.query("INSERT INTO", dbe.Table("locks"), dbe.InsVals(insVals));
                }
                catch
                {
                    return false;
                }
                return true;
            }
            else if (owner.ToString() == userId.ToString()) return true;
            
            return false;
        }

        public object LockOwner(int project, LockTypes lockType) {
            
            object res = driver.fetchSingle("SELECT ", dbe.Col("id_owner"), " FROM ", dbe.Table("locks"), " WHERE ", dbe.Col("id_project"), " = ", 
                project, " AND ", dbe.Col("lock_type"), " = ", lockNumbers[lockType]);
            return res;
        }

        public void RemoveForsakenLocks(List<object> activeUserIds) {
            //List<object> activeUsersObj = new List<object>();
            //foreach (int i in activeUserIds) 
            //    activeUsersObj.Add(i);
            if (activeUserIds.Count > 0)
                driver.query("DELETE FROM ", dbe.Table("locks"), " WHERE ", dbe.Col("id_owner"), " NOT IN ", dbe.InList(activeUserIds));
        }

        public void ReleaseLocksExceptProject(object userId, int projectId) {
            driver.query("DELETE FROM ", dbe.Table("locks"), " WHERE ", dbe.Col("id_owner"), " = ", dbe.InObj(userId.ToString()), " AND ", dbe.Col("id_project"), " != ", projectId);
        }

        public void BeginTransaction() {
            driver.BeginTransaction();
        }

        public void CommitTransaction() {
            driver.CommitTransaction();
        }

    }
}
