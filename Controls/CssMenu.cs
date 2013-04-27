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
/*
<div id="menuContainer">
<ul id="nav">
<li><a href="#nogo">Home</a></li>
<li><a href="#nogo">About us &#187;<!--[if gte IE 7]><!--></a><!--<![endif]-->
	<!--[if lte IE 6]><table><tr><td><![endif]--><ul>
		<li><a href="#nogo">Who we are</a></li>
		<li><a href="#nogo">What we do</a></li>
		<li><a href="#nogo">Where to find us</a></li>
	</ul><!--[if lte IE 6]></td></tr></table></a><![endif]-->
</li>
<li><a href="#nogo">Products &#187;<!--[if gte IE 7]><!--></a><!--<![endif]-->
	<!--[if lte IE 6]><table><tr><td><![endif]--><ul>
	<li><a href="#nogo">Tripods &#187;<!--[if gte IE 7]><!--></a><!--<![endif]-->
		<!--[if lte IE 6]><table><tr><td><![endif]--><ul>
			<li><a href="#nogo">Monopods</a></li>
			<li><a href="#nogo">Tripods</a></li>
			<li><a href="#nogo">Adjutable head</a></li>
			<li><a href="#nogo">Fixed</a></li>
			<li><a href="#nogo">Flash mount</a></li>
			<li><a href="#nogo">Floating head</a></li>
		</ul><!--[if lte IE 6]></td></tr></table></a><![endif]-->
	</li>
	<li><a href="#nogo">Films &#187;<!--[if gte IE 7]><!--></a><!--<![endif]-->
		<!--[if lte IE 6]><table><tr><td><![endif]--><ul>
			<li><a href="#nogo">35mm</a></li>
			<li><a href="#nogo">Color Print</a></li>
			<li><a href="#nogo">Black and White</a></li>
			<li><a href="#nogo">Roll</a></li>
			<li><a href="#nogo">Color Slide</a></li>
		</ul><!--[if lte IE 6]></td></tr></table></a><![endif]-->
	</li>
	<li><a href="#nogo">Cameras &#187;<!--[if gte IE 7]><!--></a><!--<![endif]-->
			<!--[if lte IE 6]><table><tr><td><![endif]--><ul>
			<li><a href="#nogo">Compact &#187;<!--[if gte IE 7]><!--></a><!--<![endif]-->
				<!--[if lte IE 6]><table><tr><td><![endif]--><ul>
				<li><a href="#nogo">Canon</a></li>
				<li><a href="#nogo">Nikon</a></li>
				<li><a href="#nogo">Minolta</a></li>
				<li><a href="#nogo">Pentax</a></li>
				</ul><!--[if lte IE 6]></td></tr></table></a><![endif]-->
			</li>
			<li><a href="#nogo">Digital &#187;<!--[if gte IE 7]><!--></a><!--<![endif]-->
					<!--[if lte IE 6]><table><tr><td><![endif]--><ul>
					<li><a href="#nogo">Canon</a></li>
					<li><a href="#nogo">Nikon &#187;<!--[if gte IE 7]><!--></a><!--<![endif]-->
							<!--[if lte IE 6]><table><tr><td><![endif]--><ul>
							<li><a href="#nogo">Lenses &#187;<!--[if gte IE 7]><!--></a><!--<![endif]-->
								<!--[if lte IE 6]><table><tr><td><![endif]--><ul>
									<li><a href="#nogo">Standard</a></li>
									<li><a href="#nogo">Telephoto</a></li>
									<li><a href="#nogo">Wide Angle</a></li>
									<li><a href="#nogo">Fish Eye</a></li>
									<li><a href="#nogo">Mirror</a></li>
									<li><a href="#nogo">Macro</a></li>
								</ul><!--[if lte IE 6]></td></tr></table></a><![endif]-->
							</li>
							<li><a href="#nogo">Speedlight</a></li>
							<li><a href="#nogo">Coolpix &#187;<!--[if gte IE 7]><!--></a><!--<![endif]-->
									<!--[if lte IE 6]><table><tr><td><![endif]--><ul>
									<li><a href="#nogo">Coolpix S10</a></li>
									<li><a href="#nogo">Coolpix L2</a></li>
									<li><a href="#nogo">Coolpix S500</a></li>
									<li><a href="#nogo">Coolpix P5000</a></li>
									<li><a href="#nogo">Coolpix 4600</a></li>
									<li><a href="#nogo">Coolpix S6 Silver</a></li>
									</ul><!--[if lte IE 6]></td></tr></table></a><![endif]-->
							</li>
							<li><a href="#nogo">D200</a></li>
							<li><a href="#nogo">D80</a></li>
							</ul><!--[if lte IE 6]></td></tr></table></a><![endif]-->
					</li>
					<li><a href="#nogo">Minolta</a></li>
					<li><a href="#nogo">Pentax</a></li>
					</ul><!--[if lte IE 6]></td></tr></table></a><![endif]-->
			</li>
			<li><a href="#nogo">SLR &#187;<!--[if gte IE 7]><!--></a><!--<![endif]-->
				<!--[if lte IE 6]><table><tr><td><![endif]--><ul>
				<li><a href="#nogo">Canon</a></li>
				<li><a href="#nogo">Nikon</a></li>
				<li><a href="#nogo">Minolta</a></li>
				<li><a href="#nogo">Pentax</a></li>
				<li><a href="#nogo">Panasonic</a></li>
				</ul><!--[if lte IE 6]></td></tr></table></a><![endif]-->
			</li>
			</ul><!--[if lte IE 6]></td></tr></table></a><![endif]-->
		</li>
	<li><a href="#nogo">Flash</a></li>
	<li><a href="#nogo">Video</a></li>
	</ul><!--[if lte IE 6]></td></tr></table></a><![endif]-->
	</li>
<li><a href="#nogo">FAQs &#187;<!--[if gte IE 7]><!--></a><!--<![endif]-->
	<!--[if lte IE 6]><table><tr><td><![endif]--><ul>
		<li><a href="#nogo">Cameras</a></li>
		<li><a href="#nogo">Film types</a></li>
		<li><a href="#nogo">Digital Photography</a></li>
	</ul><!--[if lte IE 6]></td></tr></table></a><![endif]-->
</li>
<li><a href="#nogo">Privacy &#187;<!--[if gte IE 7]><!--></a><!--<![endif]-->
	<!--[if lte IE 6]><table><tr><td><![endif]--><ul>
		<li><a href="#nogo">Privacy Policy</a></li>
		<li><a href="#nogo">Privacy Statement</a></li>
	</ul><!--[if lte IE 6]></td></tr></table></a><![endif]-->
</li>
<li><a href="#nogo">Contact us</a></li>
</ul>
</div>*/