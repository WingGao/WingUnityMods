using System;
using HarmonyLib;

namespace WingUtil.Harmony
{
    public static class FileLogF
    {
        public static void Log(string format, params object[] args)
        {
            var fmt = String.Format(format, args);
            var full = String.Format("[Wing][{0}] {1}", DateTime.Now.ToLongTimeString(), fmt);
            FileLog.Log(full);
        }
    }
}