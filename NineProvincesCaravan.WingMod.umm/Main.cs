using System.Reflection;
using Framework;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using WingUtil;

namespace WingMod
{
    static class Main
    {
        public static UnityModManager.ModEntry mod;
        
        static void Load(UnityModManager.ModEntry modEntry)
        {
            Harmony.DEBUG = true;
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            mod = modEntry;
            modEntry.OnToggle = OnToggle;
            
            WingLog.Log("hello wing load");
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* active or inactive */)
        {
            if (value)
            {
                Run(); // Perform all necessary steps to start mod.
            }
            else
            {
                // Stop(); // Perform all necessary steps to stop mod.
            }

            // enabled = value;
            return true; // If true, the mod will switch the state. If not, the state will not change.
        }

        static void Run()
        {
            WingLog.Log("hello wing run");
        }
        
        [HarmonyPatch(typeof(DlgSettings), "OnUpdate")]
        static class DlgSettings_Patch
        {
            static void Postfix(DlgSettings __instance,ref UICLabel ___lbVersion)
            {
                ___lbVersion.text = "V_Wing";
            }
        }
    }
}