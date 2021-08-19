using System;
using System.IO;

namespace WingUtil
{
    public class WingLog
    {
        private static string filePath = "D:\\wing_mod.log";

        public static void Reset()
        {
            File.WriteAllText(filePath, "");
        }

        public static void Log(string format, params object[] args)
        {
            lock (filePath)
            {
                // File.AppendAllText("D:\\wing_mod.log", msg + "\n");
                var t = DateTime.Now.ToLongTimeString();
                var full = String.Format("[" + t + "] " + format + "\n", args);
                File.AppendAllText(filePath, full);
            }
        }
    }
}