using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;

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

        public static String GetStackFrame(int skip)
        {
            StackFrame callStack = new StackFrame(skip, true);
            return FmtStackFrame(callStack);
        }

        public static String FmtStackFrame(StackFrame frame)
        {
            string format = "in {0}:line {1}";
            StringBuilder stringBuilder = new StringBuilder();
            MethodBase method = frame.GetMethod();
            Type declaringType = method.DeclaringType;
            if (declaringType != (Type)null)
            {
                stringBuilder.Append(declaringType.FullName.Replace('+', '.'));
                stringBuilder.Append(".");
            }

            stringBuilder.Append(method.Name);
            if ((object)(method as MethodInfo) != null && method.IsGenericMethod)
            {
                Type[] genericArguments = method.GetGenericArguments();
                stringBuilder.Append("[");
                int index2 = 0;
                bool flag3 = true;
                for (; index2 < genericArguments.Length; ++index2)
                {
                    if (!flag3)
                        stringBuilder.Append(",");
                    else
                        flag3 = false;
                    stringBuilder.Append(genericArguments[index2].Name);
                }

                stringBuilder.Append("]");
            }

            stringBuilder.Append("(");
            ParameterInfo[] parameters = method.GetParameters();
            bool flag4 = true;
            for (int index3 = 0; index3 < parameters.Length; ++index3)
            {
                if (!flag4)
                    stringBuilder.Append(", ");
                else
                    flag4 = false;
                string str2 = "<UnknownType>";
                if (parameters[index3].ParameterType != (Type)null)
                    str2 = parameters[index3].ParameterType.Name;
                stringBuilder.Append(str2 + " " + parameters[index3].Name);
            }

            stringBuilder.Append(")");
            if (frame.GetILOffset() != -1)
            {
                string str3 = (string)null;
                try
                {
                    str3 = frame.GetFileName();
                }
                catch (Exception ex)
                {
                }

                if (str3 != null)
                {
                    stringBuilder.Append(' ');
                    stringBuilder.AppendFormat((IFormatProvider)CultureInfo.InvariantCulture, format, (object)str3,
                        (object)frame.GetFileLineNumber());
                }
            }

            return stringBuilder.ToString();
        }
    }
}