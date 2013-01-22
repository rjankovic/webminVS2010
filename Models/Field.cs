using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using _min.Interfaces;
using _min.Common;
using System.Data;
using System.Runtime.Serialization;
using System.IO;

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
        public string column {get; private set;}
        [DataMember]
        public FieldTypes type { get; private set; }
        [DataMember]
        public string errMsg { get; private set; }
        private Panel _panel;
        public Panel panel { 
            get { 
            return _panel; 
            } 
            set {
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
        public int position {get; private set;}
        
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

        public string Serialize() { 
            MemoryStream ms = new MemoryStream();
            DataContractSerializer ser = new DataContractSerializer(typeof(Field));
            ser.WriteObject(ms, this);

            return Functions.StreamToString(ms);
        }

        //todo validation
        public virtual bool Validate(object value) { 
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
    }

    [DataContract]
    class FKField : Field
    {

        private object _value;
        public override object value 
        {
            get {
                return _value;
            }
            set {
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

    }

    [DataContract]
    class M2NMappingField : Field
    {
        private object _value;
        public override object value
        {
            get {
                return _value;
            }
            set {
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
    }
        
}