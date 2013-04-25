﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Text.RegularExpressions;
using _min.Interfaces;
using _min.Models;

namespace _min.Common
{
    public enum DbServer { MySql, MSSQL };
    public enum UserAction { View, Insert, Update, Delete, Multiple }
    public enum AppRequest { ArchitectureReload, StopLogging, StartLogging }
    public enum PanelTypes { Editable, NavTable, NavTree, MenuDrop, MenuTabs, Monitor, Container }
    public enum FieldTypes { FK, M2NMapping, Date, DateTime, Time, Holder, ShortText, Text, Bool, Enum }
    public enum PropertyConcerns { Control, Validation, View }
    public enum ValidationRules { Required, Ordinal, Decimal, DateTime, Date, Unique }
    public enum GlobalState { Architect, Administer, UsersManagement, ProjectsManagement, Account, Error, Unknown }
    public enum LockTypes { AdminLock, ArchitectLock }

    public class LockAcquiryException : Exception
    { }

    public static class Environment
    {
        private static GlobalState _globalState = Common.GlobalState.Unknown;
        public static GlobalState GlobalState
        {
            get { return _globalState; }
            set {
                //if (_globalState == Common.GlobalState.Unknown)
                    _globalState = value;
                //else throw new Exception("Global state already set");
            }
        }


        
        // User and Project objects can be created freely, 
        // but only once initiated in the Environment
        
        public class Project {
            public int Id { get; private set; }
            public string Name { get; private set; }
            public string ServerType { get; private set; }
            public string ConnstringWeb { get; private set; }
            public string ConnstringIS { get; private set; }
            public int Version { get; private set; }
            public string WebDbName { get; private set; }
            public Project(int id, string name, string serverType, string connstringWeb, string connstringIS, int version) {
                this.Id = id;
                this.Name = name;
                this.ServerType = serverType;
                this.ConnstringWeb = connstringWeb;
                this.ConnstringIS = connstringIS;
                this.Version = version;
                if(connstringWeb != null && Regex.IsMatch(ConnstringWeb, ".*[Dd]atabase=\"?([^\";]+)\"?.*"))
                    this.WebDbName = Regex.Match(ConnstringWeb, ".*[Dd]atabase=\"?([^\";]+)\"?.*").Groups[1].Value;
            }
        }

        
        public static Project _project = null;

        public static Project project
        {
            get 
            {
                return _project;
            }
            set
            {
                    _project = value;
            }
        }

        // possibly for different database engines over different dbDrivers
        public static DataTable dbLogTable = new DataTable();
    }


    static class Constants {
        public const string SALT = "hjjh5435435jl43kj5ljlj53l4j5lk4";

        public const string ADMIN_PREFIX = "Admin of ";
        public const string ARCHITECT_PREFIX = "Architect of ";
        public const string ENUM_COLUMN_VALUES = "EnumValues";
        
        public const string CONTROL_HIERARCHY_RELATION = "Hierarchy";
        public const string SYSDRIVER_FK_PANEL_PARENT = "FK_panel_parent";
        public const string SYSDRIVER_FK_CONTROL_PANEL = "FK_control_panel";
        public const string SYSDRIVER_FK_FIELD_PANEL = "FK_field_panel";

        public const string COLUMN_EDITABLE = "Editable";

        public const string SERVER_MYSQL = "MySql";     // code in projects.server_type
        public const string SERVER_MSSQL = "MSSql";

        public const string SESSION_ARCHITECTURE = "Architecture";
        public const string SESSION_ARCHITECTURE_VERSION = "ArchitectureVersion";
        public const string SESSION_ACTIVE_PANEL = "ActivePanel";


    }

}
