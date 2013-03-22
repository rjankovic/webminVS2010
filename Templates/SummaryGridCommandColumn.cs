using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using _min.Common;
using _min.Models;

namespace _min.Templates
{
    public class SummaryGridCommandColumn : ITemplate
    {
        List<UserAction> options;
        ListItemType itemType;

        public SummaryGridCommandColumn(ListItemType itemType, List<UserAction> options = null) {
            this.itemType = itemType;
            this.options = new List<UserAction>();
            this.options.Add(UserAction.View);
            this.options.Add(UserAction.Insert);
            this.options.Add(UserAction.Delete);
            this.options.Add(UserAction.Multiple);
            if (options != null)
                this.options = options;
        }

        public void InstantiateIn(System.Web.UI.Control container)
        {
            switch (itemType)
            {
                case ListItemType.Footer:
                    if(options.Contains(UserAction.Insert)){
                        LinkButton insButton = new LinkButton();
                        insButton.Text = "Insert new item";
                        insButton.CommandName = "_Insert";
                        container.Controls.Add(insButton);
                    }
                    break;
                case ListItemType.Item:
                    if (options.Contains(UserAction.View))
                    {
                        LinkButton viewButton = new LinkButton();
                        viewButton.Text = "Edit";
                        viewButton.CommandName = "_View";
                        container.Controls.Add(viewButton);
                    }
                    if (options.Contains(UserAction.Delete))
                    {
                        LinkButton delButton = new LinkButton();
                        delButton.Text = "Delete";
                        delButton.CommandName = "_Delete";
                        delButton.OnClientClick = "return confirm('Really?')";
                        container.Controls.Add(delButton);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}