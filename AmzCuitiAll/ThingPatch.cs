using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using XiaWorld;

namespace AmzCuiTiAll
{
    [HarmonyPatch(typeof(Thing))]
    internal static class ThingPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("AddSpecialFlag")]
        [HarmonyPatch(new Type[] { typeof(int), typeof(int) })]
        static bool AddSpecialFlagPrefix(int name)
        {
            //无限制搜魂大法
            if (name == (int) g_emNpcSpecailFlag.FLAG_SeachSoul && AmzCuiTiAll.SouHunEnabled)
            {
                return false;
            }

            return true;
        }
    }
}