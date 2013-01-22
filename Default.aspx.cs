using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using MySql.Data.MySqlClient;
using MySql;
using _min.Models;
using _min.Interfaces;
using CE = _min.Common.Environment;
using CC = _min.Common.Constants;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace _min_t7
{
    public partial class _Default : System.Web.UI.Page
    {

        private _min.Models.Panel proposal = null;

        StatsMySql stats;
        SystemDriverMySql sysDriver;
        WebDriverMySql webDriver;
        Architect architect;

        public event ArchitectNotice AdditionalArchitectNotice;


        private void ProposalReady(IAsyncResult result)
        {
            Task<Panel> taskResult = result as Task<Panel>;
            proposal = taskResult.Result;
            //AdditionalArchitectNotice(architect, new ArchitectNoticeEventArgs("Done."));
            sysDriver.ProcessLogTable();
            //AdditionalArchitectNotice(architect, new ArchitectNoticeEventArgs("Checking proposal"));
            //Panel retrieved = sysDriver.getArchitectureInPanel();
            TextBox1.Text = "retrieved ";
            bool correctness = architect.checkProposal();
            TextBox1.Text += correctness;
            //List<string> diff = Common.Debug.ComparePanels(proposal, retrieved);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            //string dbName = "naborycz";
            string dbName = "ks";
            
            FK fk = new FK("FKtable", "FKcolumn", "FKreftable", "FKrefCol", "FKdisplayCol");
            FKField fkf = new FKField(123, "FKCol", 0, fk, "C a p t i o n");
            fkf.validationRules.Add(_min.Common.ValidationRules.Required);
            fkf.validationRules.Add(_min.Common.ValidationRules.DateTime);
            string s = fkf.Serialize();
            
            TextBox1.Text = s;
            DataContractSerializer ser = new DataContractSerializer(typeof(Field));
            Field f2 = (Field)ser.ReadObject(_min.Common.Functions.GenerateStreamFromString(s));
            TextBox1.Text = f2.Serialize();


            stats = new StatsMySql(
                dbName, "Server=85.248.220.75;Uid=dotnet;Pwd=dotnet;Database=information_schema;pooling=true");
            sysDriver = new SystemDriverMySql(
                "Server=85.248.220.75;Uid=dotnet;Pwd=dotnet;Database=deskmin2;pooling=true", _min.Common.Environment.dbLogTable, true);
            CE.project = sysDriver.getProject(1);
            webDriver = new WebDriverMySql(
                "Server=85.248.220.75;Uid=dotnet;Pwd=dotnet;Database=" + dbName + ";pooling=false");
            architect = new Architect(sysDriver, stats);


            CE.user = new CE.User();
            CE.user.id = 1;
            CE.user.login = "test";
            CE.user.name = "test";
            CE.user.rights = 10;

        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            /*
            architect.Notice += new ArchitectNotice(Architect_Notice);
            this.AdditionalArchitectNotice += new ArchitectNotice(Architect_Notice);
            architect.Question += new ArchitectQuestion(Architect_Question);
            architect.Error += new ArchitectureError(Architect_Error);
            architect.Warning += new ArchitectWarning(Architect_Warning);
            */
            Task<Panel> proposalTask = new Task<Panel>(() =>
            {
                return architect.propose();
            }
            );
            proposalTask.ContinueWith((taskResult) => ProposalReady(taskResult));

            proposalTask.Start();
        }

    }
}
