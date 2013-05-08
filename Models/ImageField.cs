using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Serialization;
using System.IO;

using _min.Interfaces;
using _min.Common;
using _min.Controls;
using UControl = System.Web.UI.WebControls.WebControl;
using WC = System.Web.UI.WebControls;
using System.Web;
using System.Drawing.Imaging;
using CE = _min.Common.Environment;
using SImage = System.Drawing.Image;
using System.Drawing;
using System.Text.RegularExpressions;

namespace _min.Models
{
    [DataContract]
    public class ImageField : ColumnField
    {

        public static IEnumerable<Type> GetKnownTypes()
        {
            return CE.GetKnownTypes();
        }

        [IgnoreDataMember]
        private ImageUploadControl myControl;
        [DataMember]
        private _min.Common.TargetImageFormat targetFormat = TargetImageFormat.JPG;
        [IgnoreDataMember]
        private HttpPostedFile postedFile;

        public _min.Common.TargetImageFormat TargetFormat
        {
            get { return targetFormat; }
            set { targetFormat = value; }
        }
        [DataMember]
        private string mainDirectory = null;

        public string MainDirectory
        {
            get { return mainDirectory; }
            set { mainDirectory = value; }
        }
        [DataMember]
        private string thumbDirectory = null;

        public string ThumbDirectory
        {
            get { return thumbDirectory; }
            set { thumbDirectory = value; }
        }
        [DataMember]
        private _min.Common.FileNameFormat nameFormat = FileNameFormat.Both;

        public _min.Common.FileNameFormat NameFormat
        {
            get { return nameFormat; }
            set { nameFormat = value; }
        }
        [DataMember]
        private int fullWidth = 0;

        public int FullWidth
        {
            get { return fullWidth; }
            set { fullWidth = value; }
        }
        [DataMember]
        private int thumbWidth = 120;

        public int ThumbWidth
        {
            get { return thumbWidth; }
            set { thumbWidth = value; }
        }

        private string TargetFileExtension(TargetImageFormat fomrat){
            switch (fomrat)
            {
                case TargetImageFormat.JPG:
                    return "jpeg";
                case TargetImageFormat.PNG:
                    return "png";
                default:
                    throw new FormatException();
            }
        }

        private Dictionary<Common.TargetImageFormat, ImageFormat> formatEnumDecoder;


        private string fileName;
        public override UControl MyControl
        {
            get
            {
                if (myControl == null)
                {
                    
                    formatEnumDecoder = new Dictionary<TargetImageFormat, ImageFormat>
                    {
                        { TargetImageFormat.JPG , ImageFormat.Jpeg },
                        { TargetImageFormat.PNG, ImageFormat.Png}
                    };
                      
                    myControl = new ImageUploadControl(mainDirectory, fileName);
                    myControl.ID = "Field" + this.FieldId;
                }
                return myControl;
            }
        }

        public ImageField(
            string columnName, string caption) 
        :base(columnName, caption)
        { }

        public override void Validate()
        {
            ErrorMessage = null;

            if (postedFile.ContentLength == 0) {
                if (Required && fileName == null) { // required && this is not an update - really need the file 
                    ErrorMessage = "Please upload an image in the field " + Caption;
                }
                // no file => no more processing, just set the fileName to null as the retrieved data for the DB
                fileName = null;
                return;
            }
            SImage img;
            try
            {
                img = SImage.FromStream(postedFile.InputStream);
            }
            catch (Exception) {
                ErrorMessage = "Please, upload an image";
                return;
            }
            var converter = new System.Drawing.ImageConverter();
            SImage fullSize = img;
            if (fullWidth > 0)
                fullSize = Resize(img, fullWidth);
            SImage thumb = null;
            if (thumbDirectory != null)
            {
                thumb = img;
                if (thumbWidth > 0)
                {
                    thumb = Resize(img, thumbWidth);
                }
            }

            if (postedFile.ContentLength > 2020000)
            {
                ErrorMessage = "The file must not exceed 2 MB";
                return;
            }
            ImageFormat format = img.RawFormat;
            /*
            if (format == null || (format != ImageFormat.Jpeg && format != ImageFormat.Png && format != ImageFormat.Bmp && format != ImageFormat.Gif))
            {
                ErrorMessage = "Only JPG, PNG, BMP and GIF images are allowed";
                return;
            }*/
            try
            {
            /*
                fileName =
                (nameFormat == FileNameFormat.UnixTime ? "" : postedFile.FileName.Substring(0, postedFile.FileName.LastIndexOf(".")))
                + (nameFormat == FileNameFormat.UploadName ? "" : Functions.UnixTimestamp().ToString().Replace(",", ""))
                + "." + Functions.GetFilenameExtension(formatEnumDecoder[targetFormat]);
            */
                fullSize.Save(AppDomain.CurrentDomain.BaseDirectory + mainDirectory + fileName, formatEnumDecoder[targetFormat]);
                if (thumbDirectory != null)
                {
                    thumb.Save(AppDomain.CurrentDomain.BaseDirectory + thumbDirectory + fileName, formatEnumDecoder[targetFormat]);
                }
      
            }
            catch (Exception e)
            {
                ErrorMessage = "Could not upload the file - " + e.Message;    
            }
        }

