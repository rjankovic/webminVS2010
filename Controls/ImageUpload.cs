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
    /// Used by ImageField; uploads image to given directory and if is provided a fileName at initialization, displays its preview.
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
                //upload.ID = ID + "_Upload";
                //preview.ID = ID + "_Preview";
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
            /*
            this.imageFormat = imageFormat;
            this.mainPath = mainPath;
            this.thumbPath = thumbPath; // remain null => no thumbs
            this.fullWidth = fullWidth;
            this.thumbWidth = thumbWidth;
            this.nameFormat = nameFormat;
            this.fileName = fileName;
             */ 
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
                /*
                if (thumbPath != null)
                {
                    preview.ImageUrl = AppDomain.CurrentDomain.BaseDirectory + thumbPath + fileName;
                    preview.Width = Math.Min(150, thumbWidth);
                }
                else {
                    preview.ImageUrl = AppDomain.CurrentDomain.BaseDirectory + mainPath + fileName;
                    preview.Width = Math.Min(150, fullWidth);
                }*/
            }

        /*
        public void Save() {
            HttpPostedFile file = upload.PostedFile;
            SImage img = System.Drawing.Image.FromStream(file.InputStream);
            ImageConverter converter = new ImageConverter();
            SImage fullSize = img;
            if(fullWidth > 0)
                fullSize = Resize(img, fullWidth);
            SImage thumb = null;
            if (thumbPath != null) {
                thumb = img;
                if (thumbWidth > 0)
                {
                    thumb = Resize(img, thumbWidth);
                }
            }

            fileName = 
                (nameFormat == FileNameFormat.UnixTime ? "" : Regex.Replace(file.FileName, ".*//*", "").Substring(0, fileName.LastIndexOf(".")))
                + (nameFormat == FileNameFormat.UploadName ? "" : Functions.UnixTimestamp().ToString())
                + "." + Functions.GetFilenameExtension(imageFormat);

            fullSize.Save(AppDomain.CurrentDomain.BaseDirectory + mainPath + fileName, imageFormat);
            if (thumbPath != null) {
                thumb.Save(AppDomain.CurrentDomain.BaseDirectory + thumbPath + fileName, imageFormat);
            }
        }

        public int FileSize{
            get
            {
                //return 0;
                return upload.PostedFile.ContentLength;
            }
        }

        public ImageFormat GetFormat() {
            try
            {
                HttpPostedFile file = upload.PostedFile;
                SImage img = System.Drawing.Image.FromStream(file.InputStream);
                return new System.Drawing.Imaging.ImageFormat(img.RawFormat.Guid);
            }
            catch (Exception) {
                return null;
            }
        }

        private SImage Resize(SImage original, int width) {
            Graphics g = Graphics.FromImage(original);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            
            int newWidth = Math.Min(original.Width, width);
            int newHeight = original.Height * (newWidth / original.Width);
            
            g.FillRectangle(Brushes.White, 0, 0, newWidth, newHeight);
            SImage res = new System.Drawing.Bitmap(newWidth, newHeight);
            g.DrawImage(res, 0, 0, newWidth, newHeight);
            return res;
        }

        */
        
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