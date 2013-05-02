using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Runtime.Serialization;
using System.IO;
using System.Text.RegularExpressions;

using _min.Interfaces;
using _min.Common;
using _min.Controls;
using UControl = System.Web.UI.WebControls.WebControl;
using WC = System.Web.UI.WebControls;
using System.Web;
using CC = _min.Common.Constants;

using NLipsum.Core;

namespace _min.Models
{
    [KnownType("GetKnownTypes")]
    [DataContract]
    public abstract class FieldBase : IField
    {
        static IEnumerable<Type> GetKnownTypes() {
            var factories = (IEnumerable<IColumnFieldFactory>)HttpContext.Current.Application["ColumnFieldFactories"];
            List<Type> types = new List<Type>();
            foreach (IColumnFieldFactory factory in factories)
                types.Add(factory.ProductionType);

            types.Add(typeof(M2NMappingField));
            types.Add(typeof(ColumnField));
            return types;
        }

        [IgnoreDataMember]
        public abstract System.Web.UI.WebControls.WebControl MyControl { get; }
        [IgnoreDataMember]
        public int? FieldId { get; private set; }       // better setter once
        public void SetId(int id)
        {
            if (FieldId == null)
                FieldId = id;
            else
                throw new DataException("The field id has been set.");
        }
        [IgnoreDataMember]
        private Panel _panel;
        public Panel Panel
        {
            get
            {
                return _panel;
            }
            set
            {
                if (_panel == null)
                {
                    _panel = value;
                    PanelId = value.panelId;
                }
                else throw new Exception("Panel already set");
            }
        }
        [DataMember]
        public int? PanelId { get; private set; }
        [DataMember]
        public string Caption { get; set; }
        [DataMember]
        public bool Required { get; set; }
        [IgnoreDataMember]
        public string ErrorMessage { get; protected set; }

        public void RefreshPanelId() {
            PanelId = Panel.panelId;
        }

        public abstract void Validate();

        public virtual List<System.Web.UI.WebControls.BaseValidator> GetValidators()
        {
            List<WC.BaseValidator> res = new List<WC.BaseValidator>();
            if (Required)
            {
                WC.RequiredFieldValidator reqVal;
                reqVal = new WC.RequiredFieldValidator();
                reqVal.ControlToValidate = MyControl.ID;
                reqVal.ErrorMessage = this.Caption + " is required";
                reqVal.Display = WC.ValidatorDisplay.None;
                res.Add(reqVal);
            }
            return res;
        }

        public abstract void RetrieveData();

        public abstract void FillData();

        public abstract void InventData();

        public virtual string Serialize()
        {
            MemoryStream ms = new MemoryStream();
            DataContractSerializer ser = new DataContractSerializer(typeof(FieldBase));
            ser.WriteObject(ms, this);

            return Functions.StreamToString(ms);
        }

        public FieldBase(string caption)
        {
            this.Caption = caption;
        }
    

        public abstract object  Data {get; set;}
}
    [DataContract]
    public abstract class ColumnField : FieldBase, IColumnField {
        [DataMember]
        public string ColumnName { get; protected set; }
        [DataMember]
        public bool Unique { get; set; }

        public ColumnField(string columnName, string caption)
            :base(caption)
        {
            this.ColumnName = columnName;
        }
        
    }

    [DataContract]
    public class TextField : ColumnField
    {

        protected WC.TextBox myControl;
        protected string value;

        public override UControl MyControl
        {
            get
            {
                if (myControl == null)
                {
                    myControl = new WC.TextBox();
                    myControl.ID = "Field" + FieldId;
                    myControl.ClientIDMode = System.Web.UI.ClientIDMode.Static;
                }
                return myControl;
            }
        }

        public override void Validate()
        {
            if (string.IsNullOrEmpty(value))
                ErrorMessage = "The " + Caption + " is required.";
        }

        public override void RetrieveData()
        {
            value = myControl.Text;
        }

        public override void FillData()
        {
            myControl.Text = value;
        }
        public override void InventData()
        {
            this.value = NLipsum.Core.LipsumGenerator.Generate
                (1, NLipsum.Core.Features.Sentences, "{0}", NLipsum.Core.Lipsums.RobinsonoKruso);
        }

        public override object Data
        {
            get
            {
                return value;
            }
            set
            {
                if (value == null) {
                    this.value = null; 
                    return;
                }

                if (!(value is string))
                    throw new FormatException("A textbox can be only assigned text data");
                this.value = value as string;
            }
        }