        private bool ThumbnailCallback() {
            return false;
        }

        private SImage Resize(SImage original, int width)
        {
            /*
            Graphics g = Graphics.FromImage(original);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            */
            Image.GetThumbnailImageAbort callback = new Image.GetThumbnailImageAbort(ThumbnailCallback);
            int newWidth = Math.Min(original.Width, width);
            int newHeight = (int)(original.Height * ((float)newWidth / (float)(original.Width)));
            return original.GetThumbnailImage(newWidth, newHeight, callback, IntPtr.Zero);
            /*
            g.FillRectangle(Brushes.White, 0, 0, newWidth, newHeight);
            SImage res = new System.Drawing.Bitmap(newWidth, newHeight);
            g.DrawImage(res, 0, 0, newWidth, newHeight);
            return res;
             */ 
        }

        public override void RetrieveData()
        {
            //this.fileName = myControl.FileName;
            postedFile = myControl.PostedFile;
            if (postedFile.ContentLength > 0)
            {
                fileName =
    (nameFormat == FileNameFormat.UnixTime ? "" : postedFile.FileName.Substring(0, postedFile.FileName.LastIndexOf(".")))
    + (nameFormat == FileNameFormat.UploadName ? "" : Functions.UnixTimestamp().ToString().Replace(",", ""))
    + "." + TargetFileExtension(targetFormat);
            }
        }

        public override void FillData()
        {
            myControl.FileName = fileName;
            myControl.DisplayImage();
        }

        public override void InventData()
        {

        }

        public override object Data
        {
            get
            {
                return fileName;

            }
            set
            {
                if (value == null || value == DBNull.Value)
                {
                    fileName = null;
                    return;
                }
                if (!(value is string))
                {
                    throw new ArgumentException("The file name of the image must be a string");
                }
                fileName = value as string;
            }
        }

        public override List<WC.BaseValidator> GetValidators()
        {
            return new List<WC.BaseValidator>();
        }
    }


    public class ImageFieldFactory : ICustomizableColumnFieldFactory
    {

        private WC.TextBox mainPathBox = new WC.TextBox();
        private WC.TextBox thumbPathBox = new WC.TextBox();
        private WC.TextBox fullWidthBox = new WC.TextBox();
        private WC.TextBox thumbWidthBox = new WC.TextBox();
        private WC.CheckBox useThumbsCheck = new WC.CheckBox();
        private WC.RadioButtonList targetFormatRadios = new WC.RadioButtonList();
        
        private string errorMessage = null;

        private string mainPath;
        private string thumbPath;
        private int fullWidth;
        private int thumbWidth;
        private bool useThubs;
        private TargetImageFormat targetFormat = TargetImageFormat.JPG;

        public void ShowForm(WC.Panel panel)
    {
            
        WC.Table formTbl = new WC.Table();
        WC.TableCell mainPathLabel = new WC.TableCell();
        mainPathLabel.Text = "Full size image directory";
        WC.TableCell thumbPathLabel = new WC.TableCell();
        thumbPathLabel.Text = "Thumbnail image directory";
        WC.TableCell mainWidthLabel = new WC.TableCell();
        mainWidthLabel.Text = "Maximum width for the main image";
        WC.TableCell thumbWidthLabel = new WC.TableCell();
        thumbWidthLabel.Text = "Maximum width for the thumbnail";
        WC.TableCell useThumbsLabel = new WC.TableCell();
        useThumbsLabel.Text = "Use thumbnails";
        WC.TableCell targetFormatLabel = new WC.TableCell();
        targetFormatLabel.Text = "Convert images to format";
        
        WC.TableCell mainPathCell = new WC.TableCell();
        mainPathCell.Controls.Add(mainPathBox);
        WC.TableCell thumbPathCell = new WC.TableCell();
        thumbPathCell.Controls.Add(thumbPathBox);
        WC.TableCell mainWidthCell = new WC.TableCell();
        mainWidthCell.Controls.Add(fullWidthBox);
        WC.TableCell thumbWidthCell = new WC.TableCell();
        thumbWidthCell.Controls.Add(thumbWidthBox);
        WC.TableCell useThumbsCell = new WC.TableCell();
        useThumbsCell.Controls.Add(useThumbsCheck);
        WC.TableCell targetFormatCell = new WC.TableCell();
        targetFormatCell.Controls.Add(targetFormatRadios);

        WC.TableRow r1 = new WC.TableRow();
        r1.Cells.Add(mainPathLabel);
        r1.Cells.Add(mainPathCell);
        formTbl.Rows.Add(r1);

        WC.TableRow r2 = new WC.TableRow();
        r2.Cells.Add(thumbPathLabel);
        r2.Cells.Add(thumbPathCell);
        formTbl.Rows.Add(r2);

        WC.TableRow r3 = new WC.TableRow();
        r3.Cells.Add(mainWidthLabel);
        r3.Cells.Add(mainWidthCell);
        formTbl.Rows.Add(r3);

        WC.TableRow r4 = new WC.TableRow();
        r4.Cells.Add(thumbWidthLabel);
        r4.Cells.Add(thumbWidthCell);
        formTbl.Rows.Add(r4);

        WC.TableRow r5 = new WC.TableRow();
        r5.Cells.Add(useThumbsLabel);
        r5.Cells.Add(useThumbsCell);
        formTbl.Rows.Add(r5);

        WC.TableRow r6 = new WC.TableRow();
        r6.Cells.Add(targetFormatLabel);
        r6.Cells.Add(targetFormatCell);
        formTbl.Rows.Add(r6);

        targetFormatRadios.DataSource = Enum.GetValues(typeof(TargetImageFormat));
        targetFormatRadios.DataBind();

        panel.Controls.Add(formTbl);
    }

