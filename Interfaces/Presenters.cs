using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using _min.Interfaces;
using _min.Common;
using _min.Models;

namespace _min.Interfaces
{
    interface Presenter
    {
        void panelSubmitted(Panel panel, UserAction action, DataTable data);
        void navigationMove(Panel panel, UserAction action);
        void proposalSubmitted(Panel panel);
        void Validate(Panel panel, DataTable data);
        //...
    }
}
