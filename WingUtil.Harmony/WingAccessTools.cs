using System;
using HarmonyLib;

namespace WingUtil.Harmony
{
    public static class WingAccessTools
    {
        public static T GetFieldValue<T>(object obj, string fieldName) where T : class
        {
            return AccessTools.Field(obj.GetType(), fieldName).GetValue(obj) as T;
        }

        public static void SetFieldValue(object obj, string fieldName, object val)
        {
            AccessTools.Field(obj.GetType(), fieldName).SetValue(obj, val);
        }
        public static void SetPropertyValue(object obj, string fieldName, object val)
        {
            AccessTools.Property(obj.GetType(), fieldName).SetValue(obj, val);
        }
    }
}