using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Data;
using System.Web.UI;
using NLipsum.Core;
using System.Drawing.Imaging;

namespace _min.Common
{
    public static class Functions
    {
        /// <summary>
        /// Tries to convert string to given type if TryParse method exists in the type
        /// DEPRECEATED
        /// </summary>
        /// <param name="str"></param>
        /// <param name="t"></param>
        /// <param name="res"></param>
        /// <returns>TryParse was present && string was successfully parsed</returns>
        public static bool TryTryParse(string str, Type t, out object res) 
        {
            var parseMethod = t.GetMethod("TryParse", new Type[] { typeof(string), t.MakeByRefType() });
            if (parseMethod != null)
            {
                object objectArgument = null;
                object[] tryParseParams = new object[] { str, objectArgument };
                if ((bool)parseMethod.Invoke(t, tryParseParams))
                {
                    res = Convert.ChangeType(tryParseParams[1], t);
                    return true;
                }

            }
            res = null;
            return false;
        }

        public static string LWord()
        {
            return NLipsum.Core.LipsumGenerator.Generate(1, NLipsum.Core.Features.Words, "{0}", NLipsum.Core.Lipsums.TheRaven);
        }

        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static string StreamToString(MemoryStream s) {
            s.Position = 0;
            StreamReader reader = new StreamReader(s);
            return reader.ReadToEnd();
        }

        public static double UnixTimestamp()
        {
            return (DateTime.Now - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
        }

    }

    public class ColumnDisplayComparer : IComparer<DataColumn>
    {

        public int Compare(DataColumn x, DataColumn y)
        {
            if (x.Unique && !y.Unique) return -1;
            if (x == y || x.DataType == y.DataType) return 0;
            if (x == null) return 1;
            if (y == null) return -1;
            if (x.DataType == typeof(string) && y.DataType == typeof(string))
                return y.MaxLength - x.MaxLength;
            if (x.DataType == typeof(string)) return -1;
            if (y.DataType == typeof(string)) return 1;
            if (x.DataType == typeof(DateTime)) return -1;
            if (x.DataType == typeof(DateTime)) return 1;
            if (x.DataType == typeof(int)) return -1;
            if (y.DataType == typeof(int)) return 1;
            return 0;
        }
    }

    public class ValidationError : IValidator
    {
        private ValidationError(string message)
        {
            ErrorMessage = message;
            IsValid = false;
        }

        public string ErrorMessage { get; set; }

        public bool IsValid { get; set; }

        public void Validate()
        {
            // no action required
        }

        public static void Display(string message, Page currentPage)
        {
            currentPage.Validators.Add(new ValidationError(message));
        }
    }



}
