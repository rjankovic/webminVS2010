using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using _min.Models;
using MySql.Data.MySqlClient;
using _min.Interfaces;
using System.Data;
using _min.Common;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using CC = _min.Common.Constants;
using CE = _min.Common.Environment;
using System.IO;

namespace _min.Models
{
    class SystemDriverMySql : BaseDriverMySql, ISystemDriver
    {
        private DbDeployableMySql dbe = new DbDeployableMySql();
        public Panel MainPanel { get; private set; }
        public Dictionary<int, Panel> Panels { get; private set; }


        private bool isInTransaction = false;
        
        
        
        
        public SystemDriverMySql(string connstring, DataTable logTable = null, bool writeLog = false)
            : base(connstring, logTable, writeLog)
        {
        }

        public void InitArchitecture(Panel mainPanel = null)
        {
            Panels = new Dictionary<int, Panel>();
            if (mainPanel != null) MainPanel = mainPanel;
            else MainPanel = getArchitectureInPanel();
            FlattenPanel(MainPanel);
        }

        public void InitArchitectureBasePanel(Panel mainPanel = null)
        {
            Panels = new Dictionary<int, Panel>();
            if (mainPanel != null) MainPanel = mainPanel;
            else MainPanel = GetBasePanel();
            Panels[MainPanel.panelId] = MainPanel;
        }
        /*

        public void saveLog()
        {
            if (logTable.IsInitialized) { 
                foreach(DataRow r in logTable.Rows){
                    query("INSERT INTO log_db VALUES ", r);
                }
                logTable.Rows.Clear();
            }
        }

        public void saveLog(DataTable data)
        {
            if (data.IsInitialized)
            {
                foreach (DataRow r in data.Rows)
                {
                    query("INSERT INTO log_db", r);
                }
                data.Rows.Clear();
            }
        }
        */

        private void FlattenPanel(Panel parentPanel) {
            Panels.Add(parentPanel.panelId, parentPanel);
            foreach (Panel p in parentPanel.children)
                FlattenPanel(p);
        }

        public void logUserAction(System.Data.DataRow data)
        {
            query("INSERT INTO log_users", data);
        }

        // not this way!
        public bool isUserAuthorized(int panelId, UserAction act)
        {
            int rightsPossessed = (int)fetchSingle("SELECT access FROM access_rights WHERE id_user = "
                + CE.user.id + " AND id_project = ", CE.project.id);
            string rightsCol;
            switch (act) { 
                case UserAction.View:
                    rightsCol = "view";
                    break;
                case UserAction.Insert:
                    rightsCol = "insert";
                    break;
                default:
                    rightsCol = "modify";
                    break;
            }
            return (bool)fetchSingle("SELECT ", rightsPossessed, " >= ", rightsCol, 
                "_rights_reqiured FROM panels WHERE id_panel = ", panelId);
        }

        // neither
        public void doRequests()
        {
            DataTable requests = fetchAll("SELECT * FROM requests WHERE id_project IS NULL OR id_project = ",
                Common.Environment.project.id, " AND `when` > NOW()");
            
            // TODO fire requests
            
            var requestsToRemove = from req in requests.AsEnumerable() where req["repeat"] == null select req["id_request"];
            query("DELETE FROM requests WHERE id_request IN ", requestsToRemove);
            var requestsToRepeat = from req in requests.AsEnumerable() where req["repeat"] != null select req;
            foreach(DataRow row in requestsToRepeat)
                query("UPDATE requests SET `when` = ADDTIME(`when`,", row["repeat"], ")");
        }

        private List<Field> PanelFields(int idPanel){
            DataTable tbl = fetchAll("SELECT * FROM fields WHERE id_panel = ", idPanel);
            List<Field> res = new List<Field>();
            foreach(DataRow row in tbl.Rows){

                DataContractSerializer serializer = new DataContractSerializer(typeof(Field));
                Field f = (Field)(serializer.ReadObject(Functions.GenerateStreamFromString(row["content"] as string)));
                f.fieldId = (int)row["id_field"];
                res.Add(f);

                }

            return res;
        }

