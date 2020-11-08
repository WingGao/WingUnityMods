
using Harmony12;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityModManagerNet;
using wlh;

namespace BloodySpellMod
{
    public static  class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry.ModLogger logger;
        public static Settings settings;
        /**
         *  嗜血印修改
         *  1. 添加立即存档
         *  2. 修改华容道退出即通关
         */
        public static void Load(UnityModManager.ModEntry modEntry)
        {
            settings = Settings.Load<Settings>(modEntry);
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            modEntry.OnGUI = OnGUI;
            modEntry.OnShowGUI = OnShowGUI;
            modEntry.OnToggle = OnToggle;
            logger = modEntry.Logger;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            return true;
        }

        public static void OnShowGUI(UnityModManager.ModEntry modEntry)
        {
            logger.Log("OnShowGUI");
            //if (DataMgr.Instance != null && DataMgr.Instance.HeroProperty != null)
            //{
            //    settings.attackDamage1 = DataMgr.Instance.HeroProperty.attackDamage1;
            //}

            //Logf("UI_chufa_new {0}",UI_chufa_new.Instance.z)
            zhuangtai.lifeadd_nan1 *= 3;
            zhuangtai.lifemax_nan1 *= 3;
        }

        public  static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Draw(modEntry);
            if (GUILayout.Button("立即存档"))
            {
                var hero = DataMgr.Instance.HeroProperty;
                HeroProperty.SaveSceneName(hero.SceneName);
                HeroProperty.SaveGameProgress(hero.GameProgress);
                MsgBoxMgr.ShowMsg_0Btn("保存成功", (string)null, -1, true);
            }
        }
        public static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        // 华容道退出即通关
        [HarmonyPatch(typeof(HuaRongDao), "ShowQuitAsk")]
        static class HuaRongDao_ShowQuitAsk
        {
            static bool Prefix(HuaRongDao __instance, ref bool ___isWin)
            {
                if (!Main.enabled)
                    return true;
                logger.Log(String.Format("华容道修改成功"));
                ___isWin = true;
                return true;
            }
        }
        // 攻击修改
        [HarmonyPatch(typeof(zhuangtai), "Update")]
        static class Zhuangtai_Update
        {
            static bool Prefix(zhuangtai __instance)
            {
                return true;
            }
            static void Postfix(zhuangtai __instance)
            {
                if (!Main.enabled)
                    return;
                //zhuangtai.gongji_shixue1 = zhuangtai.gongji_shixue1 + zhuangtai.zhuwuqi_gongjili * settings.attackMultiply;
                //zhuangtai.wuqi_gongjili *= settings.attackMultiply;
                //zhuangtai.fangyu_shixue1 = zhuangtai.fangyu_shixue1 + zhuangtai.zhuangbei_fangyuli * settings.defenceMultiply;
                return;
            }
        }
        [HarmonyPatch(typeof(zhuangtai_zhuangbei), "zhuangbei_quzhi")]
        static class Zhuangtai_zhuangbei_quzhi
        {
            static bool Prefix(zhuangtai_zhuangbei __instance)
            {
                if (!Main.enabled)
                    return true;
                //zhuangtai.zhuwuqi_gongjili += zhuangtai.zhuwuqi_gongjili * settings.attackMultiply;
                return true;
            }
            static void Postfix(zhuangtai __instance)
            {
                if (!Main.enabled)
                    return;
                //zhuangtai.gongji_shixue1 = zhuangtai.gongji_shixue1 + zhuangtai.zhuwuqi_gongjili * settings.attackMultiply;
                //zhuangtai.wuqi_gongjili *= settings.attackMultiply;
               // zhuangtai.fangyu_shixue1 = zhuangtai.fangyu_shixue1 + zhuangtai.zhuangbei_fangyuli * settings.defenceMultiply;
                return;
            }
        }
        [HarmonyPatch(typeof(wlh.Equipment), "GetDressedBySlot")]
        static class Equipment_GetDressedBySlot
        {
            static void Postfix(wlh.Equipment __instance,int slotIndex, ref wlh.Equipment __result )
            {
                if (!Main.enabled)
                    return;
                if(slotIndex == 0)
                {
                    var weapon = __result as wlh.Weapon;
                    weapon.attackDamage *= settings.attackMultiply;
                }
                //zhuangtai.gongji_shixue1 = zhuangtai.gongji_shixue1 + zhuangtai.zhuwuqi_gongjili * settings.attackMultiply;
                //zhuangtai.wuqi_gongjili *= settings.attackMultiply;
                // zhuangtai.fangyu_shixue1 = zhuangtai.fangyu_shixue1 + zhuangtai.zhuangbei_fangyuli * settings.defenceMultiply;
                return;
            }
        }

        public static void ApplySettings() {
            //logger.Log(String.Format("ApplySettings {0}", settings.attackDamage1));
            //DataMgr.Instance.HeroProperty.attackDamage1 = settings.attackDamage1;
            //var dressedBySlot1 = wlh.Equipment.GetDressedBySlot(0) as wlh.Weapon;
            //dressedBySlot1.attackDamage *= settings.attackMultiply;
        }
        // 界面配置
        public class Settings : UnityModManager.ModSettings, IDrawable
        {
            [Header("状态")]
            [Draw("攻击倍率", Precision = 1, Min = 0), Space(5)] public float attackMultiply = 1f;
            [Draw("防御倍率", Precision = 1, Min = 0), Space(5)] public float defenceMultiply = 0f;

            public void OnChange()
            {
                Main.ApplySettings();
            }

            public override void Save(UnityModManager.ModEntry modEntry)
            {
                Save(this, modEntry);
            }
        }

        static void Logf(String format, params object[] args)
        {
            logger.Log(String.Format(format, args));
        }

        //// 桌子打开速度
        //[HarmonyPatch(typeof(DeskInteraction), "Init")]
        //static class DeskInteraction_Init
        //{
        //    static void Postfix(DeskInteraction __instance)
        //    {
        //        if (!Main.enabled)
        //            return;
        //        __instance.m_DeskOpeningTime = 0.1f;
        //    }
        //}

    }
}
