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
    /// 
    /// Based on http://www.cssplay.co.uk/menus/skeleton3.html
    /// </summary>
    [ToolboxData("<{0}:CssMenu runat=\"server\"></{0}:CssMenu>")]
    public class CssMenu : Menu
    {
        protected override void Render(HtmlTextWriter writer)
        {
            
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "cssMenuContainer " + this.CssClass);
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "cssMenuNav");
            writer.RenderBeginTag(HtmlTextWriterTag.Ul);
            foreach(MenuItem item in Items)
                RenderChild(item, writer);
            writer.RenderEndTag();
            writer.RenderEndTag();
            
        }

        private void RenderChild(MenuItem item, HtmlTextWriter writer){
            writer.RenderBeginTag(HtmlTextWriterTag.Li);
            writer.AddAttribute(HtmlTextWriterAttribute.Href, 
                (item.NavigateUrl == String.Empty) ? ("#") : (item.NavigateUrl));
            writer.RenderBeginTag(HtmlTextWriterTag.A);
            writer.Write(item.Text);
            writer.RenderEndTag();
            if(item.ChildItems.Count > 0){
                writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                foreach(MenuItem subItem in item.ChildItems)
                    RenderChild(subItem, writer);
                writer.RenderEndTag();
            }
            writer.RenderEndTag();
        }
    }
}