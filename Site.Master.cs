using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Security;
using _min.Common;
using _min.Models;
using System.Configuration;
using MPanel = _min.Models.Panel;
using CC = _min.Common.Constants;
using CE = _min.Common.Environment;
using _min.Interfaces;
using System.Web.Routing;

namespace _min
{
    public partial class MinMaster : System.Web.UI.MasterPage
    {
        public _min.Models.Architect Architect { get; private set; }
        public ISystemDriver SysDriver { get; private set; }
        public IWebDriver WebDriver { get; private set; }
        public IStats Stats { get; private set; }
        public string ProjectName { get; private set; }
        public MembershipUser user = null;
        public DbServer DbServer;

        protected void Page_Init(object sender, EventArgs e)
        {
            DbServer = (DbServer)Enum.Parse(typeof(DbServer), System.Configuration.ConfigurationManager.AppSettings["ServerType"] as string);

            user = Membership.GetUser();

            // set the warning only for logged in users
                System.Configuration.ConfigurationManager.AppSettings["SessionWarning"] = 
                    (user is MembershipUser) ? (Session.Timeout - 5).ToString() :"-1";


            string lp = Request.Url.LocalPath;

            if (lp.StartsWith("/architect")){
                Common.Environment.GlobalState = GlobalState.Architect;
            }
            else if (lp.StartsWith("/admin")) {
                Common.Environment.GlobalState = GlobalState.Administer;
            }
            else if(lp == "/sys/users"){
                Common.Environment.GlobalState = GlobalState.UsersManagement;
            }
            else if(lp == "/sys/projects"){
                Common.Environment.GlobalState = GlobalState.ProjectsManagement;
            }
            else if(lp.StartsWith("/account")){
                Common.Environment.GlobalState = GlobalState.Account;
            }
            else 
                Common.Environment.GlobalState = GlobalState.Error;

            
            // session expiry means logout, even if the provider would keep it
            if ((Session.IsNewSession || user == null) 
                && CE.GlobalState != GlobalState.Account && CE.GlobalState != GlobalState.Error)
            {
                FormsAuthentication.SignOut();
                Response.RedirectToRoute("LockoutRoute", new { message = 7 });
            }


            IBaseDriver systemBaseDriver = null;

            switch (DbServer)
            {
                case DbServer.MySql:
                    systemBaseDriver = new BaseDriverMySql(ConfigurationManager.ConnectionStrings["MySqlServer"].ConnectionString);
                    break;
                case DbServer.MsSql:
                    systemBaseDriver = new BaseDriverMsSql(ConfigurationManager.ConnectionStrings["MsSqlServer"].ConnectionString);
                    break;
                default:
                    break;
            }

            SysDriver = new SystemDriver(systemBaseDriver);
            
            // global service

            bool NewProjectLoad = false;

            // get current project and init drivers and architect
            if (Page.RouteData.Values.ContainsKey("projectName"))
            {
                ProjectName = Page.RouteData.Values["projectName"] as string;
                CE.Project actProject = SysDriver.GetProject(ProjectName);

                if (CE.project == null || actProject.Id != CE.project.Id || actProject.Version != CE.project.Version)
                {
                    Session.Clear();    // may not be neccessary in all cases, but better be safe
                    NewProjectLoad = true;
                }
                
                CE.project = SysDriver.GetProject(ProjectName);



                IBaseDriver statsBaseDriver = null;
                IBaseDriver webBaseDriver = null;

                switch (DbServer)
                {
                    case DbServer.MySql:
                        statsBaseDriver = new BaseDriverMySql(CE.project.ConnstringIS);
                        Stats = new StatsMySql((BaseDriverMySql)statsBaseDriver, CE.project.WebDbName);
                        webBaseDriver = new BaseDriverMySql(CE._project.ConnstringWeb);
                        break;
                    case DbServer.MsSql:
                        statsBaseDriver = new BaseDriverMsSql(CE.project.ConnstringIS);
                        Stats = new StatsMsSql((BaseDriverMsSql)statsBaseDriver);
                        webBaseDriver = new BaseDriverMsSql(CE._project.ConnstringWeb);
                        break;
                    default:
                        break;
                }
                WebDriver = new WebDriver(webBaseDriver);


                Architect = new _min.Models.Architect(SysDriver, Stats);

                if ((!Page.IsPostBack || NewProjectLoad) && CE.GlobalState != GlobalState.Error) 
                    // new version or differnet page ~ othervise access must have remained 
                    // at least "allowable", if not allowed
                {
                    LockingAccess();    // just check
                }
                // check whether there is something to load at all
                if (Page.RouteData.Route != RouteTable.Routes["ArchitectInitRoute"])
                {
                    if (!SysDriver.ProposalExists())
                    {
                        if (CE.GlobalState == GlobalState.Architect)
                        {
                            Response.RedirectToRoute("ArchitectInitRoute", new { projectName = Page.RouteData.Values["projectName"] });
                            Response.End();
                        }
                        else
                        {
                            // change to some kind of "Not found" page
                            Response.RedirectToRoute("DefaultRoute", new { projectName = Page.RouteData.Values["projectName"] });
                            Response.End();
                        }
                    }

                    // get the current architecture - either extract from Session or directry from the DB, if project version has changed
                    int actVersion = CE.project.Version;
                    if (Session[CC.SESSION_ARCHITECTURE] is _min.Models.Panel
                        && Session[CC.SESSION_ARCHITECTURE_VERSION] is int
                        && (int)Session[CC.SESSION_ARCHITECTURE_VERSION] == actVersion)
                    {
                        SysDriver.SetArchitecture((MPanel)Session[CC.SESSION_ARCHITECTURE]);
                    }
                    else
                    {
                        SysDriver.FullProjectLoad();
                        Session[CC.SESSION_ARCHITECTURE] = SysDriver.MainPanel;
                        Session[CC.SESSION_ARCHITECTURE_VERSION] = CE.project.Version;
                    }
                }
            }

            // local issues

            if (!Page.IsPostBack)
            {

                if (user != null)
                {

                    List<string> adminOf;
                    List<string> architectOf;
                    List<CE.Project> allProjects = SysDriver.GetProjectObjects();
                    List<string> allNames = (from CE.Project p in allProjects select p.Name).ToList<string>();

                    object userId = user.ProviderUserKey;
                    int globalRights = SysDriver.GetUserRights(userId, null);
                    // by default, fetch only the sites to which the access rights are set explicitly,
                    // if global rights are sufficient, replace them with the complete lists
                    SysDriver.UserMenuOptions(userId, out adminOf, out architectOf);
                    if (globalRights % 100 >= 10) adminOf = allNames;
                    if (globalRights % 1000 >= 100) architectOf = allNames;


                    MenuItem administerItem = new MenuItem("Administer", "admin");
                    foreach (string site in adminOf)
                    {
                        administerItem.ChildItems.Add(new MenuItem(site, site, null, "/admin/" + site));
                    }
                    if (adminOf.Count > 0)
                        NavigationMenu.Items.AddAt(0, administerItem);

                    // architect menu
                    MenuItem architectItem = new MenuItem("Architect", "architect");
                    foreach (string site in architectOf)
                    {
                        architectItem.ChildItems.Add(new MenuItem(site, site, null, "/architect/show/" + Server.UrlEncode(site)));
                    }
                    if (architectOf.Count > 0)
                        NavigationMenu.Items.AddAt(1, architectItem);



                    // user & projects management

                    NavigationMenu.Items.Add(new MenuItem("Manage users", "users", null, "/sys/users"));

                    if (globalRights >= 10000)   // this is the one and only project manager for this application instance
                        NavigationMenu.Items.Add(new MenuItem("Manage projects", "projects", null, "/sys/projects"));


                    // account settings for logged in users
                    MenuItem accountItem = new MenuItem("Account", "account");
                    accountItem.ChildItems.Add(new MenuItem("Change password", null, null, "/account/change-password"));
                    accountItem.ChildItems.Add(new MenuItem("Logout", null, null, "/account/logout"));
                    NavigationMenu.Items.Add(accountItem);
                }
                else {
                    MenuItem accountItem = new MenuItem("Account", "account");
                    accountItem.ChildItems.Add(new MenuItem("Login", null, null, "/account/login"));
                    accountItem.ChildItems.Add(new MenuItem("Register", null, null, "/account/register"));
                    accountItem.ChildItems.Add(new MenuItem("Password recovery", null, null, "/account/password-recovery"));
                    
                    NavigationMenu.Items.Add(accountItem);
                }
                NavigationMenu.RenderingMode = MenuRenderingMode.Table;
            }

        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // for the session timer setting in the header 
            // - must be set after the AppSettings["SessionWarning"] is initalized
            Page.Header.DataBind();
        }