        private List<Control> PanelControls(int idPanel) {
            DataTable tbl = fetchAll("SELECT * FROM controls WHERE id_panel = ", idPanel);
            List<Control> res = new List<Control>();
            foreach (DataRow row in tbl.Rows)
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(Control));
                Control c = (Control)(serializer.ReadObject(Functions.GenerateStreamFromString(row["content"] as string)));
                c.controlId = (int)row["id_control"];
                //c.SetCreationId((int)row["id_control"]);    // !!
                if (c is TreeControl) {
                    TreeControl c2 = c as TreeControl;
                    c2.storedHierarchyData = new HierarchyNavTable();
                    if (c2.storedHierarchyDataSet.Tables.Count > 0)
                    {
                        c2.storedHierarchyData.Merge(c2.storedHierarchyDataSet.Tables[0]);
                        c2.storedHierarchyDataSet.Tables.Add(c2.storedHierarchyData);
                        //if(!c2.storedHierarchyData.DataSet.Relations.Contains("Hierarchy")) // !!
                        c2.storedHierarchyDataSet.Relations.Clear();
                        c2.storedHierarchyData.DataSet.Relations.Add("Hierarchy", 
                c2.storedHierarchyData.Columns["Id"], c2.storedHierarchyData.Columns["ParentId"], true);

                    }

                } 
                res.Add(c);
            }
             
