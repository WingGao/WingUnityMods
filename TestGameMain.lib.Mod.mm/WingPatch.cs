﻿using System;
using System.Reflection;
using HarmonyLib;
using TestGameMain;
using TestGameMain.Mod.mm;

namespace WingGao.Mod
{
    public class WingPatch
    {
        private static Boolean _wingPatched = false;

        public static void StartPatch()
        {
            if (!_wingPatched)
            {
                _wingPatched = true;
                var harmony = new Harmony("net.wingao.mod");
                Harmony.DEBUG = true;
                var assembly = Assembly.GetExecutingAssembly();
                FileLog.Log($"patch {assembly.FullName}");
                harmony.PatchAll(assembly);
                //开启服务
                WingHttpServer.Start();
            }
        }

        [HarmonyPatch(typeof(ClassA), "GetNamePrivate")]
        class ClassA_Patch
        {
            static void Postfix(ref string __result)
            {
                __result = "HarmonyPatch";
            }
        }
    }
}