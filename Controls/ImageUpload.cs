using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Web.UI.WebControls;
using System.ComponentModel;
using System.Web.UI;
using _min.Models;
using MPanel = _min.Models.Panel;
using WC = System.Web.UI.WebControls;
using System.Data;
using _min.Common;
using System.Drawing;
using System.Drawing.Imaging;
using SImage = System.Drawing.Image;
using System.Text.RegularExpressions;


namespace _min.Controls
{
    /// <summary>
    /// Used by ImageField
    /// </summary>
    [ToolboxData("<{0}:TreeNavigator runat=server></{0}:ImageUpload>")]
    public class ImageUploadControl : CompositeControl
    {
        private string fileName;
        private WC.FileUpload upload;
        private WC.Image preview;
        public string path;


        public string FileName {
            get { return fileName; }
            set { fileName = value; }
        }


        private string _ID;
        public override string ID
        {
            get { 
                return _ID; 
            }
            set { 
                _ID = value;
            }
        }


        protected override void CreateChildControls()
        {
            this.Controls.Clear();
            this.upload = new FileUpload();
            this.preview = new WC.Image();
            upload.ID = ID + "_Upload";
            preview.ID = ID + "_Preview";
            if (fileName != null){
                preview = new WC.Image();
                preview.Width = 150;
                preview.ImageUrl = "/" +  path + fileName;
            }
            this.Controls.Add(upload);
            this.Controls.Add(preview);
        }

        public ImageUploadControl(string path, string fileName = null) {
            this.path = path;
            if (this.fileName != null) {
                DisplayImage();
            }
        }

        public HttpPostedFile PostedFile {
            get
            {
                return upload.PostedFile;
            }
        }

        public void DisplayImage(){
                CreateChildControls();
            }

        protected override void Render(HtmlTextWriter writer)
        {
            AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "10", false);
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            upload.Attributes.Add("runat", "server");
            upload.RenderControl(writer);
            writer.RenderEndTag();
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            preview.Attributes.Add("runat", "server");
            preview.RenderControl(writer);
            writer.RenderEndTag();
            writer.RenderEndTag();
            
            writer.RenderEndTag();
        }
    }
}