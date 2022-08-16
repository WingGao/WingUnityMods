using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using France.Game.controller;
using France.Game.model.level;
using France.Game.model.role;
using FranceGame.Plugins;
using Game.Client;
using HarmonyLib;

namespace WingMod
{
    [BepInPlugin("WingMod", "WingMod", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony harmony = new Harmony("WingMod");
        public static Plugin Instance;
        

        private void Awake()
        {
            Instance = this;
            // Plugin startup logic
            Logger.LogInfo($"Plugin WingMod is loaded!");
            harmony.PatchAll(typeof(GameLoggerPatcher));
            harmony.PatchAll(typeof(RolePatcher));
        }

        private void OnDestroy()
        {
            harmony?.UnpatchAll();
        }

        // [HarmonyPatch(typeof(Debugger))]
        static class GameLoggerPatcher
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Debugger), "Log", new Type[] {typeof(LogLevel), typeof(object)})]
            static void Prefix1(object message)
            {
                Instance.Logger.LogInfo("[GameLog]"+message);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Debugger), "Log", new Type[] {typeof(LogLevel), typeof(string), typeof(object[])})]
            static void Prefix2(string format, params object[] args)
            {
                Instance.Logger.LogInfo(string.Format("[GameLog]"+format, args));
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
            static void InitPost(Role __instance,int characterId)
            {
                Instance.Logger.LogInfo($"Role.Init {characterId}");
            }
            
            [HarmonyPrefix]
            [HarmonyPatch(typeof(BattleSceneController), "attackTarget")]
            static void BattleSceneController_attackTarget_Post(BattleSceneController __instance,ref GFightUnit attacker)
            {
                Instance.Logger.LogInfo($"attack {attacker.getRole().getBasicInfo().name}  hp={attacker.getHp()}");
            }
        }
    }
}