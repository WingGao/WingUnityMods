using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WingGao.Mod
{
    public class WingPatch
    {
        private static Boolean _wingPatched = false;

        public static void StartPatch()
        {
            Debug.Log("WingPatch start");
            if (!_wingPatched)
            {
                _wingPatched = true;
                var harmony = new Harmony("net.wingao.mod");
                Harmony.DEBUG = true;
                var assembly = Assembly.GetExecutingAssembly();
                FileLog.Log($"patch {assembly.FullName}");
                harmony.PatchAll(assembly);
                //开启服务
                WingWS.Start();
            }
        }

        [HarmonyPatch(typeof(Class_A), "GenText")]
        class MapManager_Patch
        {
            static void Postfix(ref string __result)
            {
                __result += " wing";
            }
        }
    }
}