using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using iFActionGame2;
using System.Runtime.InteropServices;


namespace WingMod
{
    
    public static class MyPlugin
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        public static void Hook()
        {
            AllocConsole();
            FileLog.Reset();
            Harmony.DEBUG = true;
            var harmony = new Harmony("WingMod");
            harmony.PatchAll();
            FileLog.Log($"hello {DateTime.Now.ToString()}");

            // var originalMethods = Harmony.GetAllPatchedMethods();
            // foreach (var method in originalMethods)
            // {
            //     FileLog.Log($"Patched {method.Name} {method.FullDescription()}");
            // }
            // FindAss();
            //重定向Console
            // Console.SetError(FileLog.LogWriter);
            // Console.SetOut(FileLog.LogWriter);
        }

        static void FindAss()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                FileLog.Log($"{assembly.FullName} => {assembly.Location}");
            }
        }

        static void GetVersion()
        {
            var tRV = AccessTools.TypeByName("iFActionScript.RV");
            var tVer = AccessTools.Field(tRV, "ver");
            var gameVerStr = tVer.GetValue(null);
            FileLog.Log($"游戏版本 {gameVerStr}");
            // tVer.SetValue(null, gameVerStr + " wing_patched");
        }

        [HarmonyPatch(typeof(Game))]
        public class GamePatch
        {
            [HarmonyPatch("OnLoad")]
            [HarmonyPrefix]
            static void Game_OnLoad_patch()
            {
                FileLog.Log($"OnLoad");
                // var tRV = AccessTools.TypeByName("iFActionScript.RV");
                // var tVer = AccessTools.Field(tRV, "ver");
                // tVer.SetValue(null, "wing_patched");
            }

            [HarmonyPatch("loadScript")]
            [HarmonyPostfix]
            static void Game_loadScript_patch()
            {
                GetVersion();
                AccessTools.Method("iFActionScript.WingSourceHarmPatcher:Patch").Invoke(null, null);
            }
        }
    }
}