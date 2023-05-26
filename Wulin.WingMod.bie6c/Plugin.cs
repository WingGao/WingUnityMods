using Il2CppSystem;
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
        public ConfigEntry<float> NengliExpMultiplier;
        public ConfigEntry<float> HaoGanExpMultiplier;
        public ConfigEntry<bool> DaodeFlag;

        public override void Load()
        {
            Instance = this;
            KongfuExpMultiplier = Config.Bind("修改", "武功经验倍率", 10f);
            NengliExpMultiplier = Config.Bind("修改", "能力经验倍率", 10f);
            HaoGanExpMultiplier = Config.Bind("修改", "好感倍率", 10f);
            DaodeFlag = Config.Bind("修改", "道德不减", true);
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
            [HarmonyPatch(typeof(GameCharacterInstance))]
            static class GameCharacterInstance_Patch
            {
                /// <summary>
                /// 属性计算，像经验倍率什么的
                /// </summary>
                /// <param name="key"></param>
                /// <param name="origin"></param>
                [HarmonyPatch(nameof(GameCharacterInstance.ModifyValueByPropAsBuff),
                    new[] { typeof(string), typeof(int), typeof(GameCharacterInstance.FinalPropSource) })]
                [HarmonyPostfix]
                static void ModifyValueByPropAsBuff_Patch(string key, int origin, ref int __result)
                {
                    Decimal r = new Decimal(__result);
                    ModifyValueByPropAsBuff2_Patch(key, new Decimal(origin), ref r);
                    __result = (int)r;
                }

                [HarmonyPatch(nameof(GameCharacterInstance.ModifyValueByPropAsBuff),
                    new[] { typeof(string), typeof(Decimal), typeof(GameCharacterInstance.FinalPropSource) })]
                [HarmonyPostfix]
                static void ModifyValueByPropAsBuff2_Patch(string key, Decimal origin, ref Decimal __result)
                {
                    if (__result <= 0) return; //排除减小的情况

                    var needLog = true;
                    switch (key)
                    {
                        case CharacterPropKey.武功使用经验:
                        case CharacterPropKey.实战经验获得:
                            __result = origin * (Decimal)Instance.KongfuExpMultiplier.Value;
                            break;
                        case CharacterPropKey.非宠物好感提升加成:
                        case CharacterPropKey.宠物好感提升加成:
                            __result = origin * (Decimal)Instance.HaoGanExpMultiplier.Value;
                            break;
                        case CharacterPropKey.点穴:
                        case CharacterPropKey.视野失效:
                            needLog = false;
                            break;
                        default:
                            if (key.StartsWith("属性提升加成_"))
                            {
                                var nk = key.Substring("属性提升加成_".Length);
                                if (nk.StartsWith("能力经验_") || nk.EndsWith("经验") || nk == CharacterPropKey.宠物饱食度)
                                {
                                    // 属性提升加成_敏捷经验
                                    // 属性提升加成_名声经验
                                    // 属性提升加成_能力经验_生存_识图
                                    __result = origin * (Decimal)Instance.NengliExpMultiplier.Value;
                                }
                            }

                            break;
                    }


                    if (needLog)
                        LogInfo($"ModifyValueByPropAsBuff_Patch key={key} origin={origin} __result={__result}");
                }

                [HarmonyPatch(nameof(GameCharacterInstance.ChangeOriginProp))]
                [HarmonyPostfix]
                static void ChangeOriginProp_Patch(string key, Decimal value)
                {
                    LogInfo($"ChangeOriginProp_Patch key={key} value={value}");
                }

                [HarmonyPatch(nameof(GameCharacterInstance.ChangeAdditionProp))]
                [HarmonyPrefix]
                static void ChangeAdditionProp_Patch(string key, ref Decimal value)
                {
                    if (Instance.DaodeFlag.Value && key == CharacterPropKey.礼节)
                    {
                        if (value < 0) value *= -1;
                    }

                    LogInfo($"ChangeAdditionProp_Patch key={key} value={value}");
                }
            }
        }

        [HarmonyPatch(typeof(KungfuInstance))]
        static class KungfuInstancePatch
        {
            /// <summary>
            /// 武功使用经验
            /// </summary>
            /// <param name="amount"></param>
            // [HarmonyPatch(nameof(KungfuInstance.AddExp))]
            // [HarmonyPrefix]
            static void AddExp_Patch(ref int amount)
            {
                amount = (int)(amount * Instance.KongfuExpMultiplier.Value);
                LogInfo($"AddExp_Patch {amount} {new System.Diagnostics.StackTrace()}");
            }
        }
    }
}