        public TextField(string columnName, string caption)
            : base(columnName, caption)
        { }
    }


    public class TextFieldFactory : IColumnFieldFactory
    {

        public virtual string UIName
        {
            get
            {
                return "TextBox";
            }
        }

        public Type ProductionType {
            get {
                return typeof(TextField);
            }
        }

        public FieldSpecificityLevel Specificity {
            get {
                return FieldSpecificityLevel.Low;
            }
        }
        
        public virtual bool CanHandle(DataColumn column)
        {
            return column.DataType == typeof(string);
        }

        public virtual ColumnField Create(DataColumn column)
        {
            return new TextField(column.ColumnName, column.ColumnName);
        }
    }

    [DataContract]
    public class TextEditorField : TextField
    {
        public override UControl MyControl
        {
            get
            {
                WC.TextBox tb = (WC.TextBox)base.MyControl;
                tb.Width = 500;
                tb.Height = 250;
                tb.CssClass = "includeEditor";
                return tb;
            }
        }

        public override void InventData()
        {
            value = NLipsum.Core.LipsumGenerator.GenerateHtml(3);
        }

        public TextEditorField(string columnName, string caption)
            : base(columnName, caption)

        { }
    }

    public class TextEditorFieldFactory : IColumnFieldFactory
    {
        public string UIName
        {
            get
            {
                return "Text Editor";
            }
        }
        public FieldSpecificityLevel Specificity
        {
            get
            {
                return FieldSpecificityLevel.Medium;
            }
        }
        public Type ProductionType
        {
            get
            {
                return typeof(TextEditorField);
            }
        }
        public bool CanHandle(DataColumn column)
        {
            return column.DataType == typeof(string) && column.MaxLength > 255;
        }

        public ColumnField Create(DataColumn column)
        {
            return new TextEditorField(column.ColumnName, column.ColumnName);
        }

    }



    [DataContract]
    public class DateField : ColumnField
    {
        protected WC.TextBox myControl;
        private DateTime? value = null;
        private string unparsedValue;



        public override void Validate()
        {
            if (String.IsNullOrEmpty(unparsedValue))
            {
                if (Required)
                    ErrorMessage = "Please fill in the date in " + Caption;
            }
            else
            {
                if (value == DateTime.MinValue)
                    ErrorMessage = "The date in " + Caption + " is not well-formatted";
            }
        }


        public override void RetrieveData()
        {
            unparsedValue = myControl.Text;
            DateTime res;
            if (DateTime.TryParse(unparsedValue, out res))
                value = res;
        }

        public override void FillData()
        {
            if (value != null)
                myControl.Text = ((DateTime)value).ToShortDateString();
        }

        public override void InventData()
        {
            value = DateTime.Now;
        }

        public override object Data
        {
            get
            {
                return value;
            }
            set
            {
                if (value == null || value == DBNull.Value)
                {
                    this.value = null;
                    return;
                }
                if (!(value is DateTime || (value is MySql.Data.Types.MySqlDateTime
                    && ((MySql.Data.Types.MySqlDateTime)value).IsValidDateTime)))
                    throw new FormatException("A date filed can be only assigned a date");
                if (value is DateTime)
                    this.value = (DateTime)value;
                else
                    this.value = ((MySql.Data.Types.MySqlDateTime)value).GetDateTime();
            }
        }

        public override UControl MyControl
        {
            get
            {
                if (myControl == null)
                {
                    myControl = new WC.TextBox();
                    myControl.ID = "Field" + FieldId;
                    myControl.ClientIDMode = System.Web.UI.ClientIDMode.Static;
                    myControl.CssClass = "includeDatePicker";
                }
                return myControl;
            }
        }

        public DateField(string columnName, string caption)
            :base(columnName, caption)
        { }
    }

    public class DateFieldFactory : IColumnFieldFactory
    {
        public string UIName
        {
            get
            {
                return "Date Field";
            }
        }

        public FieldSpecificityLevel Specificity
        {
            get
            {
                return FieldSpecificityLevel.High;
            }
        }

        public virtual Type ProductionType
        {
            get
            {
                return typeof(DateField);
            }
        }

        public bool CanHandle(DataColumn column)
        {
            return column.DataType == typeof(DateTime) || column.DataType == typeof(MySql.Data.Types.MySqlDateTime);
        }

