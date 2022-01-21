using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace WarmSnow.WingMod.umm
{
    static class Main
    {
        // 配置
        public class Settings : UnityModManager.ModSettings, IDrawable
        {
            [Header("修改")] [Draw("移动动速度（自己有效）")] public float MySpeed = 2;

            [Draw("基础移动速度（自己商会有效）")] public float BaseSpeed = 5;
            [Draw("制作经验倍数")] public float MakeExpMul = 5;
            [Draw("影响值不减")] public bool InfDecEnable = true;

            [Header("商会")] [Draw("产业建造忽视气候")] public bool IndustryIgnoreClimate = true;
            [Draw("产业建造忽视种类")] public bool IndustryIgnoreMax = true;
            [Draw("工作-清理库存不清理主会")] public bool JobClearStoreIgnoreMain = true;

            public void OnChange()
            {
            }

            public override void Save(UnityModManager.ModEntry modEntry)
            {
                Save(this, modEntry);
            }
        }

        public static UnityModManager.ModEntry mod;
        public static Settings settings;
        private static bool IsKeyShiftHeld = false;


        static void Load(UnityModManager.ModEntry modEntry)
        {
            settings = Settings.Load<Settings>(modEntry);

            Harmony.DEBUG = true;
            FileLog.Reset();
            WingLog.Reset();

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            mod = modEntry;
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
                // Stop(); // Perform all necessary steps to stop mod.
            }

            // enabled = value;
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

       
    }
}