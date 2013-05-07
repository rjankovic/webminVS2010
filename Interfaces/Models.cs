using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using _min.Common;
using _min.Models;
using MySql.Data.MySqlClient;
using MySql.Web;
using CE = _min.Common.Environment;
using WC = System.Web.UI.WebControls;

namespace _min.Interfaces
{
    public interface IBaseDriver 
    {
        DataTable fetchAll(params object[] parts);
        DataRow fetch(params object[] parts);
        object fetchSingle(params object[] parts);
        int query(params object[] parts);   // returns rows affected
        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();
        DataTable logTable { get; }
        int LastId();
        int NextAIForTable(string tableName);
        void TestConnection();
    }

    public interface IMySqlQueryDeployable
    {
        void Deoploy(MySqlCommand cmd, StringBuilder sb, ref int paramCount);
    }

    /// <summary>
    /// A set of IMySqlDeployable to make typing queries nicer, safer and faster - a IMySqlDeployable can append ("Deploy") 
    /// itself to a MySqlCommand and is used by BaseDriverMysql.  
    /// </summary>
    public interface IDbDeployableFactory {
        IDbInObj InObj(object o);
        
        IDbVals InsVals(Dictionary<string, object> vals);
        IDbVals InsVals(DataRow r);

        IDbVals UpdVals(Dictionary<string, object> vals);
        IDbVals UpdVals(DataRow r);
        
        IDbCol Col(string column);
        IDbCol Col(string column, string alias);
        IDbCol Col(string table, string column, string alias);
        
        IMySqlQueryDeployable Cols(List<IDbCol> cols);
        IMySqlQueryDeployable Cols(List<string> colNames);
        
        IDbJoin Join(FK fk);
        IDbJoin Join(FK fk, string alias);
        
        IMySqlQueryDeployable Joins(List<IDbJoin> joins);
        IMySqlQueryDeployable Joins(List<FK> FKs);

        IDbInList InList(List<object> list);
        IMySqlQueryDeployable Condition(DataRow lowerBounds, DataRow upperBounds = null);

        IDbTable Table(string table, string alias = null);
    }

    public interface IDbInObj: IMySqlQueryDeployable
    {
        object o { get; set; }
        
    }
    public interface IDbVals: IMySqlQueryDeployable
    {
        Dictionary<string, object> vals { get; set; }
        
    }
    public interface IDbCol: IMySqlQueryDeployable
    {
        string table { get; set; }
        string column { get; set; }
        string alias { get; set; }
    }
    public interface IDbCols: IMySqlQueryDeployable
    {
        List<IDbCol> cols { get; set; }
    }
    public interface IDbJoin: IMySqlQueryDeployable
    {
        FK fk { get; set; }
        string alias { get; set; }
    }
    public interface IDbJoins: IMySqlQueryDeployable
    {
        List<IDbJoin> joins { get; set; }
    }
    public interface IDbInList: IMySqlQueryDeployable
    {
        List<object> list { get; set; }
    }

    public interface IDbTable : IMySqlQueryDeployable
    {
        string table { get; set; }
        string alias { get; set; }
    }

    public interface IWebDriver : IBaseDriver     // webDB
    {
        void FillPanel(Panel panel);
        void FillPanelFKOptions(Panel panel);
        int InsertIntoPanel(Panel panel);  // returns insertedId
        void UpdatePanel(Panel panel);
        void DeleteFromPanel(Panel panel);
        DataRow PKColRowFormat(Panel panel);
    }

    public interface IStats : IBaseDriver {    // information_schema
        Dictionary<string, DataColumnCollection> ColumnTypes { get; }
        Dictionary<string, List<string>> ColumnsToDisplay { get; }
        List<string> TwoColumnTables();
        List<string> TablesMissingPK();
        List<FK> SelfRefFKs();
        Dictionary<string, List<M2NMapping>> Mappings { get; }
        Dictionary<string, List<string>> PKs { get; }
        Dictionary<string, List<FK>> FKs { get; }
        List<string> Tables { get; }
        void SetDisplayPreferences(Dictionary<string, string> pref);
    }

