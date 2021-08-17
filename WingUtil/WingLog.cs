using System;
using System.IO;

namespace WingUtil
{
    public class WingLog
    {
        public static void Log(string format, params object[] args)
        {
            // File.AppendAllText("D:\\wing_mod.log", msg + "\n");
            var t = DateTime.Now.ToLongTimeString();
            File.AppendAllText("D:\\wing_mod.log", String.Format("[" + t + "] " + format + "\n", args));
        }
    }
}