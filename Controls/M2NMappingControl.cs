using System;
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
        
        public void SetOptions(IDictionary<string, int> vals){
            outList.DataSource = vals;
            EnsureChildControls();
        }

        protected override void CreateChildControls()
        {
            inList.DataBind();
            outList.DataBind();
            inList.SelectedIndexChanged += OnINListSelectedItemChanged;
            outList.SelectedIndexChanged += OnOutListSelectedItemChanged;
            inList.AutoPostBack = true;
            outList.AutoPostBack = true;
            this.Controls.Clear();
            this.Controls.Add(inList);
            this.Controls.Add(outList);
        }

        private void OnINListSelectedItemChanged(object sender, EventArgs e) {
            //if (inList.SelectedIndex == -1) return;
            outList.Items.Add(inList.SelectedItem);
            inList.Items.Remove(inList.SelectedItem);
            outList.SelectedIndex = -1; // the selected item moves
        }
        private void OnOutListSelectedItemChanged(object sender, EventArgs e)
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