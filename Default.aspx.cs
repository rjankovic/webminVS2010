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
using System.Data;
using System.IO;
using _min.Common;
using _min.Controls;

namespace _min_t7
{
    public partial class _Default : System.Web.UI.Page
    {

        private _min.Models.Panel proposal = null;

        StatsMySql stats;
        SystemDriverMySql sysDriver;
        WebDriverMySql webDriver;
        _min.Models.Architect architect;

        public event ArchitectNotice AdditionalArchitectNotice;


        private void ProposalReady(IAsyncResult result)
        {
            Session.Clear();
            Task<Panel> taskResult = result as Task<Panel>;
            proposal = taskResult.Result;
            //AdditionalArchitectNotice(architect, new ArchitectNoticeEventArgs("Done."));
            sysDriver.ProcessLogTable();
            //AdditionalArchitectNotice(architect, new ArchitectNoticeEventArgs("Checking proposal"));
            //Panel retrieved = sysDriver.getArchitectureInPanel();
            TextBox1.Text = "retrieved ";
            //bool correctness = architect.checkPanelProposalProposal();
            //TextBox1.Text += correctness;
            //List<string> diff = Common.Debug.ComparePanels(proposal, retrieved);
        }

        protected void Page_Init(object sender, EventArgs e) {
            M2NMappingControl m2nc = new M2NMappingControl();
            Dictionary<string, int> vals = new Dictionary<string, int>();
            vals.Add("Halo", 1);
            vals.Add("Aha", 2);
            vals.Add("Lala", 3);
            m2nc.SetOptions(vals);
            this.Form.Controls.Add(m2nc); 
            //this.Controls.Add(m2nc);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Session.Clear();
            
            DataSet ds = new DataSet();
            HierarchyNavTable hdt = new HierarchyNavTable();
            HierarchyRow r = hdt.NewRow() as HierarchyRow;
            r.Caption = "C";
            r.Id = 1;
            r.ParentId = 0;
            r.NavId = 2;
            hdt.Rows.Add(r);
            HierarchyRow r2 = hdt.NewRow() as HierarchyRow;
            r2.Id = 2;
            r2.ParentId = 1;
            r2.Caption = "D";
            r2.NavId = 3;
            hdt.Rows.Add(r2);
            
            ds.Tables.Add(hdt);
            ds.Relations.Add(new DataRelation("Hierarchy", hdt.Columns["Id"], hdt.Columns["ParentId"], false));
            DataContractSerializer serializer = new DataContractSerializer(typeof(Control));
            /*

            DataRow[] children = hdt.Rows[0].GetChildRows("Hierarchy");
            

            MemoryStream ms = new MemoryStream();
            DataContractSerializer ser = new DataContractSerializer(typeof(DataSet));
            ser.WriteObject(ms, ds);
            string serialized = Functions.StreamToString(ms);
            DataSet deser = (DataSet)(ser.ReadObject(Functions.GenerateStreamFromString(serialized)));

            deser = deser;
            */
            //string dbName = "naborycz";
            //string dbName = "ks";
             
            /*
            
            FK fk = new FK("FKtable", "FKcolumn", "FKreftable", "FKrefCol", "FKdisplayCol");
            FKField fkf = new FKField(123, "FKCol", 0, fk, "C a p t i o n");
            fkf.validationRules.Add(_min.Common.ValidationRules.Required);
            fkf.validationRules.Add(_min.Common.ValidationRules.DateTime);
            string s = fkf.Serialize();
            
            TextBox1.Text = s;
            DataContractSerializer ser = new DataContractSerializer(typeof(Field));
            Field f2 = (Field)ser.ReadObject(_min.Common.Functions.GenerateStreamFromString(s));
            TextBox1.Text = f2.Serialize();
            */
            /*
            stats = new StatsMySql(
                dbName, "Server=85.248.220.75;Uid=dotnet;Pwd=dotnet;Database=information_schema;pooling=true");
            sysDriver = new SystemDriverMySql(
                "Server=85.248.220.75;Uid=dotnet;Pwd=dotnet;Database=deskmin2;pooling=true", _min.Common.Environment.dbLogTable, true);
            CE.project = sysDriver.getProject(1);
            webDriver = new WebDriverMySql(
                "Server=85.248.220.75;Uid=dotnet;Pwd=dotnet;Database=" + dbName + ";pooling=false");
            architect = new _min.Models.Architect(sysDriver, stats);


            CE.user = new CE.User();
            CE.user.id = 1;
            CE.user.login = "test";
            CE.user.name = "test";
            CE.user.rights = 10;
             */ 

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
            /*
            Task<Panel> proposalTask = new Task<Panel>(() =>
            {
                return architect.propose();
            }
            );
            proposalTask.ContinueWith((taskResult) => ProposalReady(taskResult));

            proposalTask.Start();
             */ 
        }

    }
}
