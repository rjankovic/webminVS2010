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
        [DataMember]
        public int? fieldId { get; private set; }
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
        [DataMember]
        public virtual object value { get; set; }
        [DataMember]
        public string caption { get; private set; }
        [DataMember]
        public List<ValidationRules> validationRules { get; private set; }
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

        //todo validation
        public virtual bool Validate(object value)
        {
            return true;
        }


        public virtual bool ValidateSelf()
        {
            return true;

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
            switch (type)
            {
                case FieldTypes.Date:
                    WC.Calendar resCal = new WC.Calendar();
                    resCal.SelectionMode = WC.CalendarSelectionMode.Day;
                    return resCal;
                case FieldTypes.DateTime:
                    throw new NotImplementedException(); // composite control
                case FieldTypes.Time:
                    throw new NotImplementedException();
                case FieldTypes.Holder:
                    WC.PlaceHolder resHol = new WC.PlaceHolder();
                    return resHol;
                case FieldTypes.Decimal:
                case FieldTypes.Ordinal:
                case FieldTypes.Varchar:
                    WC.TextBox resTxt = new WC.TextBox();
                    resTxt.Width = 200;
                    return resTxt;
                case FieldTypes.Text:
                    AjaxControlToolkit.HtmlEditorExtender editor = new AjaxControlToolkit.HtmlEditorExtender();
                    WC.TextBox tb = new WC.TextBox();
                    tb.ID = "Field" + fieldId.ToString();
                    tb.Width = 500;
                    tb.Height = 250;
                    editor.Enabled = true;
                    editor.TargetControlID = tb.ID;
                    extenders.Add(editor);
                    return tb;
                case FieldTypes.Bool:
                    WC.CheckBox cb = new WC.CheckBox();
                    return cb;
                case FieldTypes.Enum:
                    throw new NotFiniteNumberException();
                default:
                    throw new NotImplementedException();
            }
        }

        public virtual List<WC.BaseValidator> GetValidator(UControl fieldControl)
        {
            List<WC.BaseValidator> res = new List<WC.BaseValidator>();

            foreach (ValidationRules rule in validationRules)
            {
                switch (rule)
                {
                    case ValidationRules.Required:
                        WC.RequiredFieldValidator reqVal = new WC.RequiredFieldValidator();
                        reqVal.ControlToValidate = fieldControl.ID; // if not set, set in ToUControl using panel and field id
                        reqVal.ErrorMessage = this.caption + " is required";
                        res.Add(reqVal);
                        break;
                    case ValidationRules.Ordinal:
                        WC.RegularExpressionValidator regexVal = new WC.RegularExpressionValidator();
                        regexVal.ValidationExpression = "[0-9]+";
                        regexVal.ErrorMessage = this.caption + " must be an integer";
                        res.Add(regexVal);
                        break;
                    case ValidationRules.Decimal:
                        WC.RegularExpressionValidator regexVal2 = new WC.RegularExpressionValidator();
                        regexVal2.ValidationExpression = "[0-9]+([.,][0-9]+)?";
                        regexVal2.ErrorMessage = this.caption + " must be a decimal number";
                        res.Add(regexVal2);
                        break;
                    case ValidationRules.ZIP:
                        WC.RegularExpressionValidator regexValZIP = new WC.RegularExpressionValidator();
                        regexValZIP.ValidationExpression = "[0-9]{5}";
                        regexValZIP.ErrorMessage = this.caption + " must be a valid ZIP code";
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

        private object _value;
        public override object value
        {
            get
            {
                return _value;
            }
            set
            {
                if (!(value is string) && !(value == null))
                    throw new ArgumentException("Value of a mapping field must be string or null");
                else
                    this._value = value;
            }
        }
        [DataMember]
        public FK fk { get; private set; }

        public FKField(int fieldId, string column, int panelId, FK fk, string caption = null)
            : base(fieldId, column, FieldTypes.FK, panelId, caption)
        {
            this.fk = fk;
        }

        public override bool ValidateSelf()
        {
            return fk.validateInput((string)value);
        }

        public override UControl ToUControl(List<AjaxControlToolkit.ExtenderControlBase> extenders, EventHandler handler = null)
        {
            WC.DropDownList res = new WC.DropDownList();
            res.DataSource = fk.options;
            res.DataBind();
            return res;
        }

    }

    [DataContract]
    class M2NMappingField : Field
    {
        private object _value;
        public override object value
        {
            get
            {
                return _value;
            }
            set
            {
                if (!(value is List<string>) && !(value == null))
                    throw new ArgumentException("Value of a mapping field must be List<string> or null");
                else
                    this._value = value;
            }
        }

        [DataMember]
        public M2NMapping mapping { get; private set; }

        public M2NMappingField(int fieldId, string column, int panelId,
            M2NMapping mapping, string caption = null)
            : base(fieldId, column, FieldTypes.M2NMapping, panelId)
        {
            this.mapping = mapping;
        }

        public override bool ValidateSelf()
        {
            return mapping.validateWholeInput((List<string>)value);

        }

        public UControl ToUControl(EventHandler handler, List<AjaxControlToolkit.ExtenderControlBase> extenders)
        {
            _min.Controls.M2NMappingControl res = new M2NMappingControl();
            res.SetOptions(this.mapping.options);
            return res;
        }
    }

}