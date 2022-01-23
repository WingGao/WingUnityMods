using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using WingUtil;

namespace WingMod
{
    static class Main
    {
        // 配置
        public class Settings : UnityModManager.ModSettings, IDrawable
        {
            [Header("修改")] [Draw("掉落神兵/圣物/技能最高等级")]
            public bool DropWeaponLevel3 = true;

            [Draw("剑返自动触发")] public bool FlySwardAutoBack = true;
            [Draw("剑返无冷却")] public bool FlySwardBackNoCd = true;

            // [Draw("飞剑冷却百分比", Max = 100, Min = 0, Precision = 0)]
            // public float FlySwardBackCoolPer = 50;

            [Draw("减伤百分比", Max = 100, Min = 0, Precision = 0)]
            public float EnemyDamagePer = 99;

            [Draw("不会死亡")] public bool DeathDisable = true;


            [Draw("伤害倍率", Min = 1, Precision = 0)] public float DamageMultiply = 1f;

            [Draw("魂不减")] public bool SoulNotDecrease = true;

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

        static void LogF(string str)
        {
            UnityModManager.Logger.Log(str);
        }

        //圣物掉落
        [HarmonyPatch(typeof(PotionDropPool))]
        public static class PotionDropPool_Pop
        {
            [HarmonyPrefix]
            [HarmonyPatch("Pop", new[] {typeof(PN), typeof(int), typeof(Vector3)})]
            public static void Prefix1(ref int level)
            {
                if (settings.DropWeaponLevel3) level = 2; //金色
            }

            [HarmonyPrefix]
            [HarmonyPatch("Pop", new[] {typeof(int), typeof(int), typeof(Vector3)})]
            static void Prefix2(ref int level)
            {
                Prefix1(ref level);
            }
        }

        //技能掉落
        [HarmonyPatch(typeof(SkillDropPool), "Pop")]
        public static class SkillDropPool_Pop
        {
            static void Prefix(ref bool isGolden)
            {
                if (settings.DropWeaponLevel3) isGolden = true;
            }
        }

        //神兵掉落
        [HarmonyPatch(typeof(MagicSwordPool), "Pop")]
        public static class MagicSwordPool_Pop
        {
            static void Prefix(ref int level)
            {
                if (settings.DropWeaponLevel3) level = 3; //绝世
            }
        }

        //神兵词条
        [HarmonyPatch(typeof(MagicSwordControl), "RandomEntrys")]
        public static class MagicSwordControl_RandomEntrys
        {
            static void Prefix(ref int level)
            {
                if (settings.DropWeaponLevel3) level = 3; //绝世
            }
        }

        // 减伤
        [HarmonyPatch(typeof(PlayerAnimControl), "DealDamage")]
        public static class PlayerAnimControl_DealDamage
        {
            static void Prefix(ref float damage)
            {
                damage *= (100 - settings.EnemyDamagePer) / 100;
            }

            static void Postfix(PlayerAnimControl __instance)
            {
                if (settings.DeathDisable) __instance.playerParameter.HP = Math.Max(1f, __instance.playerParameter.HP);
            }
        }

        // 角色控制
        [HarmonyPatch(typeof(PlayerAnimControl))]
        public static class PlayerAnimControlPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("Souls", MethodType.Setter)]
            static bool SoulSetterPrefix(int __0, int ___souls)
            {
                //魂不减
                if (settings.SoulNotDecrease)
                {
                    LogF($"SoulSetterPrefix {___souls} ==> {__0}");
                    if (__0 > 0 && __0 < ___souls) return false;
                }

                return true;
            }
        }

        // 飞剑冷却完成
        static bool IsFlySwardBackCdOk()
        {
            return PlayerAnimControl.instance.DrawCoolDownTimer >= PlayerAnimControl.instance.drawCoolDown *
                (1.0 - (double) PlayerAnimControl.instance.playerParameter.DRAW_SWORD_CD_REDUCE);
        }

        // 怪物受伤
        [HarmonyPatch(typeof(EnemyControl), "DealDamage")]
        public static class EnemyControl_DealDamage
        {
            static void Prefix(EnemyControl __instance, ref float damage)
            {
                //自动剑返
                if (settings.FlySwardAutoBack && PlayerAnimControl.instance.playerParameter.SWORDS_COUNT == 0 &&
                    IsFlySwardBackCdOk())
                {
                    PlayerAnimControl.instance.ShouldDrawSword = true;
                    // 冷却时间
                    PlayerAnimControl.instance.DrawCoolDownTimer = 0;
                }

                // 剑返无cd
                if (settings.FlySwardBackNoCd)
                    PlayerAnimControl.instance.DrawCoolDownTimer = PlayerAnimControl.instance.drawCoolDown;
                // 伤害倍率
                damage *= settings.DamageMultiply;
            }
        }
    }
}