            return res;
        }

        // only exceptionally
        /*
        public DataTable fetchBaseNavControlTable(List<Panel>) { 
            // well...
            /*
            DataTable fetched = fetchAll("SELECT id_panel, id_parent, table_name AS name FROM panels");      
            // TODO get a real unique panel name
            HierarchyNavTable hier = new HierarchyNavTable();
            HierarchyRow r = new HierarchyRow();
            foreach (row fr in fetched) { 
                r.Caption = 
            }
             
            return null;
        }
        */
        // -||-
        private DataSet getPanelHierarchy() {
            DataTable panels = fetchAll("SELECT id_panel, table_name, id_parent FROM panels " 
            + " JOIN panel_types USING(id_type) WHERE id_project = ", CE.project.id);
            panels.TableName = "panels";
            DataColumn[] tablePK = new DataColumn[1] {panels.Columns["id_panel"]};
            panels.PrimaryKey = tablePK;
            DataSet ds = new DataSet();
            ds.Tables.Add(panels);
            ds.Relations.Add("panelHierarchy", ds.Tables["panels"].Columns["id_panel"], ds.Tables["panels"].Columns["id_parent"]);
            return ds;
        }


        private List<Panel> getVisiblePanelChildren(Panel basePanel, bool recursive = true)
        {
            DataTable visibleChildrenIds = new DataTable();
            if(basePanel.fields.Count > 0)
                visibleChildrenIds = fetchAll("SELECT id_panel FROM panels WHERE id_holder IN", 
                    dbe.InList(new List<object>(from Field f in basePanel.fields select (object)f.fieldId)));
    
            Panel currentChild;
            List<Panel> res = new List<Panel>();
            foreach(DataRow row in visibleChildrenIds.AsEnumerable()){
                currentChild = getPanel((int)(row["id_panel"]), recursive, basePanel);
            }
            return res;
        }

        private List<Panel> getPanelChildren(Panel basePanel, bool recursive = true)
        {
            DataTable ChildrenIds = new DataTable();
            
                ChildrenIds = fetchAll("SELECT id_panel FROM panels WHERE id_parent = " + basePanel.panelId);

            Panel currentChild;
            List<Panel> res = new List<Panel>();
            foreach (DataRow row in ChildrenIds.AsEnumerable())
            {
                currentChild = getPanel((int)(row["id_panel"]), recursive, basePanel);
                res.Add(currentChild);
            }
            return res;
        }

        public void BindPanelChildrenFull(Panel basePanel) {

            DataTable childrenIds = new DataTable();
            if (basePanel.fields.Count > 0)
                childrenIds = fetchAll("SELECT id_panel FROM panels WHERE id_parent =", basePanel.panelId);

            Panel currentChild;
            foreach (DataRow row in childrenIds.AsEnumerable())
            {
                currentChild = getPanel((int)(row["id_panel"]), false, basePanel);
                BindPanelChildrenFull(currentChild);
                basePanel.AddChild(currentChild);
            }
        }

        public Panel getPanel(int panelId, bool recursive = true, Panel parent = null)
        {
            DataRow panelRow = fetch("SELECT * FROM panels WHERE id_panel = ", panelId);

            DataContractSerializer serializer = new DataContractSerializer(typeof(Panel));
            Panel result =  ((Panel)(serializer.ReadObject(Functions.GenerateStreamFromString(panelRow["content"] as string))));
            result.InitAfterDeserialization();

            List<Field> fields = PanelFields(panelId);
            List<Control> controls = PanelControls(panelId);
            /*
            foreach (Field field in fields) {
                field.panel = result;
            }
            foreach (Control control in controls)
            {
                control.panel = result;
            }
            */
            result.panelId = panelId;
            result.AddControls(controls);
            result.AddFields(fields);
            if(recursive) result.AddChildren(getPanelChildren(result, true));
            
            //if(recursive) result.AddChildren(getVisiblePanelChildren(result, true));
            return result;
        }

        public Panel getPanel(string tableName, UserAction action, bool recursive = true, Panel parent = null)
        {
            int panelId = (int)fetchSingle("SELECT id_panel FROM panels JOIN panel_types USING(id_type) WHERE table_name = '" + tableName 
                + "' AND id_project = ", CE.project.id, " AND type_name = '" + action.ToString() + "'");
            
            return getPanel(panelId, recursive, parent);
        }

        public Panel getArchitectureInPanel() {        // !!! make sure there is only one panel with id_parent = NULL
            int basePanelId = (int)fetchSingle("SELECT id_panel FROM panels WHERE id_parent IS NULL AND id_project = ", 
                CE.project.id);
            StartTransaction();
            Panel res = getPanel(basePanelId, true);
            CommitTransaction();
            return res;
        }

        public Panel GetBasePanel() {
            int basePanelId = (int)fetchSingle("SELECT id_panel FROM panels WHERE id_parent IS NULL AND id_project = ",
                CE.project.id);
            // wel...
            Panel res = getPanel(basePanelId, true);
            return res;
        }

        public void AddPanel(Panel panel, bool recursive = true)
        {
            Dictionary<string, object> insertVals = new Dictionary<string, object>();

            insertVals["content"] = panel.Serialize();
            insertVals["id_project"] = CE.project.id;
            if(panel.parent != null)
                insertVals["id_parent"] = panel.parent.panelId;
            if (!IsInTransaction)
            {
                StartTransaction();
                query("INSERT INTO panels ", dbe.InsVals(insertVals));
                panel.SetCreationId(LastId());
                CommitTransaction();
            }
            else {
                query("INSERT INTO panels ", dbe.InsVals(insertVals));
                panel.SetCreationId(LastId());
            }

            foreach (Field field in panel.fields) {
                AddField(field);
            }
            foreach(Control control in panel.controls){
                AddControl(control);
            }

            //  rewrite props !

            if (recursive) {
                foreach (Panel child in panel.children)
                {
                    AddPanel(child, true);
                }
            }
        }

        public void updatePanel(Panel panel, bool recursive = true)
        {
            Dictionary<string, object> updateVals = new Dictionary<string, object>();
            if (panel.parent != null)
                updateVals["id_parent"] = panel.parent;
            updateVals["content"] = panel.Serialize();
            query("UPDATE panels SET", dbe.UpdVals(updateVals), "WHERE id_panel = ", panel.panelId);
            query("DELETE FROM fields WHERE id_panel = " + panel.panelId);
            foreach (Field field in panel.fields)
            {
                AddField(field);
            }
            query("DELETE FROM controls WHERE id_panel = " + panel.panelId);
            foreach (Control control in panel.controls)
            {
                AddControl(control);
            }
            //RewritePanelProperties(panel);!!

            if (recursive) {
                foreach (Panel child in panel.children)
                    updatePanel(child, true);
            }
        }

        public void removePanel(Panel panel)
        {
            query("DELETE FROM panels WHERE id_panel = ", panel.panelId);
        }


        public void AddField(Field field) {   // fieldId = 0
            Dictionary<string, object> insertVals = new Dictionary<string,object>();
            
            insertVals["id_panel"] = field.panelId;
            insertVals["content"] = field.Serialize();
            if (!IsInTransaction)
            {
                StartTransaction();
                query("INSERT INTO fields", dbe.InsVals(insertVals));
                field.SetCreationId(LastId());    // must be 0 in creation
                CommitTransaction();
            }
            else {
                query("INSERT INTO fields", dbe.InsVals(insertVals));
                field.SetCreationId(LastId());    // must be 0 in creation
            }


        }

        private void updateField(Field field){
            MemoryStream ms = new MemoryStream();
            DataContractSerializer ser = new DataContractSerializer(typeof(Field));
            ser.WriteObject(ms, field);
            query("UPDATE fields SET content = ", Functions.StreamToString(ms), " WHERE id_field = ", field.fieldId);
            
        }

        public void removeField(Field field)
        {
            query("DELETE FROM fields WHERE id_field = ", field.fieldId);
        }


        public void AddControl(Control control)
        {   // fieldId = 0
            Dictionary<string, object> insertVals = new Dictionary<string, object>();

            insertVals["id_panel"] = control.panelId;
            insertVals["content"] = control.Serialize();
            if (!IsInTransaction)
            {
                StartTransaction();
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

        private void updateControl(Control control)
        {
            MemoryStream ms = new MemoryStream();
            DataContractSerializer ser = new DataContractSerializer(typeof(Control));
            ser.WriteObject(ms, control);
            query("UPDATE controls SET content = ", Functions.StreamToString(ms), " WHERE id_control = ", control.controlId);

        }

        public void removeControl(Control control)
        {
            query("DELETE FROM controls WHERE id_control = ", control.controlId);
        }
        
        
        // don`t use this
        public CE.User getUser(string userName, string password)
        {
            CE.User user = new CE.User();
            DataRow row = fetch("SELECT * FROM users WHERE login = '" + userName +"' AND MD5('" 
                + password + CC.SALT + "' = password");
            if(row == null) throw new Exception("User not found");
            user.id = (int)row["id_user"];
            user.login = userName;
            user.name = (string)row["name"];
            user.rights = (int)fetchSingle("SELECT access FROM access_rights WHERE id_user = ", 
                user.id, " AND id_project = ", CE.project.id);
            return user;
        }

        private CE.Project ProjectFromDataRow(DataRow row) {
            CE.Project project = new CE.Project();
            project.id = (Int32)row["id_project"];
            project.lastChange = (DateTime)row["last_modified"];
            project.name = (string)row["name"];
            project.serverName = (string)row["server_type"];
            project.connstringIS = (string)row["connstring_information_schema"];
            project.connstringWeb = (string)row["connstring_web"];
            return project;
        }

        public CE.Project getProject(int projectId) { 
            DataRow row = fetch("SELECT * FROM projects WHERE id_project = ", projectId);
            return ProjectFromDataRow(row);
            }

        public CE.Project getProject(string projectName) {
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
        public DataTable GetProjects() {
            DataTable res = fetchAll("SELECT * FROM projects");
            res.PrimaryKey = new DataColumn[] { res.Columns["id_project"] };
            return res;
        }
        public void UpdateProject(int id, Dictionary<string,object> data) {
            query("UPDATE projects SET ", dbe.UpdVals(data), " WHERE id_project = ", id); 
        }

        public void InsertProject(Dictionary<string, object> data) {
            query("INSERT INTO projects ", dbe.UpdVals(data));
        }

        /*
        public Dictionary<PanelTypes, int> PanelTypeNameIdMap() {
            Dictionary<PanelTypes, int> res = new Dictionary<PanelTypes, int>();
            DataTable tab = fetchAll("SELECT * FROM panel_types");
            foreach (DataRow row in tab.Rows) { 
                res.Add((PanelTypes)Enum.Parse(typeof(PanelTypes), row["type_name"] as string), (int)row["id_type"]);
            }
            return res;
        }

        public Dictionary<FieldTypes, int> FieldTypeNameIdMap()
        {
            Dictionary<FieldTypes, int> res = new Dictionary<FieldTypes, int>();
            DataTable tab = fetchAll("SELECT * FROM field_types");
            foreach (DataRow row in tab.Rows)
            {
                res.Add((FieldTypes)Enum.Parse(typeof(FieldTypes), row["type_name"] as string), (int)row["id_type"]);
            }
            return res;
        }
        */
        public bool ProposalExists() {
            object res = fetchSingle("SELECT COUNT(*) FROM panels WHERE id_project = ", CE.project.id);
            return (Convert.ToInt32(res) > 0);
        }

        public void ClearProposal() {
            query("DELETE FROM panels WHERE id_project = ", CE.project.id);
        }

        public void ProcessLogTable() {
            ProcessLogTable(logTable);
        }

        public void ProcessLogTable(DataTable data)
        {
            bool didWriteLog = writeLog;    // do not log the logging
            writeLog = false;

            if (!(data is DataTable)) throw new MissingMemberException("logTable is not a DataTable");
            foreach (DataRow row in data.Rows)
            {
                // replace string and numeric constants with "s?" and "n?"
                //row["query"] = Regex.Replace(row["query"] as string, "['\"][^'\"]+['\"]", "s?");
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

        public void RewriteControlDefinitions(Panel panel, bool recursive = true)
        {
            Dictionary<string, object> updateVals = new Dictionary<string, object>();
           
            foreach (Control c in panel.controls)
            {
                updateVals["content"] = c.Serialize();
                query("UPDATE controls SET ", dbe.UpdVals(updateVals), " WHERE id_control = ", c.controlId);
            }

            if (recursive)
                foreach (Panel p in panel.children)
                    RewriteControlDefinitions(p);
        }

        public void RewriteFieldDefinitions(Panel panel, bool recursive = true)
        {
            Dictionary<string, object> updateVals = new Dictionary<string, object>();

            foreach (Field f in panel.fields)
            {
                updateVals["content"] = f.Serialize();
                query("UPDATE fields SET ", dbe.UpdVals(updateVals), " WHERE id_field = ", f.fieldId);
            }

            if (recursive)
                foreach (Panel p in panel.children)
                    RewriteFieldDefinitions(p);
        }
        
        /*
        public void RewriteControlDefinitions(Panel panel)
        {
            throw new NotImplementedException();
        }
         */

        /*
        public Dictionary<UserAction, int> GetPanelActionPanels(int currentPanel)
        {
            DataTable tbl = fetchAll("SELECT `action`, id_panel FROM panels WHERE tablename = " + currentPanel);
            Dictionary<UserAction, int> res = new Dictionary<UserAction, int>();
            foreach(DataRow row in tbl.Rows){
                res.Add((UserAction)Enum.Parse(typeof(UserAction), row["action"].ToString()), (int)row["id_panel"]);
            }
            return res;
        }
         */ 
    }
}