        public ColumnField Create(DataColumn column)
        {
            return new DateField(column.ColumnName, column.ColumnName);
        }
    }





    [DataContract]
    public class CheckboxField : ColumnField
    {
        protected WC.CheckBox myControl;
        private bool value = false;


        public override void Validate()
        {
            // should not be used
            if (Required && value == false)
                ErrorMessage = "You must check the option of " + Caption;
        }

        public override List<WC.BaseValidator> GetValidators()
        {
            return new List<WC.BaseValidator>();
        }


        public override void RetrieveData()
        {
            value = myControl.Checked;
        }

        public override void FillData()
        {
            myControl.Checked = value;
        }

        public override void InventData()
        {

        }


        public override object Data
        {
            get
            {
                return value;
            }
            set
            {
                if (value == null || value == DBNull.Value)
                {
                    this.value = false;
                    return;
                }
                if (!(value is bool))
                    throw new FormatException("A checkbox filed can be only assigned boolean data");
                this.value = (bool)value;
            }
        }

        public override UControl MyControl
        {
            get
            {
                if (myControl == null)
                {
                    myControl = new WC.CheckBox();
                    myControl.ID = "Field" + FieldId;
                    myControl.ClientIDMode = System.Web.UI.ClientIDMode.Static;
                }
                return myControl;
            }
        }

        public CheckboxField(string columnName, string caption)
            :base(columnName, caption)
        {
        }
    }



    public class Checkboxactory : IColumnFieldFactory
    {
        public string UIName
        {
            get
            {
                return "Checkbox";
            }
        }

        public FieldSpecificityLevel Specificity
        {
            get
            {
                return FieldSpecificityLevel.High;
            }
        }

        public virtual Type ProductionType
        {
            get
            {
                return typeof(CheckboxField);
            }
        }

        public bool CanHandle(DataColumn column)
        {
            return column.DataType == typeof(bool) || column.DataType == typeof(byte) || column.DataType == typeof(sbyte);
        }

        public ColumnField Create(DataColumn column)
        {
            return new CheckboxField(column.ColumnName, column.ColumnName);
        }
    }



    [DataContract]
    public class FKField : ColumnField
    {
        protected WC.DropDownList myControl;
        private int? value = null;


        public override void Validate()
        {

        }

        public override List<WC.BaseValidator> GetValidators()
        {
            return new List<WC.BaseValidator>();
        }
        [DataMember]
        public FK FK { get; private set; }
        [IgnoreDataMember]
        public SortedDictionary<int, string> FKOptions { get; set; }


        public override void RetrieveData()
        {
            value = Int32.Parse(myControl.SelectedValue);
        }

        public override void FillData()
        {
            myControl.SelectedIndex = myControl.Items.IndexOf(myControl.Items.FindByValue(value.ToString()));
        }

        public override void InventData()
        {
            FKOptions = new SortedDictionary<int, string>();
            Random rnd = new Random();
            int a = rnd.Next() % 5 + 5;
            string[] res = new string[a];
            for (int i = 0; i < a; i++)
                FKOptions.Add(i, NLipsum.Core.LipsumGenerator.Generate(1, NLipsum.Core.Features.Words, "{0}", NLipsum.Core.Lipsums.Faust));
        }

        public override object Data
        {
            get
            {
                return value;
            }
            set
            {
                if (value == null || value == DBNull.Value)
                {
                    this.value = null;
                    return;
                }
                if (!(value is int))
                    throw new FormatException("A foreing key can only be assigned from integer values");
                this.value = (Int32)value;
            }
        }

        public override UControl MyControl
        {
            get
            {
                if (myControl == null)
                {
                    myControl = new WC.DropDownList();
                    myControl.ID = "Field" + FieldId;
                    myControl.ClientIDMode = System.Web.UI.ClientIDMode.Static;
                    if (!Required)
                        FKOptions.Add(int.MinValue, "");
                    myControl.DataSource = FKOptions;
                    myControl.DataTextField = "Value";
                    myControl.DataValueField = "Key";
                    myControl.DataBind();
                }
                return myControl;
            }
        }

        public FKField(string columnName, string caption, FK FK)
        :base(columnName, caption)
        {
            this.FK = FK;
        }
    }



    public class FKFieldFactory : IColumnFieldFactory
    {
        public virtual string UIName
        {
            get
            {
                return "FK select";
            }
        }

        public FieldSpecificityLevel Specificity
        {
            get
            {
                return FieldSpecificityLevel.High;
            }
        }

