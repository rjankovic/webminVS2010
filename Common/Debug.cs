using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using _min.Interfaces;
using _min.Models;
using _min.Common;

namespace _min.Common
{
    /*
    public static class Debug
    {
        private static string ValueComparison(object o1, object o2) {
            return null;
            if (o1.GetType().ToString() == o2.GetType().ToString())
            {
                if (o1 != o2) return o1.ToString() + " != " + o2.ToString();
                else return null;
            }
            object parsed;
            if (o1 is string && Functions.TryTryParse(o1 as string, o2.GetType(), out parsed))
            {
                return " > parseable, values " + ((parsed != o2) ? "do not " : "") + "match.";
            }
            if (o2 is string && Functions.TryTryParse(o2 as string, o1.GetType(), out parsed))
            {
                return " < parseable, values " + ((parsed != o1) ? "do not " : "") + "match.";
            }
            return "cannot parse between " + o1.GetType().ToString() + " and " + o2.GetType().ToString();
        }

        private static void PropertyCollectionComparison(PropertyCollection p1, PropertyCollection p2, 
            string caption, List<string> res) {
            res.Add("---COMPARING " + caption);
            string comparison;
            foreach (string property in p1.Keys)
            {
                if (p2.ContainsKey(property))
                {
                    comparison = ValueComparison(p1[property], p2[property]);
                    if (comparison != null) res.Add("attr " + property + " : " + comparison);
                }
            }
            foreach (string property in p1.Keys)
            {
                if (p2.ContainsKey(property))
                {
                    comparison = ValueComparison(p1[property], p2[property]);
                    if (comparison != null) res.Add("attr " + property + " : " + comparison);
                }
                else
                    res.Add("attr " + property + " only in the first");
            }
            foreach (string property in p2.Keys) { 
                if(!p1.ContainsKey(property))
                    res.Add("attr " + property + " only in the second");
            }
        }
        
        public static List<string> ComparePanels(Panel p1, Panel p2, List<string> res = null) {
            if (res == null) res = new List<string>();
            res.Add("------Comparing panels " + p1.panelId + " and " + p2.panelId);
            PropertyCollectionComparison(p1.viewAttr, p2.viewAttr, "panel viewAttr", res);
            PropertyCollectionComparison(p1.controlAttr, p2.controlAttr, "panel controlAttr", res);
            foreach(Field f1 in p1.fields){
                if (!p2.fields.Any(f2 => f2.fieldId == f1.fieldId))
                    res.Add("Field " + f1.fieldId + " for " + f1.column + " in the first panel only");
                else {
                    res.Add("---Comparing field " + f1.fieldId);
                    Field f2 = p2.fields.Find(x => x.fieldId == f1.fieldId);
                    PropertyCollectionComparison(f1.rules, f2.rules, "field validation", res);
                    PropertyCollectionComparison(f1.attr, f2.attr, "field view attr", res);
                }
            }
            foreach(Field f2 in p2.fields)
                if(!p1.fields.Any(x => x.fieldId == f2.fieldId)) 
                    res.Add("Field " + f2.fieldId + " for " + f2.column + " in the second panel only");
            foreach (Panel ch1 in p1.children) {
                if (!p2.children.Any(x => x.panelId == ch1.panelId))
                    res.Add("child panel " + ch1.panelId + " for " + ch1.tableName + " in the first only");
                else {
                    res.Add("------Comparing child panels " + ch1.panelId);
                    ComparePanels(ch1, p2.children.Find(x => x.panelId == ch1.panelId), res); 
                }
            }
            foreach (Panel ch2 in p2.children) {
                if (!p1.children.Any(x => x.panelId == ch2.panelId))
                    res.Add("child panel " + ch2.panelId + " for " + ch2.tableName + " in the second only");
            }
            return res;
        }
    }
     */
}