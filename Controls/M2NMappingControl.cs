﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Web.UI.WebControls;
using System.ComponentModel;
using System.Web.UI;

namespace _min.Controls
{
    [ToolboxData("<{0}:M2NMapping runat=server></{0}:M2NMapping>")]
    public class M2NMappingControl : CompositeControl
    {
        private ListBox inList = new ListBox();
        private ListBox outList = new ListBox();
        private string _ID;

        public ListItemCollection IncludedItems {
            get {
                EnsureChildControls();

                return inList.Items;
            }
        }
        public ListItemCollection ExcludedItems
        {
            get
            {
                EnsureChildControls();
                return outList.Items;
            }
        }

        public override string ID {
            get { return _ID; }
            set { _ID = value; }
        }

        
        public void SetOptions(IDictionary<string, int> vals){
            outList.DataSource = vals;
            outList.DataTextField = "Key";
            outList.DataValueField = "Value";
            EnsureChildControls();
            inList.DataBind();
            outList.DataBind();
        }

        public void SetIncludedOptions(List<string> included) {
            foreach (string s in included) {
                ListItem item = outList.Items.FindByText(s);
                inList.Items.Add(item);
                outList.Items.Remove(item);
            }
        }

        protected override void CreateChildControls()
        {
            //inList.EnableViewState = true;
            //outList.EnableViewState = true;
            inList.SelectedIndexChanged += OnINListSelectedItemChanged;
            outList.SelectedIndexChanged += OnOutListSelectedItemChanged;
            inList.AutoPostBack = true;
            outList.AutoPostBack = true;
            this.Controls.Clear();
            this.Controls.Add(inList);
            this.Controls.Add(outList);
        }

        protected void OnINListSelectedItemChanged(object sender, EventArgs e) {
            //if (inList.SelectedIndex == -1) return;
            outList.Items.Add(inList.SelectedItem);
            inList.Items.Remove(inList.SelectedItem);
            outList.SelectedIndex = -1; // the selected item moves
        }
        protected void OnOutListSelectedItemChanged(object sender, EventArgs e)
        {
            //if (outList.SelectedIndex == -1) return;
            inList.Items.Add(outList.SelectedItem);
            outList.Items.Remove(outList.SelectedItem);
            inList.SelectedIndex = -1;  // the selected item moves
        }

       

        protected override void Render(HtmlTextWriter writer)
        {
            AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "10", false);
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            inList.Attributes.Add("runat", "server");
            inList.RenderControl(writer);
            writer.RenderEndTag();
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            outList.Attributes.Add("runat", "server");
            outList.RenderControl(writer);
            writer.RenderEndTag();
            writer.RenderEndTag();
            writer.RenderEndTag();
        }
    }
}