        public virtual Type ProductionType
        {
            get
            {
                return typeof(FKField);
            }
        }

        public bool CanHandle(DataColumn column)
        {
            return column.DataType == typeof(bool) || column.ExtendedProperties.Contains("FK");
        }

        public ColumnField Create(DataColumn column)
        {
            return new FKField(column.ColumnName, column.ColumnName, (FK)column.ExtendedProperties["FK"]);
        }

    }


    [DataContract]
    class M2NMappingField : FieldBase
    {
        [IgnoreDataMember]
        public SortedDictionary<int, string> options = null;
        [IgnoreDataMember]
        private List<int> value = null;
        public override object Data
        {
            get
            {
                return value;
            }
            set
            {
                if (value == null || value == DBNull.Value)
                {
                    this.value = new List<int>();
                    return;
                }
                if (!(value is List<int>))
                    throw new ArgumentException("Value of a mapping field must be List<int> or null");
                else if (value != null) this.value = (List<int>)value;
                else this.value = new List<int>();
            }
        }

        public SortedDictionary<int, string> FKOptions { get; set; }


        [DataMember]
        private M2NMapping mapping;
        private M2NMappingControl myControl;
        public M2NMapping Mapping
        {
            get { return mapping; }
            set { mapping = value; }
        }


        public M2NMappingField(M2NMapping mapping, string caption)
            : base(caption)
        {
            this.Mapping = mapping;
        }

        public override UControl MyControl
        {
            get
            {

                if (myControl == null)
                {
                    myControl = new M2NMappingControl();
                    myControl.ID = "Field" + FieldId;
                    myControl.SetOptions(FKOptions);
                }
                return myControl;
            }
        }


        public override void RetrieveData()
        {
            M2NMappingControl c = (M2NMappingControl)myControl;
            value = c.RetrieveData();
        }

        public override void FillData()
        {
            myControl.SetIncludedOptions(value);
        }

        public override void InventData()
        {
            FKOptions = new SortedDictionary<int, string>();
            Random rnd = new Random();
            int a = rnd.Next() % 5 + 5;
            string[] res = new string[a];
            for (int i = 0; i < a; i++)
                FKOptions.Add(i, NLipsum.Core.LipsumGenerator.Generate(1, NLipsum.Core.Features.Words, "{0}", NLipsum.Core.Lipsums.Faust));
        }

        public override void Validate()
        {
            
        }
        public override List<WC.BaseValidator> GetValidators()
        {
            return new List<WC.BaseValidator>();
        }
    }

    [DataContract]
    public class IntegerField : ColumnField {
        private int? value;
        private WC.TextBox myControl;

        public IntegerField(string columnName, string caption)
            : base(columnName, caption)
        { }

        public override UControl MyControl
        {
            get { 
                if(myControl == null){
                    myControl = new WC.TextBox();
                    myControl.ID = "Field" + FieldId;
                    myControl.ClientIDMode = System.Web.UI.ClientIDMode.Static;
                }
                return myControl;
            }
        }

        public override void Validate()
        {
            if(Required && String.IsNullOrEmpty(myControl.Text))
                ErrorMessage = "The field " + Caption + " is required";
            else if(!String.IsNullOrEmpty(myControl.Text) && (value == null))
                ErrorMessage = "The value of " + Caption + " must be an integer";
        }

        public override void RetrieveData()
        {
            int parsed;
            if (Int32.TryParse(myControl.Text, out parsed))
                value = parsed;
        }

        public override void FillData()
        {
            myControl.Text = value == null ? "" : value.ToString();
        }

        public override void InventData()
        {
            value = (int)(DateTime.Now.Ticks % 100000);
        }

        public override object Data
        {
            get
            {
                return value;
            }
            set
            {
                if (value == null || value == DBNull.Value) {
                    this.value = null;
                    return;
                }
                this.value = Int32.Parse(value.ToString());
            }
        }

        public override List<WC.BaseValidator> GetValidators()
        {
            List<WC.BaseValidator> res = base.GetValidators();
            WC.RegularExpressionValidator numVal = new WC.RegularExpressionValidator();
            numVal.ErrorMessage = "The field " + Caption + " must be an integer";
            numVal.ControlToValidate = myControl.ID;
            numVal.ValidationExpression = "[0-9]+";
            numVal.Display = WC.ValidatorDisplay.None;
            res.Add(numVal);
            return res;
        }
    }


