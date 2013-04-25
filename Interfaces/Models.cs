using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using _min.Common;
using _min.Models;
using MySql.Data.MySqlClient;
using MySql.Web;
using CE = _min.Common.Environment;

namespace _min.Interfaces
{
    public interface IBaseDriver 
    {
        // + constructor from connection
        // 3 connections total
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
        void FillPanelArchitect(Panel panel);
        void FillPanelFKOptionsArchitect(Panel panel);
        int InsertIntoPanel(Panel panel);  // returns insertedId
        void UpdatePanel(Panel panel);
        void DeleteFromPanel(Panel panel);
        //DataTable fetchFKOptions(FK fk);
        //void MapM2NVals(M2NMapping mapping, int key, int[] vals);
        //void UnmapM2NMappingKey(M2NMapping mapping, int key);
        DataRow PKColRowFormat(Panel panel);
    }

    public interface IStats : IBaseDriver {    // information_schema
        Dictionary<string, DataColumnCollection> ColumnTypes { get; }
        Dictionary<string, List<string>> ColumnsToDisplay { get; }
        //List<FK> ForeignKeys(string tableName);
        //List<List<string>> Indexes(string tableName);
        //List<string> PrimaryKeyCols(string tableName);
        List<string> TwoColumnTables();
        //List<string> TableList();
        //List<M2NMapping> FindMappings();
        List<string> TablesMissingPK();
        //Dictionary<string, List<string>> PrimaryKeyCols();
        List<FK> SelfRefFKs();
        Dictionary<string, List<M2NMapping>> Mappings { get; }
        Dictionary<string, List<string>> PKs { get; }
        //FK SelfRefFKStrict(string tableName);
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
        //bool isUserAuthorized(int panelId, UserAction act);
        //void doRequests();
        //Panel getPanel(string tableName, UserAction action, bool recursive = true, Panel parent = null);
        //Panel getPanel(int panelId, bool recursive = true, Panel parent = null);
        //Panel getArchitectureInPanel();
        //Panel GetBasePanel();
        void AddPanel(Panel panel, bool recursive = true);
        void AddPanels(List<Panel> panels);
        void AddField(Field field);
        void AddControl(Control control);
        void UpdatePanel(Panel panel, bool recursive = true);
        //void updatePanelProperty(Panel panel, string propertyName);
        void RemovePanel(Panel panel);
        //void RewriteControlDefinitions(Panel panel, bool recursive = true);
        //void RewriteFieldDefinitions(Panel panel, bool recursive = true);
        //Common.Environment.User getUser(string userName, string password);
        Common.Environment.Project GetProject(int projectId);
        Common.Environment.Project GetProject(string projectName);

        bool ProposalExists();
        void ClearProposal();
        //DataTable fetchBaseNavControlTable();   // the DataTable for main TreeControl / MenuDrop

        string[] GetProjectNameList();
        DataTable GetProjects();
        void UpdateProject(CE.Project project);
        int InsertProject(CE.Project project);
        void DeleteProject(int projectId);

        //void InitArchitecture(Panel mainPanel = null);
        //void InitArchitectureBasePanel(Panel mainPanel = null);
        void IncreaseVersionNumber();
        //Dictionary<UserAction, int> GetPanelActionPanels(int currentPanel);
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
}
