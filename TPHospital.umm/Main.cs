using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TH20;
using UnityEngine;
using UnityModManagerNet;

namespace WingMod
{
    
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Header("修改")] 
        [Draw("培训加成")]
        public float LearnMul = 1;
        [Header("员工")] 
        [Draw("休息倍率")]
        public float StaffBreakMul = 0.5f;


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
    
    public class Main
    {
        public static UnityModManager.ModEntry mod;
        // 配置
        public static Settings settings;
        static void LogF(string str)
        {
            UnityModManager.Logger.Log(str, "[WingMod] ");
        }
        /// <summary>
        /// 加载
        /// https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
        /// </summary>
        /// <param name="modEntry"></param>
        static void Load(UnityModManager.ModEntry modEntry)
        {
            settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            settings.OnInit();
            
            // Harmony.DEBUG = true;
            // FileLog.Reset();
            // WingLog.Reset();

            mod = modEntry;
            var harmony = new Harmony(mod.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            // modEntry.OnShowGUI = OnShowGUI;
            // modEntry.OnHideGUI = OnHideGUI;
            //
            // EnemyControlPatch.Init();

            LogF("WingMod load");
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Draw(modEntry);
        }
        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        //全局
        [HarmonyPatch(typeof(GameAlgorithms))]
        static class GameAlgorithmsPatch
        {
            /// <summary>
            /// 修改培训加成
            /// </summary>
            [HarmonyPatch("CalculateTrainingPointLearnRate")]
            [HarmonyPostfix]
            static void Postfix(ref float __result)
            {
                __result *= settings.LearnMul;
            }
        }
        //员工补丁
        [HarmonyPatch(typeof(Staff))]
        static class StaffPatch
        {
            /// <summary>
            /// 修改员工休息时间
            /// </summary>
            [HarmonyPatch("GetBreakLength")]
            [HarmonyPostfix]
            static void GetBreakLengthPostfix(ref float __result)
            {
                __result *= settings.StaffBreakMul;
            }
        }
    }
}