        public void LoadProduct(IColumnField field)
        {
            if (!(field is ImageField))
                throw new ArgumentException();
            ImageField imf = (ImageField)field;
            mainPath = imf.MainDirectory;
            thumbPath = imf.ThumbDirectory;
            fullWidth = imf.FullWidth;
            thumbWidth = imf.ThumbWidth;
            targetFormat = imf.TargetFormat;
            useThubs = thumbPath != null;
            FillFields();
        }

        private void FillFields(){
            mainPathBox.Text = mainPath;
            thumbPathBox.Text = thumbPath;
            fullWidthBox.Text = fullWidth.ToString();
            thumbWidthBox.Text = thumbWidth.ToString();
            useThumbsCheck.Checked = useThubs;
            targetFormatRadios.SelectedIndex = (int)targetFormat;
        }

        public void UpdateProduct(IColumnField field)
        {
            if (!(field is ImageField))
                throw new ArgumentException();
            ImageField imf = (ImageField)field;
            imf.MainDirectory = mainPath;
            imf.ThumbDirectory = thumbPath;
            imf.FullWidth = fullWidth;
            imf.ThumbWidth = thumbWidth;
        }

        public bool CanHandle(DataColumn column)
        {
            return column.DataType == typeof(string) && column.MaxLength > 50;
        }

        public _min.Models.ColumnField Create(DataColumn column)
        {
            ImageField imf = new ImageField(column.ColumnName, column.ColumnName);
            imf.MainDirectory = mainPath;
            imf.ThumbDirectory = thumbPath;
            imf.FullWidth = fullWidth;
            imf.ThumbWidth = thumbWidth;
            imf.TargetFormat = targetFormat;
            return imf;
        }

        public Type ProductionType
        {
            get { return typeof(ImageField); }
        }

        public string UIName
        {
            get { return "Image upload"; }
        }

        public FieldSpecificityLevel Specificity
        {
            get { return FieldSpecificityLevel.Medium; }
        }

        public void ValidateForm()
        {
            errorMessage = null;
            int fw = 0;
            int tw = 0;
            string mp = null;
            string tp = null;
            TargetImageFormat tf;
            
            try
            {
                if(String.IsNullOrEmpty(mainPathBox.Text)){
                    throw new Exception("Please, fill in the main direcotry for the images.");
                }
                DirectoryInfo mainDirInfo = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + mainPathBox.Text);
                if (!mainDirInfo.Exists) {
                    throw new Exception("The directory specified as the storage of full-size images (" + mainDirInfo.FullName  + ") does not exist");
                }
                mp = mainPathBox.Text;
                if (!mp.EndsWith("/")) mp += "/";
                fw = 0;
                if(!string.IsNullOrEmpty(fullWidthBox.Text)) fw = Int32.Parse(fullWidthBox.Text);
                if (useThumbsCheck.Checked) {
                    DirectoryInfo thumbDirInfo = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + thumbPathBox.Text);
                    if (!thumbDirInfo.Exists)
                    {
                        throw new Exception("The directory specified as the storage of thumbnail images (" + thumbDirInfo.FullName + ") does not exist");
                    }
                    tp = thumbPathBox.Text;
                    if (!mp.EndsWith("/")) mp += "/";
                    tw = 0;
                    if(thumbWidthBox.Text != "") tw = Int32.Parse(thumbWidthBox.Text);
                }
                if (targetFormatRadios.SelectedIndex < 0)
                    throw new Exception("Please, specify the format into which to convert the images");
                tf = (TargetImageFormat)Enum.Parse(typeof(TargetImageFormat), targetFormatRadios.SelectedValue);
                
            }
            catch (Exception e) {
                errorMessage = e.Message;
                return;
            }

            // will get here only if the validation succeeds
            mainPath = mp;
            thumbPath = tp;
            fullWidth = fw;
            thumbWidth = tw;
            targetFormat = tf;
        }

        public string ErrorMessage
        {
            get { return errorMessage; }
        }

        public object Clone()
        {
            return new ImageFieldFactory();
        }
    }
}