        /// <summary>
        /// Checks wheteher the user has the paermission to use the page and if so, ensures tat the page is not locked due to architecture
        /// modification in progess. Otherwise, redirects the user to the lockout page with an explanation message.
        /// </summary>
        private void LockingAccess()    // neccessary for each postback (?)
        {
            if (user == null)
            {
                Response.RedirectToRoute("LockoutRoute", new { message = 0 });
                Response.End();
            }

            List<object> usersOnline = GetUsersOnline();
            SysDriver.RemoveForsakenLocks(usersOnline);
            object userId = user.ProviderUserKey;

            int globalRights = SysDriver.GetUserRights(userId, null);
            int localRights = SysDriver.GetUserRights(userId, CE.project.Id);

            // totalRights are the maximum from local and global rights, counting by digits
            int totalRights = 0;
            int multiply = 1;
            for (int i = 0; i < 4; i++)
            {
                totalRights += Math.Max(localRights % 10, globalRights % 10)*multiply;
                localRights /= 10;
                globalRights /= 10;
                multiply *= 10;
            }

            // projects management - access rights 10000+ - one such user per application instance, created during installation
            bool projects = globalRights >= 10000;
            bool users = totalRights % 10000 >= 1000;
            bool architect = totalRights % 1000 >= 100;
            bool admin = totalRights % 100 >= 10;

            // release my locks if not in the architect context
            if (CE.GlobalState != GlobalState.Architect)
            {
                SysDriver.ReleaseLock(userId, CE.project.Id, LockTypes.AdminLock);
                SysDriver.ReleaseLock(userId, CE.project.Id, LockTypes.ArchitectLock);
            }

            SysDriver.ReleaseLocksExceptProject(userId, CE.project.Id);

            int errMsg = 0;

            switch (CE.GlobalState)
            {
                case GlobalState.Architect:
                    if (!architect) errMsg = 3;
                    break;
                case GlobalState.Administer:
                    if (!admin) errMsg = 4;
                    break;
                case GlobalState.UsersManagement:
                    if (!users) errMsg = 5;
                    break;
                case GlobalState.ProjectsManagement:
                    if (!projects) errMsg = 6;
                    break;
                case GlobalState.Account:
                    break;
                case GlobalState.Error:
                    break;
                default:
                    break;
            }
            if (errMsg != 0 && CE.GlobalState != GlobalState.Error)
            {
                Response.RedirectToRoute("LockoutRoute", new { message = errMsg });
            }

            // architect must get both architect and administer lock
            if (CE.GlobalState == GlobalState.Architect)
            {
                if (!SysDriver.TryGetLock(userId, CE.project.Id, LockTypes.ArchitectLock)
                    || !SysDriver.TryGetLock(userId, CE.project.Id, LockTypes.AdminLock))
                    Response.RedirectToRoute("LockoutRoute", new
                    {
                        message = 2
                    });
            }
            // adminin checks the lock
            else if (CE.GlobalState == GlobalState.Administer && SysDriver.LockOwner(CE.project.Id, LockTypes.AdminLock) != null)
            {
                Response.RedirectToRoute("LockoutRoute", new
                {
                    message = 1
                        
                });
            }
        }

        /// <summary>
        /// lisst the ids of users online
        /// </summary>
        /// <returns></returns>
        private List<object> GetUsersOnline()
        {
            MembershipUserCollection all = Membership.GetAllUsers();
            List<object> res = new List<object>();
            foreach (MembershipUser u in all)
            {
                if (u.IsOnline)
                    res.Add(u.ProviderUserKey);
            }
            return res;
        }
    }
}
