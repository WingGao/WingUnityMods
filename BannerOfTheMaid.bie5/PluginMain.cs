using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using France.Game.controller;
using France.Game.model.level;
using France.Game.model.role;
using France.Resource;
using FranceGame.Plugins;
using Game.Client;
using HarmonyLib;
using UnityEngine;

namespace WingMod
{
    [BepInPlugin("WingMod", "WingMod", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony harmony = new Harmony("WingMod");
        public static Plugin Instance;
        private ConfigEntry<float> ExpMultVal;
        private ConfigEntry<Boolean> ExpMultEnable;
        private ConfigEntry<Boolean> ExpGainSkip;
        private ConfigEntry<Boolean> NoStopEnable;


        private void Awake()
        {
            Instance = this;
            // Plugin startup logic
            Logger.LogInfo($"Plugin WingMod is loaded!");
            harmony.PatchAll(typeof(GameLoggerPatcher));
            harmony.PatchAll(typeof(RolePatcher));

            // ExpMultVal = Config.Bind("Global", "ExpMultVal", 1f, new ConfigDescription("经验倍率", new AcceptableValueRange<float>(0f, 10f)));
            // ExpMultVal = Config.Bind("Global", "ExpMultVal", 1f, new ConfigDescription("经验倍率", new AcceptableValueRange<float>(0f, 10f)));
            ExpMultVal = Config.Bind("Global", "ExpMultVal", 1f, "经验倍率");
            ExpMultEnable = Config.Bind("Global", "ExpMultEnable", false, "经验倍率");
            ExpGainSkip = Config.Bind("Global", "ExpGainSkip", true, "经验动画跳过");
            NoStopEnable = Config.Bind("Global", "NoStopEnable", false, "无限行动");
        }

        private void OnDestroy()
        {
            harmony?.UnpatchAll();
            // Config.Clear();
        }

        // [HarmonyPatch(typeof(Debugger))]
        static class GameLoggerPatcher
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Debugger), "Log", new Type[] {typeof(LogLevel), typeof(object)})]
            static void Prefix1(object message)
            {
                Instance.Logger.LogInfo("[GameLog]" + message);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Debugger), "Log", new Type[] {typeof(LogLevel), typeof(string), typeof(object[])})]
            static void Prefix2(string format, params object[] args)
            {
                Instance.Logger.LogInfo(string.Format("[GameLog]" + format, args));
            }
        }
        // static class UIComponentPatcher
        // {
        //     [HarmonyPrefix]
        //     [HarmonyPatch(typeof(UIComponent), "Open")]
        //     static void OpenPrefix(UIComponent __instance)
        //     {
        //         Instance.Logger.LogInfo($"Open {__instance.uiName}");
        //     }
        // }

        /// France.Game.controller.BattleSceneController.currentOPerActor 战斗元素
        /// 
        /// 
        static class RolePatcher
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Role), MethodType.Constructor, new Type[] {typeof(int)})]
            static void InitPost(Role __instance, int characterId)
            {
                Instance.Logger.LogInfo($"Role.Init {characterId}");
            }

            // 升级的时候全属性增加
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Role), "lvUp")]
            static void Role_lvUp(Role __instance)
            {
                CharacterRoleData basicInfo = __instance.getBasicInfo();
                CharacterClassData classInfo = __instance.getClassInfo();
                var toLv = __instance.lv + 1;

                LvUpPropertyDataCollection propertyDataCollection = !basicInfo.lvupData.ContainsKey(__instance.classId)
                    ? basicInfo.lvupData.First<KeyValuePair<int, LvUpPropertyDataCollection>>().Value
                    : basicInfo.lvupData[__instance.classId];
                Instance.Logger.LogInfo($"Role_lvUp {basicInfo.name} lv={toLv} {propertyDataCollection}");
                if (propertyDataCollection.datas.ContainsKey(toLv))
                {
                    CharacterLvUpPropertyData data = propertyDataCollection.datas[toLv];
                    data.growthHP = Math.Max(data.growthHP, 1);
                    data.growthATT = Math.Max(1, data.growthATT);
                    data.growthDEF[0] = Math.Max(1, data.growthDEF[0]);
                    data.growthDEF[1] = Math.Max(1, data.growthDEF[1]);
                    data.growthDEX = Math.Max(1, data.growthDEX);
                    data.growthSPD = Math.Max(1, data.growthSPD);
                    data.growthLUCK = Math.Max(1, data.growthLUCK);
                }
            }

            // 最少获得经验
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Role), "gainExp")]
            static void Role_gainExp(ref int exp)
            {
                if (exp <= 0)
                {
                    exp = 3;
                }

                if (Instance.ExpMultEnable.Value) exp = Mathf.CeilToInt(exp * Instance.ExpMultVal.Value);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(BattleSceneController), "attackTarget")]
            static void BattleSceneController_attackTarget_Post(BattleSceneController __instance, ref GFightUnit attacker, ref GActor targetActor)
            {
                var role = attacker.getRole();
                var defenderUnit = (GFightUnit) targetActor;
                var defRole = defenderUnit.getRole();
                // 自己人武器耐久全满
                var list = new GFightUnit[] {attacker, defenderUnit};
                list.ForEach(f =>
                {
                    if (f.getTeamId() == 0)
                    {
                        var fWeapon = f.getWeapon();
                        if (fWeapon != null) fWeapon.count = fWeapon.getItemData().maxCount;
                    }
                });

                Instance.Logger.LogInfo($"attack {role.getBasicInfo().name}({role.characterId}) teamId={attacker.getTeamId()}  hp={attacker.getHp()} ");
                Instance.Logger.LogInfo(
                    $"defender {defRole.getBasicInfo().name}({defRole.characterId}) teamId={defenderUnit.getTeamId()}  hp={defenderUnit.getHp()} ");
            }

            // 自己人不死
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GFightUnit), "setHp")]
            static void GFightUnit_setHp_pre(GFightUnit __instance, ref float value)
            {
                if (__instance.getTeamId() == 0)
                {
                    //自己人
                    if (value <= 1)
                    {
                        value = 1;
                    }
                }
            }
            
            // 自己人无限行动
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GFightUnit), "setDone")]
            static void GFightUnit_setDone(GFightUnit __instance, ref bool done)
            {
                if (Instance.NoStopEnable.Value && __instance.getTeamId() == 0 && done)
                {
                    done = false;
                }
            }

            // 对话框无延迟
            [HarmonyPrefix]
            [HarmonyPatch(typeof(PopDialogManager), "setDelay")]
            static void PopDialogManager_setDelay(ref int delayFrame)
            {
                delayFrame = 0;
            }

            //经验动画
            [HarmonyPostfix]
            [HarmonyPatch(typeof(PopCharacterExpManager), "ShowExpMove")]
            static void PopCharacterExpManager_ShowExpMove(PopCharacterExpManager __instance, ref float ____speed,ref float ____lastTime)
            {
                if (Instance.ExpGainSkip != null && Instance.ExpGainSkip.Value)
                {
                    ____speed = 0.01f;
                    ____lastTime = Time.time;
                }
                else
                {
                    ____speed = 0.1f;
                }
                // return true;
            }
        }
    }
}