    public class IntegerFieldfactory : IColumnFieldFactory
    {
        public string UIName
        {
            get
            {
                return "Integer";
            }
        }
        public FieldSpecificityLevel Specificity
        {
            get
            {
                return FieldSpecificityLevel.Low;
            }
        }
        public Type ProductionType
        {
            get
            {
                return typeof(IntegerField);
            }
        }
        public bool CanHandle(DataColumn column)
        {
            return column.DataType == typeof(long) || column.DataType == typeof(int) || column.DataType == typeof(short)
                || column.DataType == typeof(ulong) || column.DataType == typeof(uint) || column.DataType == typeof(ushort);
        }

        public ColumnField Create(DataColumn column)
        {
            return new IntegerField(column.ColumnName, column.ColumnName);
        }

    }


    [DataContract]
    public class DecimalField : ColumnField
    {
        [IgnoreDataMember]
        private double? value;
        [IgnoreDataMember]
        private WC.TextBox myControl;

        public DecimalField(string columnName, string caption)
            :base(columnName, caption)
        { }

        public override UControl MyControl
        {
            get
            {
                if (myControl == null)
                {
                    myControl = new WC.TextBox();
                    myControl.ID = "Field" + FieldId;
                    myControl.ClientIDMode = System.Web.UI.ClientIDMode.Static;
                }
                return myControl;
            }
        }

        public override void Validate()
        {
            if (Required && String.IsNullOrEmpty(myControl.Text))
                ErrorMessage = "The field " + Caption + " is required";
            else if (!String.IsNullOrEmpty(myControl.Text) && (value == null))
                ErrorMessage = "The value of " + Caption + " must be numeric";
        }

        public override void RetrieveData()
        {
            double parsed;
            if (Double.TryParse(myControl.Text, out parsed))
                value = parsed;
        }

        public override void FillData()
        {
            myControl.Text = value == null ? "" : value.ToString();
        }

        public override void InventData()
        {
            value = (DateTime.Now.Ticks % 10000000 / 100.0);
        }

        public override object Data
        {
            get
            {
                return value;
            }
            set
            {
                if (value == null || value == DBNull.Value) {
                    this.value = null;
                    return;
                }
                this.value = Double.Parse(value.ToString());
            }
        }

        public override List<WC.BaseValidator> GetValidators()
        {
            List<WC.BaseValidator> res = base.GetValidators();
            WC.RegularExpressionValidator numVal = new WC.RegularExpressionValidator();
            numVal.ErrorMessage = "The field " + Caption + " must be an integer";
            numVal.ControlToValidate = myControl.ID;
            numVal.ValidationExpression = "[0-9]+([.,][0-9]+)?";
            numVal.Display = WC.ValidatorDisplay.None;
            res.Add(numVal);
            return res;
        }
    }


    public class DecimalFieldfactory : IColumnFieldFactory
    {
        public string UIName
        {
            get
            {
                return "Decimal";
            }
        }
        public FieldSpecificityLevel Specificity
        {
            get
            {
                return FieldSpecificityLevel.Low;
            }
        }
        public Type ProductionType
        {
            get
            {
                return typeof(DecimalField);
            }
        }
        public bool CanHandle(DataColumn column)
        {
            return column.DataType == typeof(float) || column.DataType == typeof(double) || column.DataType == typeof(decimal);
        }

        public ColumnField Create(DataColumn column)
        {
            return new DecimalField(column.ColumnName, column.ColumnName);
        }

    }



    [DataContract]
    public class EnumField : ColumnField {
        [IgnoreDataMember]
        private WC.DropDownList myControl;
        [IgnoreDataMember]
        private string value;
        [DataMember]
        private List<string> options;

        public EnumField(string columnName, string caption, List<string> options) 
            :base(columnName, caption)
        {
            this.options = options;
        }

        public override UControl MyControl
        {
            get {
                if (myControl == null)
                {
                    if (!Required) options.Add("");
                    options.Sort();
                    myControl = new WC.DropDownList();
                    myControl.DataSource = options;
                    myControl.DataBind();
                }
                return myControl;
            }
        }

        public override void Validate()
        {
            
        }

        public override void RetrieveData()
        {
            value = myControl.SelectedValue;
        }

        public override void FillData()
        {
            if (!String.IsNullOrEmpty(value))
                myControl.SelectedIndex = myControl.Items.IndexOf(myControl.Items.FindByText(value));
            else
                myControl.SelectedIndex = -1;
        }

