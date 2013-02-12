using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using _min.Common;
using _min.Interfaces;
using _min.Models;
using System.Configuration;
using System.Text.RegularExpressions;

using CC = _min.Common.Constants;
using CE = _min.Common.Environment;

namespace _min_t7.Architect
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        ISystemDriver sysDriver;
        IStats stats;
        _min.Models.Architect architect;
        DataTable dbLog;
        _min.Models.Panel basePanel;

        protected void Page_Init(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                string projectName = Page.RouteData.Values["projectName"] as string;
                sysDriver = new SystemDriverMySql(ConfigurationManager.ConnectionStrings["LocalMySqlServer"].ConnectionString);
                _min.Common.Environment.project = sysDriver.getProject(projectName);

                if (!sysDriver.ProposalExists())
                {
                    Response.RedirectToRoute("ArchitectInitRoute", RouteData);
                    Response.End();
                    //return;
                }

                string WebDbName = Regex.Match(CE.project.connstringWeb, ".*Database=\"?([^\";]+)\"?.*").Groups[1].Value;
                basePanel = sysDriver.GetBasePanel();
                stats = new StatsMySql(CE.project.connstringIS, WebDbName);
                architect = new _min.Models.Architect(sysDriver, stats);
                Session["sysDriver"] = sysDriver;
                Session["stats"] = stats;
                Session["architect"] = architect;
                Session["basePanel"] = basePanel;
                MainPanel.Controls.Add(basePanel.controls[0].ToUControl());
            }
            else
            {
                stats = (IStats)Session["stats"];
                sysDriver = (ISystemDriver)Session["sysDriver"];
                architect = (_min.Models.Architect)Session["architect"];
                basePanel = (_min.Models.Panel)Session["basePanel"];
            }



        }
    }
}