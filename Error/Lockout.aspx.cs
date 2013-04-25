using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using _min.Common;
using _min.Models;

using _min.Navigation;
using CE = _min.Common.Environment;
using CC = _min.Common.Constants;
using MPanel = _min.Models.Panel;
using WC = System.Web.UI.WebControls;

namespace _min.Error
{
    public partial class Lockout : System.Web.UI.Page
    {

        protected void Page_Init(object sender, EventArgs e)
        {
        }




        protected void Page_Load(object sender, EventArgs e)
        {
            // default message for errors not listed below
            lockoutNote.Text = "You either do not own the access rights neccessary to visit this page or the page is being"
                    + " used by another user and concurrent access may lead to malfunction.";
            int msgNum;
            if (Page.RouteData.Values.ContainsKey("message") 
                && Int32.TryParse(Page.RouteData.Values["message"].ToString(), out msgNum))
                switch(msgNum){
                    case 1: lockoutNote.Text = 
                        "The administration interface is currently being modified. Please come back later.";
                        break;
                    case 2: lockoutNote.Text =
                            "The architecture of the site is currently being modified. Please come back later.";
                        break;
                    case 3: lockoutNote.Text =
                        "You do not own the permission to modify this project's architecture.";
                        break;
                    case 4: lockoutNote.Text =
                        "You do not own administrative permission for this project.";
                        break;
                    case 5: lockoutNote.Text =
                        "You do not own the permission to manage users privilleges for this project.";
                        break;
                    case 6: lockoutNote.Text =
                        "You do not own the permission to manage projects.";
                        break;
                    case 7: lockoutNote.Text =
                        "The session has expired. Please log in.";
                        break;
                }

        }
    }

}