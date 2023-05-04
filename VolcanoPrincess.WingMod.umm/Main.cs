﻿using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using Object = UnityEngine.Object;

namespace WingMod
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Header("修改")] [Draw("掉落神兵/圣物/技能最高等级")]
        public bool DropWeaponLevel3 = true;

        [Draw("见闻掉落无限制")] public bool DropStoryUnlimited = true;
        [Draw("必出书怪")] public bool ShuguaiEnable = false;
        [Draw("必出特殊房间")] public bool RandomRoomDisable = true;
        [Draw("技能无限随机")] public bool RandomSkillInf = true;
        [Draw("快速精炼")] public bool PotionRefineQuick = true;
        [Draw("毒宗碎片增加")] public bool DuPopEnable = true;
        [Draw("剑返自动触发")] public bool FlySwardAutoBack = true;
        [Draw("剑返无冷却")] public bool FlySwardBackNoCd = true;
        [Draw("飞剑按住自动")] public bool FlySwardKeepPress = true;

        // [Draw("飞剑冷却百分比", Max = 100, Min = 0, Precision = 0)]
        // public float FlySwardBackCoolPer = 50;

        [Draw("减伤百分比", Max = 100, Min = 0, Precision = 0)]
        public float EnemyDamagePer = 99;

        [Draw("不会死亡")] public bool DeathDisable = true;


        [Draw("伤害倍率", Min = 1, Precision = 0)] public float DamageMultiply = 1f;

        [Draw("魂不减")] public bool SoulNotDecrease = true;

        [Draw("角色信息快捷键")] public KeyBinding ShowInfoKeyBinding;
        [Draw("武器编辑快捷键")] public KeyBinding ShowWeaponKeyBinding;


        public void OnInit()
        {
        }


        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void OnChange()
        {
        }
    }

    static class Main
    {
        #region 通用配置

        public static UnityModManager.ModEntry mod;

        // 配置
        public static Settings settings;

        public static bool Enable => mod.Enabled;


        /// <summary>
        /// 加载
        /// https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
        /// </summary>
        /// <param name="modEntry"></param>
        static void Load(UnityModManager.ModEntry modEntry)
        {
            settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            settings.OnInit();

            Harmony.DEBUG = true;
            FileLog.Reset();

            mod = modEntry;
            var harmony = new Harmony(mod.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            LogF("WingMod load");
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Draw(modEntry);
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* active or inactive */)
        {
            if (value)
            {
                Run(); // Perform all necessary steps to start mod.
            }
            else
            {
                Stop(); // Perform all necessary steps to stop mod.
            }

            return true; // If true, the mod will switch the state. If not, the state will not change.
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        static void Run()
        {
            LogF("WingMod run");
        }

        static void Stop()
        {
            LogF("WingMod Stop");
        }

        static void LogF(string str)
        {
            UnityModManager.Logger.Log(str, "[WingMod] ");
        }

        // [HarmonyPatch(typeof(Debug))]
        public static class UnityDebugPatch
        {
            private static ILogger MyLogger => new Logger(new MyDebugLogHandler());

            [HarmonyPostfix]
            [HarmonyPatch("unityLogger", MethodType.Getter)]
            public static void LogGet(ref ILogger __result, ref ILogger ___s_DefaultLogger, ref ILogger ___s_Logger)
            {
                ___s_DefaultLogger = MyLogger;
                ___s_Logger = MyLogger;
                __result = MyLogger;
            }

            [HarmonyPostfix]
            [HarmonyPatch("IsLoggingEnabled")]
            public static void IsLoggingEnabledGet(ref bool __result)
            {
                __result = true;
            }

            class MyDebugLogHandler : ILogHandler
            {
                public void LogFormat(LogType logType, Object context, string format, params object[] args)
                {
                    // StackTrace st = new StackTrace(3, true);
                    /**
                     *  at UnityEngine.Logger.Log (UnityEngine.LogType logType, System.Object message) [0x00000] in <3d993dea89b649118f5e3c1a995c56fc>:0  skip2
  at UnityEngine.Debug.Log (System.Object message) [0x00000] in <3d993dea89b649118f5e3c1a995c56fc>:0 skip3
  at PlayerPrefControl.StartFromBegining () [0x00000] in <a311cad17a3041fba8282ec312c03a2e>:0 
  at StageControl.StageControl.Start_Patch1 (StageControl ) [0x00000] in <a311cad17a3041fba8282ec312c03a2e>:0  InitObjs!8.762533
                     */
                    StackFrame callStack = new StackFrame(3, true);
                    // WingLog.GetStackFrame(4)
                    UnityModManager.Logger.Log(String.Format(format, args),
                        $"[{logType}] [] ");
                }

                public void LogException(Exception exception, Object context)
                {
                    UnityModManager.Logger.LogException(" ", exception, "[Error]");
                }
            }
        }

        #endregion

        #region 该游戏的修改

        #endregion
    }
}