using System.IO;
using UnityEngine;

namespace NEON.UI
{
    public class WingLog
    {
        public static void Log(string msg)
        {
            File.AppendAllText("D:\\wing_mod.log", msg + "\n");
        }
    }
}