        public override void InventData()
        {
            options = new List<string>();
            for (int i = 0; i < 5; i++)
                options.Add(Common.Functions.LWord());
            value = options[0];
        }

        public override object Data
        {
            get
            {
                return value;
            }
            set
            {
                if (String.IsNullOrEmpty(value as string)) {
                    this.value = null;
                    return;
                }
                if (!options.Contains(value))
                    throw new ArgumentException("This option in not a part of the enumeration");
                this.value = value as string;
            }
        }

        public override List<WC.BaseValidator> GetValidators()
        {
            return new List<WC.BaseValidator>();
        }
    }

    public class EnumFieldFactory : IColumnFieldFactory {

        public bool CanHandle(DataColumn column)
        {
            return column.ExtendedProperties.ContainsKey(CC.ENUM_COLUMN_VALUES);
        }

        public ColumnField Create(DataColumn column)
        {
            return new EnumField(column.ColumnName, column.ColumnName, (List<string>)column.ExtendedProperties[CC.ENUM_COLUMN_VALUES]);
        }

        public Type ProductionType
        {
            get
            {
                return typeof(EnumField);
            }
        }

        public string UIName
        {
            get {
                return "Enum";
            }
        }

        public FieldSpecificityLevel Specificity
        {
            get {
                return FieldSpecificityLevel.High;
            }
        }
    }

    // no factory for M2N












