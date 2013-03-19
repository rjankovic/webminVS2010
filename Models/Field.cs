using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Runtime.Serialization;
using System.IO;

using _min.Interfaces;
using _min.Common;
using _min.Controls;
using UControl = System.Web.UI.Control;
using WC = System.Web.UI.WebControls;

namespace _min.Models
{
    [DataContract]
    [KnownType(typeof(FK))]
    [KnownType(typeof(M2NMapping))]
    [KnownType(typeof(FKField))]
    [KnownType(typeof(M2NMappingField))]
    public class Field
    {
        [IgnoreDataMember]
        protected System.Web.UI.Control myControl;
        [IgnoreDataMember]
        public int? fieldId { get; set; }       // better setter once
        [DataMember]
        public string column { get; private set; }
        [DataMember]
        public FieldTypes type { get; private set; }
        [DataMember]
        public string errMsg { get; private set; }
        private Panel _panel;
        public Panel panel
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
                    panelId = value.panelId;
                }
                else throw new Exception("Panel already set");
            }
        }
        [DataMember]
        public int? panelId { get; private set; }
        [IgnoreDataMember]
        public virtual object value { get; set; }
        [DataMember]
        public string caption { get; private set; }
        [DataMember]
        public List<ValidationRules> validationRules { get; set; }
        [DataMember]
        public int position { get; private set; }

        public Field(int fieldId, string column, FieldTypes type, int? panelId, string caption = null, int position = 1)
        {
            this.fieldId = fieldId;
            this.column = column;
            this.type = type;
            this.panelId = panelId;
            value = null;
            errMsg = "";
            this.caption = caption;
            if (caption == null) this.caption = column;
            validationRules = new List<ValidationRules>();
            this.position = position;
        }

        public string Serialize()
        {
            MemoryStream ms = new MemoryStream();
            DataContractSerializer ser = new DataContractSerializer(typeof(Field));
            ser.WriteObject(ms, this);

            return Functions.StreamToString(ms);
        }


        public void SetCreationId(int id)
        {
            if (fieldId == 0) fieldId = id;
            else
                throw new Exception("Field id already initialized");
        }

        public void RefreshPanelId()
        {
            this.panelId = panel.panelId;
        }

        public virtual UControl ToUControl(List<AjaxControlToolkit.ExtenderControlBase> extenders, EventHandler handler = null)
        {
            System.Web.UI.Control res;
            switch (type)
            {

                case FieldTypes.Date:
                    WC.TextBox resCal = new WC.TextBox();
                    resCal.ID = "Field" + fieldId.ToString();
                    AjaxControlToolkit.CalendarExtender calendarExtender = new AjaxControlToolkit.CalendarExtender();
                    calendarExtender.TargetControlID = resCal.ID;
                    extenders.Add(calendarExtender);
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
                case FieldTypes.Decimal:
                case FieldTypes.Ordinal:
                    WC.TextBox resNTxt = new WC.TextBox();
                    /*
                    if (value is int || value is long || value is decimal || value is double)
                        resNTxt.Text = value.ToString();*/
                    res = resNTxt;
                    break;
                case FieldTypes.Varchar:
                    WC.TextBox resTxt = new WC.TextBox();
                    //resTxt.EnableViewState = false;
                    //if(value is string)
                    //    resTxt.Text = (string)value;
                    //resTxt.ID = "Field" + fieldId.ToString();
                    resTxt.Width = 300;
                    res = resTxt;
                    break;
                case FieldTypes.Text:
                    AjaxControlToolkit.HtmlEditorExtender editor = new AjaxControlToolkit.HtmlEditorExtender();
                    WC.TextBox tb = new WC.TextBox();
                    //if(value is string)
                    //    tb.Text = (string)value;
                    tb.ID = "Field" + fieldId.ToString();
                    tb.Width = 500;
                    tb.Height = 250;
                    editor.Enabled = true;
                    editor.TargetControlID = tb.ID;
                    extenders.Add(editor);
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
                case FieldTypes.Decimal:
                case FieldTypes.Ordinal:
                case FieldTypes.Text:
                case FieldTypes.Varchar:
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

        public virtual void SetControlData()
        {
            if (value == null || value == DBNull.Value) return;
            switch (type)
            {
                case FieldTypes.Date:
                    WC.TextBox resCal = (WC.TextBox)myControl;
                    resCal.Text = ((DateTime)value).ToShortDateString();
                    break;
                case FieldTypes.DateTime:
                    throw new NotImplementedException(); // composite control
                case FieldTypes.Time:
                    throw new NotImplementedException();
                case FieldTypes.Holder:
                    break;
                case FieldTypes.Decimal:
                case FieldTypes.Ordinal:
                case FieldTypes.Text:
                case FieldTypes.Varchar:
                    WC.TextBox resTxt = (WC.TextBox)myControl;
                    resTxt.Text = value.ToString();
                    break;
                case FieldTypes.Bool:
                    WC.CheckBox cb = (WC.CheckBox)myControl;
                    cb.Checked = (bool)value;
                    break;
                case FieldTypes.Enum:
                    throw new NotFiniteNumberException();
                default:
                    throw new NotImplementedException();
            }
        }

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
                    case ValidationRules.ZIP:
                        WC.RegularExpressionValidator regexValZIP = new WC.RegularExpressionValidator();
                        regexValZIP.ControlToValidate = fieldControl.ID;
                        regexValZIP.ValidationExpression = "[0-9]{5}";
                        regexValZIP.ErrorMessage = this.caption + " must be a valid ZIP code";
                        regexValZIP.Display = WC.ValidatorDisplay.None;
                        res.Add(regexValZIP);
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
        public SortedDictionary<string, int> options = null;
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


        public void SetOptions(SortedDictionary<string, int> options) {
            if (this.options == null) this.options = options;
            else throw new Exception("FK Options already set");
        } 
        
        public override UControl ToUControl(List<AjaxControlToolkit.ExtenderControlBase> extenders, EventHandler handler = null)
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
            ddl.DataTextField = "Key";
            ddl.DataValueField = "Value";
            if (!validationRules.Contains(ValidationRules.Required)) options[""] = int.MinValue;
            ddl.DataSource = options;
            ddl.DataBind();
            if (value == null) return;
            ddl.SelectedIndex = ddl.Items.IndexOf(ddl.Items.FindByValue(value.ToString()));
        }

    }

    [DataContract]
    class M2NMappingField : Field
    {
        [IgnoreDataMember]
        public SortedDictionary<string, int> options = null;
        [IgnoreDataMember]
        private List<int> _value = null;
        public override object value
        {
            get
            {
                return _value;
            }
            set
            {
                if (!(value is List<int>) && !(value == null))
                    throw new ArgumentException("Value of a mapping field must be List<int> or null");
                else if(value != null) _value = (List<int>)value;
                else _value = new List<int>();
            }
        }

        public List<int> ValueList {
            get {
                return _value;
            }
        }

        [DataMember]
        private M2NMapping mapping;
        public M2NMapping Mapping { 
            get { return mapping; }
            set { mapping = value; }
        }


        public M2NMappingField(int fieldId, string column, int panelId,
            M2NMapping mapping, string caption = null)
            : base(fieldId, column, FieldTypes.M2NMapping, panelId, caption)
        {
            this.Mapping = mapping;
            //if (mapping.options == null) mapping.options = new Dictionary<string, int>();
            //if(mapping == null) this.mapping = new M2NMapping(;
        }

        public void SetOptions(SortedDictionary<string, int> options)
        {
            if (this.options == null) this.options = options;
            else throw new Exception("FK Options already set");
        } 


        public override UControl ToUControl(List<AjaxControlToolkit.ExtenderControlBase> extenders, EventHandler handler = null)
        {
            _min.Controls.M2NMappingControl res = new M2NMappingControl();
            res.ID = "Field" + fieldId;
            myControl = res;
            //res.SetOptions(this.Mapping.options);
            return res;
        }

        public override void RetrieveData()
        {
            M2NMappingControl c = (M2NMappingControl)myControl;
            value = (from WC.ListItem item in c.IncludedItems select Int32.Parse(item.Value)).ToList<int>();
        }

        public override void SetControlData()
        {
            M2NMappingControl c = (M2NMappingControl)myControl;
            c.SetOptions(options);
            if (value == null) return;
            c.SetIncludedOptions((List<int>)value);
        }
    }

}