using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UniverseLib.Input;
using Wish;
using Logger = BepInEx.Logging.Logger;

namespace SunHaven.WingMod.bie5
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class MyPlugin : BaseUnityPlugin
    {
        public const string GUID = "WingMod";
        public const string NAME = "WingMod";
        public const string VERSION = "0.0.2";
        public static Harmony harmony = new Harmony("WingMod");
        public static MyPlugin Instance;
        private ConfigEntry<int> CnfOreDropMul; //矿石掉落倍率

        public static void Log(String s)
        {
            Instance.Logger.LogInfo(s);
        }

        private void Awake()
        {
            Instance = this;
            // Plugin startup logic
            Logger.LogInfo($"Plugin WingMod is loaded!");
            harmony.PatchAll(typeof(MyPatcher));
            CnfOreDropMul = Config.Bind("Global", "CnfOreDropMul", 1, "Rocks drop Multiplier");
            //手动patch
            // var randomArray_RandomItem = AccessTools.Method(typeof(RandomArray), nameof(RandomArray.RandomItem), new Type[] {typeof(int).MakeByRefType()});
            // harmony.Patch(randomArray_RandomItem, null, new HarmonyMethod(AccessTools.Method(typeof(MyPatcher), nameof(MyPatcher.RandomArray_Patch))));

            UniverseLib.Universe.Init(1f, () => PluginUI.Init(), ((s, type) => Logger.LogInfo(s)), new()
            {
                Disable_EventSystem_Override = false,
                Force_Unlock_Mouse = true,
            });
        }


        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
            // Config.Clear();
        }

        private void Update()
        {
            if (PluginUI.uiBase != null)
            {
                if (InputManager.GetKeyDown(KeyCode.F2))
                    PluginUI.Instance.ToggleUI();
            }
        }

        //矿石ID
        private static HashSet<int> OreIds = new HashSet<int>()
        {
            ItemID.CopperOre, ItemID.IronOre, ItemID.GoldOre, ItemID.AdamantOre, ItemID.MithrilOre, ItemID.SuniteOre, ItemID.GloriteOre, ItemID.SandstoneOre,
            ItemID.ElvenSteelOre
        };

        static class MyPatcher
        {
            // [HarmonyPostfix]
            // [HarmonyPatch(typeof(RandomArray), nameof(RandomArray.RandomItem), new Type[] {typeof(int)})]
            public static void RandomArray_Patch(out int amount, ItemData __result)
                // [HarmonyPatch(typeof(RandomArray), nameof(RandomArray.RandomItem), typeof(int).MakeByRefType())]
                // public static void RandomArray_Patch(RandomArray __instance, ref ItemData __result)
            {
                Instance.Logger.LogInfo($"RandomArray_Patch id={__result.id} {__result.name}");
                amount = 1;
                if (OreIds.Contains(__result.id))
                {
                    amount *= Instance.CnfOreDropMul.Value;
                }
            }

            /// <summary>
            /// 矿石破碎
            /// </summary>
            /// <param name="__instance"></param>
            /// <param name="___dropMultiplier"></param>
            /// <param name="___dropRange"></param>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Rock), nameof(Rock.Die))]
            static void Rock_Die(Rock __instance, ref float ___dropMultiplier, ref Vector2 ___dropRange)
            {
                Instance.Logger.LogInfo($"Rock_Die id={__instance.name}");
                ___dropMultiplier = Instance.CnfOreDropMul.Value;
                // ___dropRange.Set(Instance.CnfOreDropMul.Value, Instance.CnfOreDropMul.Value);
            }
        }
    }
}