using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace WingMod
{
    static class Main
    {
        public static UnityModManager.ModEntry mod;
        
        static void Load(UnityModManager.ModEntry modEntry)
        {
            
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            mod = modEntry;
            modEntry.OnToggle = OnToggle;
            
            mod.Logger.Log("hello wing load");
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
            mod.Logger.Log("hello wing run");
        }
    }
}