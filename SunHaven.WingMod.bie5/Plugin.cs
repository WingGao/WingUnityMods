using System;
using System.Collections.Generic;
using System.Diagnostics;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Wish;

namespace SunHaven.WingMod.bie5
{
    [BepInPlugin("WingMod", "WingMod", "0.0.2")]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony harmony = new Harmony("WingMod");
        public static Plugin Instance;
        private ConfigEntry<int> CnfOreDropMul; //矿石掉落倍率

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
        }


        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
            // Config.Clear();
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