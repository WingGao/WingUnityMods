using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using HarmonyLib.Tools;
using WuLin;

namespace Wulin.WingMod.bie6c
{
    [BepInPlugin("WingMod", "WingMod", "0.0.1")]
    public class Plugin : BasePlugin
    {
        public static Plugin Instance;

        public ConfigEntry<float> KongfuExpMultiplier;

        public override void Load()
        {
            Instance = this;
            KongfuExpMultiplier = Config.Bind("修改", "KongfuExpMultiplier", 10f, new ConfigDescription("武功经验倍率"));
            // Harmony.CreateAndPatchAll(typeof(Plugin));
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            HarmonyFileLog.Enabled = true;
            // Plugin startup logic
            Log.LogInfo($"Plugin WingMod is loaded!");
        }

        public static void LogInfo(string s)
        {
            Instance.Log.LogInfo(s);
        }

        // WuLin.KungfuInstance.AddExp
        [HarmonyPatch]
        static class MyPatcher
        {
            [HarmonyPatch(typeof(KungfuInstance))]
            static class KungfuInstancePatch
            {
                [HarmonyPatch(nameof(KungfuInstance.AddExp))]
                [HarmonyPrefix]
                static void AddExp_Patch(ref int amount)
                {
                    amount = (int)(amount * Instance.KongfuExpMultiplier.Value);
                    LogInfo($"AddExp_Patch {amount} {new System.Diagnostics.StackTrace()}");
                }
            }
        }
    }
}