    public interface ISystemDriver : IBaseDriver // systemDB
    {
        Panel MainPanel { get; }
        Dictionary<int, Panel> Panels { get; }

        void SetArchitecture(Panel mainPanel);
        void ProcessLogTable();
        void ProcessLogTable(DataTable data);
        void LogUserAction(DataRow data);
        void FullProjectLoad();
        
        void AddPanel(Panel panel, bool recursive = true);
        void AddPanels(List<Panel> panels);
        void AddField(IField field);
        void AddControl(Control control);
        void UpdatePanel(Panel panel, bool recursive = true);
        void RemovePanel(Panel panel);
        
        Common.Environment.Project GetProject(int projectId);
        Common.Environment.Project GetProject(string projectName);
        bool ProposalExists();
        void ClearProposal();
        DataTable GetProjects();
        void UpdateProject(CE.Project project);
        int InsertProject(CE.Project project);
        void DeleteProject(int projectId);

        void IncreaseVersionNumber();
        void SetUserRights(int user, int? project, int rights);
        int GetUserRights(int user, int? project);
        List<Common.Environment.Project> GetProjectObjects();
        
        void ReleaseLock(int user, int project, LockTypes lockType);
        bool TryGetLock(int user, int project, LockTypes lockType);
        int? LockOwner(int project, LockTypes lockType);
        void RemoveForsakenLocks(List<int> activeUsers);
        void UserMenuOptions(int user, out List<string> adminOf, out List<string> architectOf);
        void ReleaseLocksExceptProject(int userId, int projectId);
    }


    public enum FieldSpecificityLevel
    {
        Low, Medium, High
    };

    public interface IField {
        System.Web.UI.WebControls.WebControl MyControl { get; }     // created upon call
        int? FieldId { get; }
        void SetId(int id);
        Panel Panel { get; set; }
        int? PanelId { get; }
        void RefreshPanelId();
        string Caption { get; set; }
        bool Required {get; set;}
        string ErrorMessage { get; }
        void Validate();        // server-side
        List<System.Web.UI.WebControls.BaseValidator> GetValidators();
        
        void RetrieveData();    // from the webcontrol
        void FillData();    // to the webcontrol
        void InventData();  // for the architect mode
        object Data { get; set; }

        string Serialize();
    }

    public interface IColumnField : IField{
        string ColumnName {get;}
        bool Unique { get; set; }
        
    }


    public interface IFieldFactory : ICloneable
    {
        string UIName { get; }
        FieldSpecificityLevel Specificity { get; }
    }
    
    public interface IColumnFieldFactory : IFieldFactory {
        bool CanHandle(DataColumn column);
        ColumnField Create(DataColumn column);
        Type ProductionType { get; }
    }

    public interface ICustomizableColumnFieldFactory : IColumnFieldFactory {
        void ShowForm(WC.Panel panel);
        void ValidateForm();
        string ErrorMessage { get; }
        void LoadProduct(IColumnField field);
        void UpdateProduct(IColumnField field);
    }

    /*
    public interface IDependentTableField : IField{
        string TableName {get;}
        DataTable Data {get; set;}          // with table PK set
        DataTable ObsoleteData {get; set;}  // with table PK set

        bool CanHandle(DataTable table, List<DataColumn> constCols);
        // columns in primary table -> corresponding in mapping table
        Dictionary<string, string> constColsMapping;
        // columns that will be determined by the current row edited in the panel, in the names of the mapping table
        // these will be removed in a single query and replaced by new mapping
        DataRow ConstCols { get; set; }
        IDependentTableField Create(DataTable table, List<DataColumn> constCols);
    }

    public interface IDependentTableFactory
    {
        bool CanHandle(DataTable table, List<DataColumn> constCols);
        IDependentTableField Create(DataTable table, List<DataColumn> constCols);
        bool MyProduct(IDependentTableField field);
    }
    */
}
