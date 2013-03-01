using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using _min.Common;
using _min.Models;

namespace _min.Interfaces
{
    public interface ICondition
    {
        string Translate();
    }
    /*
    public interface FK 
    {
        string refTable { get; }
        string myTable { get; }
        string myColumn { get; }
        string refColumn { get; }
        string displayColumn { get; set; }
        Dictionary<string, int> options { get; }    // display value & FK
        //Dictionary<object, object> getOptionsGeneral();   // maybe later
        bool validateInput(string inputValue);
    }

    public interface M2NMapping : FK {
        string mapTable { get; }
        // column in the mapping table that reffers to ref_table
        string mapRefColumn { get; }
        // column in the mapping table that reffers to my this fiel's column
        string mapMyColumn { get; }
        bool validateWholeInput(List<string> inputValues);
    }
     */

    /*
    public interface Panel        // no data included, will be added in the presenter 
    {
        DataRow PK { get; }
        int panelId { get; }
        string panelName { get; }
        PanelTypes type { get; }
        string tableName { get; }
        Panel parent { get; }
        List<string> PKColNames { get; }
        
        List<Panel> children { get; }  // MDI
        List<Field> fields { get; }    // vratane dockov
        List<Control> controls { get; }

        bool isBaseNavPanel { get; }
        int? idHolder { get; }
        
        void AddChildren(List<Panel> children);        // none of these must overwrite already existing object / property
        void AddFields(List<Field> fields);
        void AddControls(List<Control> controls);
        void AddViewAttr(object key, object value);
        void AddControlAttr(object key, object value);
        
        void SetCreationId(int id);
        void SetParentPanel(Panel parent);
        string Serialize();
    }

    public interface Field {
        //attrs readonly
        int fieldId { get; }   // just for initialization of a newly created field, probably to be removed later
        string column { get; }
        int panelId { get; set; }
        bool ValidateSelf();
        object value { get; set; }
        FieldTypes type { get; }
        HashSet<ValidationRules> validationRules { get; }
        int position { get; }
        void SetCreationId(int id);
        string Serialize();
    }

    public interface FKField : Field {
        FK fk { get; }
    }

    public interface M2NMappingField : Field
    {
        M2NMapping mapping { get; }
    }

    public interface Control { 
    // type {UserAction}, attr - value; readonly
        int? controlId { get; }
        DataTable data { get; set;  }
        List<string> PKColNames { get; } // action parameter
        UserAction action { get; }
        int editationRightsRequired { get; }
        int insertRightsRequired { get; }
        bool navTableSpan { get; }
        string navTableCopyCaption { get; }
        string navTableDeleteCaption { get; }
        string navTableEditCaption { get; }
        bool navThroughRecords { get; }
        bool navThroughPanels { get; }
        int targetPanelId { get; }
        Panel targetPanel { get; }
        int panelId { get; }
        void SetCreationId(int id);
        string Serialize();

    }
    */
    public interface IBaseDriver 
    {
        // + constructor from connection
        // 3 connections total
        DataTable fetchAll(params object[] parts);
        DataRow fetch(params object[] parts);
        object fetchSingle(params object[] parts);
        int query(params object[] parts);   // returns rows affected
        void StartTransaction();
        void CommitTransaction();
        void RollbackTransaction();
        //int SetBufferSize(int size);
        //bool SetBufferUse(bool use);
        //void SendBuffer();
        DataTable logTable { get; }
    }

    public interface IWebDriver : IBaseDriver     // webDB
    {
        void FillPanel(Panel panel);
        void FillPanelArchitect(Panel panel);
        int insertPanel(Panel panel, DataRow values);  // returns insertedId
        void updatePanel(Panel panel, DataRow values);
        void deletePanel(Panel panel);
        DataTable fetchFKOptions(FK fk);
        void MapM2NVals(M2NMapping mapping, int key, int[] vals);
        void UnmapM2NMappingKey(M2NMapping mapping, int key);
        DataRow PKColRowFormat(Panel panel);
    }

    public interface IStats : IBaseDriver {    // information_schema
        Dictionary<string, DataColumnCollection> ColumnTypes { get; }
        Dictionary<string, List<string>> ColumnsToDisplay { get; }
        List<FK> foreignKeys(string tableName);
        List<List<string>> indexes(string tableName);
        List<string> primaryKeyCols(string tableName);
        List<string> TwoColumnTables();
        List<string> TableList();
        List<M2NMapping> findMappings();
        DateTime TableCreation(string tableName);//...
        List<string> TablesMissingPK();
        Dictionary<string, List<string>> GlobalPKs();
        List<FK> selfRefFKs();
    }
    /*
    public interface IArchitect  // systemDB, does not fill structures with data
    {
        Panel getArchitectureInPanel();        // hierarchia - vnutorne vola getPanel

        Panel propose();   // for the whole site
        Panel proposeForTable(string tableName);
        bool checkPanelProposal(Panel proposal, bool recursive = true);
        bool checkPanelProposal(int panelId, bool recursive = true);    // load from db
        bool checkProposal();       // for the whole project, load from db
    }
    */
    public interface ISystemDriver : IBaseDriver // systemDB
    {
        void ProcessLogTable();
        void ProcessLogTable(DataTable data);
        void logUserAction(DataRow data);
        bool isUserAuthorized(int panelId, UserAction act);
        void doRequests();
        Panel getPanel(string tableName, UserAction action, bool recursive = true, Panel parent = null);
        Panel getPanel(int panelId, bool recursive = true, Panel parent = null);
        Panel getArchitectureInPanel();
        Panel GetBasePanel();
        void AddPanel(Panel panel, bool recursive = true);
        void AddField(Field field);
        void AddControl(Control control);
        void updatePanel(Panel panel, bool recursive = true);
        //void updatePanelProperty(Panel panel, string propertyName);
        void removePanel(Panel panel);
        void RewriteControlDefinitions(Panel panel, bool recursive = true);
        void RewriteFieldDefinitions(Panel panel, bool recursive = true);
        //Common.Environment.User getUser(string userName, string password);
        Common.Environment.Project getProject(int projectId);
        Common.Environment.Project getProject(string projectName);

        bool ProposalExists();
        void ClearProposal();
        //DataTable fetchBaseNavControlTable();   // the DataTable for main TreeControl / MenuDrop

        string[] GetProjectNameList();
        DataTable GetProjects();
        void UpdateProject(int id, Dictionary<string, object> data);
        void InsertProject(Dictionary<string, object> data);
        //Dictionary<UserAction, int> GetPanelActionPanels(int currentPanel);
    }
}
