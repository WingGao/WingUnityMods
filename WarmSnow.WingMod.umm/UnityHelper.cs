using System;
using System.Linq;
using UnityEngine;

namespace WingUtil
{
    public static class UnityHelper
    {
        public static String Version = "2022年1月25日22:53:42";

        /// <summary>
        /// 获取MonoBehaviour对象包含的所有Components
        /// </summary>
        /// <param name="mbe"></param>
        /// <returns></returns>
        public static String FmtAllComponents(MonoBehaviour mbe)
        {
            return String.Join(";", mbe.GetComponents(typeof(Component))
                .Select(v => v.GetType().FullName).Where(v => !v.StartsWith("UnityEngine.")));
        }
    }
}