
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using _min.Common;

namespace _min.Common
{
    interface IDataProperty {
        /*
        object Value { get; }
        string DbName { get;}
        PropertyConcerns Concerns { get;}
         */
    }

    public class DataProperty<T> : IDataProperty
    {
        public virtual T Value { get; set; }
        protected T defaultValue;
        public string DbName { get; private set; }
        public PropertyConcerns Concerns { get; private set; }

        public DataProperty(string dbName, PropertyConcerns concerns, T defaultValue = default(T))
        {
            DbName = dbName;
            Concerns = concerns;
            Value = defaultValue;
            this.defaultValue = defaultValue;
        }
    }

    public class ParsedDataProperty<T> :  DataProperty<T> where T : struct
    {
        
        public ParsedDataProperty(string dbName, PropertyConcerns concerns, T defaultValue = default(T))
            :base(dbName, concerns, defaultValue)
        { }

        public virtual bool SetValueFromString(string str){
            try
            {
                Value = (T)Convert.ChangeType(str, typeof(T));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class RequiredDataProperty<T> : DataProperty<T>
    {
        protected bool isSet = false;
        
        public override T Value { 
            get 
            {
                if (!isSet) throw new MissingFieldException("Required property " + this.DbName + " not set.");
                return Value;
            } 
            set 
            {
                this.Value = value;
                isSet = true;
            } 
        }

        public RequiredDataProperty(string dbName, PropertyConcerns concerns, T defaultValue = default(T))
            : base(dbName, concerns, defaultValue)
        { }
    }

    public class RequiredParsedDataProperty<T> : RequiredDataProperty<T> where T : struct { 
     
        public RequiredParsedDataProperty(string dbName, PropertyConcerns concerns, T defaultValue = default(T))
            : base(dbName, concerns, defaultValue)
        { }

        public virtual bool SetValueFromString(string str)
        {
            try
            {
                Value = (T)Convert.ChangeType(str, typeof(T));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    //TODO enum properties
}