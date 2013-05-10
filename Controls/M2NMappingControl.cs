using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Web.UI.WebControls;
using System.ComponentModel;
using System.Web.UI;

namespace _min.Controls
{
    /// <summary>
    /// A WebControl for the management of an associtive / mapping table consisting of two select boxes, which exchange items upon select.
    /// This is achieved through jquery, the control itself onsly sets the initial values and retrieves the selection upon request.
    /// </summary>
    [ToolboxData("<{0}:M2NMappingControl></{0}:M2NMappingControl>")]
    public class M2NMappingControl : CompositeControl
    {
        private ListBox inList = new ListBox();
        private ListBox outList = new ListBox();
        private string _ID;

        public override string ID {
            get { return _ID; }
            set { _ID = value; }
        }

        /// <summary>
        /// set all the options, initially are all "excluded"
        /// </summary>
        /// <param name="vals"></param>
        public void SetOptions(IDictionary<int, string> vals){
            outList.DataSource = vals;
            outList.DataTextField = "Value";
            outList.DataValueField = "Key";
            EnsureChildControls();
            inList.DataBind();
            outList.DataBind();
        }

        public void SetOptions(List<string> vals)
        {
            outList.DataSource = vals;
            EnsureChildControls();
            inList.DataBind();
            outList.DataBind();
        }

        /// <summary>
        /// set all options to "excluded"
        /// </summary>
        private void ResetIncluded() {
            foreach (ListItem item in inList.Items)
            {
                outList.Items.Add(item);
            }
            inList.Items.Clear();
        }

        public void SetIncludedOptions(List<string> included)
        {
            ResetIncluded();
            foreach (string s in included)
            {
                ListItem item = outList.Items.FindByText(s.ToString());
                inList.Items.Add(item);
                outList.Items.Remove(item);
            }
        }

        public void SetIncludedOptions(List<int> included) {

            ResetIncluded();
            foreach (int i in included) {
                ListItem item = outList.Items.FindByValue(i.ToString());
                if (item == null) continue;
                inList.Items.Add(item);
                outList.Items.Remove(item);
            }
        }

        public List<int> RetrieveData() {
            
            string results = Page.Request.Form[inList.UniqueID];
            List<int> res = new List<int>();
            if (results == null) return res;
            foreach(string item in results.Split(',')){
                res.Add(Int32.Parse(item));
            }
            return res;
        }

        public List<string> RetrieveStringData() {
            string results = Page.Request.Form[inList.UniqueID];
            if (results == null) return new List<string>();
            return new List<string>(results.Split(','));
        }

        protected override void CreateChildControls()
        {
            inList.SelectionMode = ListSelectionMode.Multiple;
            outList.SelectionMode = ListSelectionMode.Multiple;
            inList.ClientIDMode = System.Web.UI.ClientIDMode.Static;
            outList.ClientIDMode = System.Web.UI.ClientIDMode.Static;
            inList.ID = this.ID + "_M2NIN";
            outList.ID = this.ID + "_M2NOUT";
            inList.AutoPostBack = false;
            outList.AutoPostBack = false;
            inList.EnableViewState = false;
            outList.EnableViewState = false;
            this.Controls.Clear();
            this.Controls.Add(inList);
            this.Controls.Add(outList);
        }

        
        protected override void Render(HtmlTextWriter writer)
        {
            
            AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "10", false);
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            inList.AutoPostBack = false;
            outList.AutoPostBack = false;
            inList.CssClass = "noShrink";
            outList.CssClass = "noShrink";
            inList.Attributes.Add("title", "Included items");
            outList.Attributes.Add("title", "Excluded items");
            //inList.Attributes.Add("runat", "server");
            inList.RenderControl(writer);
            writer.RenderEndTag();
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            //outList.Attributes.Add("runat", "server");
            outList.RenderControl(writer);
            writer.RenderEndTag();
            writer.RenderEndTag();
            writer.RenderEndTag();
        }
    }
}