    /*


        /// <summary>
        /// creates WebControl for hte field, may give it basic styling, doesn`t bind any data
        /// </summary>
        /// <param name="extenders">AjaxControlToolkit extenders, for the needs of a html text field</param>
        /// <param name="handler">General event handler for events on text fields, that there for any fieldtype so far</param>
        /// <returns></returns>
        public virtual UControl ToUControl(EventHandler handler = null)
        {
            System.Web.UI.Control res;
            switch (type)
            {

                case FieldTypes.Date:
                    WC.TextBox resCal = new WC.TextBox();
                    resCal.ID = "Field" + fieldId.ToString();
                    resCal.CssClass = "includeDatePicker";
                    //AjaxControlToolkit.CalendarExtender calendarExtender = new AjaxControlToolkit.CalendarExtender();
                    //calendarExtender.TargetControlID = resCal.ID;
                    //extenders.Add(calendarExtender);

                    res = resCal;
                    break;
                case FieldTypes.DateTime:
                    throw new NotImplementedException(); // composite control
                case FieldTypes.Time:
                    throw new NotImplementedException();
                case FieldTypes.Holder:
                    WC.Panel resHol = new WC.Panel();
                    //return resHol;
                    res = resHol;
                    break;
                case FieldTypes.ShortText:
                    WC.TextBox resTxt = new WC.TextBox();
                    //resTxt.EnableViewState = false;
                    //if(value is string)
                    //    resTxt.Text = (string)value;
                    //resTxt.ID = "Field" + fieldId.ToString();
                    resTxt.Width = 300;
                    res = resTxt;
                    break;
                case FieldTypes.Text:
                    //AjaxControlToolkit.HtmlEditorExtender editor = new AjaxControlToolkit.HtmlEditorExtender();
                    WC.TextBox tb = new WC.TextBox();
                    //if(value is string)
                    //    tb.Text = (string)value;
                    tb.ID = "Field" + fieldId.ToString();
                    tb.Width = 500;
                    tb.Height = 250;
                    tb.CssClass = "includeEditor";
                    //editor.Enabled = true;
                    //editor.TargetControlID = tb.ID;
                    //extenders.Add(editor);
                    res = tb;
                    break;
                case FieldTypes.Bool:
                    WC.CheckBox cb = new WC.CheckBox();
                    if (value is bool)
                        cb.Checked = (bool)value;
                    res = cb;
                    break;
                case FieldTypes.Enum:
                    throw new NotFiniteNumberException();
                default:
                    throw new NotImplementedException();
            }
            res.ID = "Field" + fieldId;
            this.myControl = res;
            return res;
        }

        // retrieves data seved in vewstate on Page_Load, saves it to inner value variable
        public virtual void RetrieveData()
        {
            switch (type)
            {
                case FieldTypes.Date:
                    WC.TextBox resCal = (WC.TextBox)myControl;
                    DateTime date = DateTime.MinValue;
                    DateTime.TryParse(resCal.Text, out date);
                    if (date != DateTime.MinValue)
                        value = date;
                    break;
                case FieldTypes.DateTime:
                    throw new NotImplementedException(); // composite control
                case FieldTypes.Time:
                    throw new NotImplementedException();
                case FieldTypes.Holder:
                    break;
                case FieldTypes.Text:
                case FieldTypes.ShortText:
                    WC.TextBox resTxt = (WC.TextBox)myControl;
                    value = resTxt.Text;
                    break;
                case FieldTypes.Bool:
                    WC.CheckBox cb = (WC.CheckBox)myControl;
                    value = cb.Checked;
                    break;
                case FieldTypes.Enum:
                    throw new NotFiniteNumberException();
                default:
                    throw new NotImplementedException();
            }
        }

        // sets the WebControl`s value based on the inner value variable
        public virtual void SetControlData()
        {
            if (value == null || value == DBNull.Value) return;
            switch (type)
            {
                case FieldTypes.Date:
                    WC.TextBox resCal = (WC.TextBox)myControl;
                    if (value is DateTime)
                        resCal.Text = ((DateTime)value).ToShortDateString();
                    break;
                case FieldTypes.DateTime:
                    throw new NotImplementedException(); // composite control
                case FieldTypes.Time:
                    throw new NotImplementedException();
                case FieldTypes.Holder:
                    break;
                case FieldTypes.Text:
                case FieldTypes.ShortText:
                    WC.TextBox resTxt = (WC.TextBox)myControl;
                    resTxt.Text = value.ToString();
                    break;
                case FieldTypes.Bool:
                    WC.CheckBox cb = (WC.CheckBox)myControl;
                    if(value is sbyte){
                        if ((sbyte)value == 0) value = false;
                        else if ((sbyte)value == 1) value = true;
                    }
                    cb.Checked = (bool)value;
                    break;
                case FieldTypes.Enum:
                    throw new NotFiniteNumberException();
                default:
                    throw new NotImplementedException();
            }
        }

        // creates a validator for every ValidatorRule in rules collection
        public virtual List<WC.BaseValidator> GetValidator()
        {
            List<WC.BaseValidator> res = new List<WC.BaseValidator>();
            UControl fieldControl = myControl;

            foreach (ValidationRules rule in validationRules)
            {
                switch (rule)
                {
                    case ValidationRules.Required:
                        WC.RequiredFieldValidator reqVal;
                        reqVal = new WC.RequiredFieldValidator();
                        reqVal.ControlToValidate = fieldControl.ID; // if not set, set in ToUControl using panel and field id
                        reqVal.ErrorMessage = this.caption + " is required";
                        reqVal.Display = WC.ValidatorDisplay.None;
                        res.Add(reqVal);
                        break;
                    case ValidationRules.Ordinal:
                        WC.RegularExpressionValidator regexVal = new WC.RegularExpressionValidator();
                        regexVal.ValidationExpression = "[0-9]+";
                        regexVal.ControlToValidate = fieldControl.ID;
                        regexVal.ErrorMessage = this.caption + " must be an integer";
                        regexVal.Display = WC.ValidatorDisplay.None;
                        res.Add(regexVal);
                        break;
                    case ValidationRules.Decimal:
                        WC.RegularExpressionValidator regexVal2 = new WC.RegularExpressionValidator();
                        regexVal2.ValidationExpression = "[0-9]+([.,][0-9]+)?";
                        regexVal2.ControlToValidate = fieldControl.ID;
                        regexVal2.ErrorMessage = this.caption + " must be a decimal number";
                        regexVal2.Display = WC.ValidatorDisplay.None;
                        res.Add(regexVal2);
                        break;
                    case ValidationRules.Unique:
                        // will be ensured during Webdriver insert / update
                        break;
                    default:
                        break;
                }
            }
            return res;
        }

        // does server-side validation (which is neccessary with mono)
        public virtual List<string> Validate()
        {
            List<string> res = new List<string>();
            UControl fieldControl = myControl;

            foreach (ValidationRules rule in validationRules)
            {
                switch (rule)
                {
                    case ValidationRules.Required:
                        if(value == null || String.IsNullOrEmpty(value.ToString()))
                            res.Add(this.caption + " is required");
                        break;
                    case ValidationRules.Ordinal:
                        if (!(value == null || String.IsNullOrEmpty(value.ToString())) && !Regex.IsMatch(value as string, "[0-9]+"))
                            res.Add(this.caption + " must be an integer");
                        break;
                    case ValidationRules.Decimal:
                        if (!(value == null || String.IsNullOrEmpty(value.ToString())) && !Regex.IsMatch(value as string, "[0-9]+([.,][0-9]+)?"))
                            res.Add(this.caption + " must be a decimal number");
                        break;
                    case ValidationRules.Unique:
                        // will be ensured during Webdriver insert / update
                        break;
                    default:
                        break;
                }
            }
            return res;
        }
    }


    [DataContract]
    class FKField : Field
    {
        [IgnoreDataMember]
        private int? _value;
        [IgnoreDataMember]
        public SortedDictionary<int, string> options = null;
        public override object value
        {

            get
            {
                return _value;
            }
            set
            {
                if (value == null || value == DBNull.Value)
                    this._value = null;
                else
                    if (!(value is int))
                        throw new ArgumentException("Value of a FK field must be int or null");
                    else
                        this._value = (int)value;
            }
        }
        [DataMember]
        private FK fk;

        public FK FK
        {
            get
            {
                return fk;
            }

            private set
            {
                fk = value;
            }

        }

        public FKField(int fieldId, string column, int panelId, FK fk, string caption = null)
            : base(fieldId, column, FieldTypes.FK, panelId, caption)
        {
            this.fk = fk;
        }


        public void SetOptions(SortedDictionary<int, string> options)
        {     // set inner options collection, dont bind them yet
            if (this.options == null) this.options = options;
            else throw new Exception("FK Options already set");
        }

        public override UControl ToUControl(EventHandler handler = null)
        {
            WC.DropDownList res = new WC.DropDownList();
            res.ID = "Field" + fieldId;
            this.myControl = res;
            return res;
        }

        public override void RetrieveData()
        {
            WC.DropDownList ddl = (WC.DropDownList)myControl;
            int v = Int32.Parse(ddl.SelectedValue);
            if (v == int.MinValue) value = null;
            else value = v;
        }

        public override void SetControlData()
        {
            WC.DropDownList ddl = (WC.DropDownList)myControl;
            ddl.DataTextField = "Value";
            ddl.DataValueField = "Key";
            if (!validationRules.Contains(ValidationRules.Required)) options[int.MinValue] = "";
            ddl.DataSource = options;
            ddl.DataBind();
            if (value == null) return;
            ddl.SelectedIndex = ddl.Items.IndexOf(ddl.Items.FindByValue(value.ToString()));
        }

    }















    */














    /*


    [DataContract]
    class EnumField : Field
    {
        [IgnoreDataMember]
        private int? _value;
        [DataMember]
        private List<string> options = null;
        public override object value
        {

            get
            {
                return _value;
            }
            set
            {
                if (value == null || value == DBNull.Value || value.ToString() == "")
                    this._value = null;
                else
                    if (value is string)
                    {
                        if (options.Contains(value as string))
                            _value = options.IndexOf(value as string) + 1;
                        else throw new ArgumentException("Value not forund within enum options");
                    }
                    else if (value is int)
                    {
                        _value = (int)value;
                    }
                    else throw new ArgumentException("Value not found winthin enum options");
            }
        }

        public EnumField(int fieldId, string column, int panelId, List<string> options, string caption = null)
            : base(fieldId, column, FieldTypes.Enum, panelId, caption)
        {
            this.options = options;
        }


        public override UControl ToUControl(EventHandler handler = null)
        {
            WC.DropDownList res = new WC.DropDownList();
            res.ID = "Field" + fieldId;
            this.myControl = res;
            return res;
        }

        public override void RetrieveData()
        {
            WC.DropDownList ddl = (WC.DropDownList)myControl;
            if(ddl.SelectedIndex == -1) return;
            int v = Int32.Parse(ddl.SelectedValue);
            value = v;
        }

        public override void SetControlData()
        {
            SortedDictionary<int, string> optDict = new SortedDictionary<int, string>();
            for (int i = 0; i < options.Count; i++)
            {
                optDict[i + 1] = options[i];
            }

            WC.DropDownList ddl = (WC.DropDownList)myControl;
            ddl.DataTextField = "Value";
            ddl.DataValueField = "Key";
            if (!validationRules.Contains(ValidationRules.Required)) optDict[int.MinValue] = "";
            ddl.DataSource = optDict;
            ddl.DataBind();
            if (value == null) return;
            ddl.SelectedIndex = ddl.Items.IndexOf(ddl.Items.FindByValue(value.ToString()));
        }

    }
     */
}