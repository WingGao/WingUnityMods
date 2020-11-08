
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityModManagerNet;
using HarmonyLib;
using Teran;
using Newtonsoft.Json;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;

namespace LostCastleMod
{
    [EnableReloading]
    public static class Main
    {
        public static bool enabled = true;
        public static UnityModManager.ModEntry.ModLogger logger;
        public static Settings settings;
        public static Harmony harmony;

        public static void Load(UnityModManager.ModEntry modEntry)
        {
            logger = modEntry.Logger;
            //Main.Logf("test {0}", JsonConvert.SerializeObject(modEntry.Info));

            settings = Settings.Load<Settings>(modEntry);
            //Harmony.DEBUG = true;
            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            modEntry.OnGUI = OnGUI;
            modEntry.OnShowGUI = OnShowGUI;
            //modEntry.OnToggle = OnToggle;


            //modEntry.OnUnload = Unload;
        }
        static bool Unload(UnityModManager.ModEntry modEntry)
        {
            harmony.UnpatchAll();
            return true;
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
        }

        public static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Draw(modEntry);
            //if (GUILayout.Button("立即存档"))
            //{
            //    var hero = DataMgr.Instance.HeroProperty;
            //    HeroProperty.SaveSceneName(hero.SceneName);
            //    HeroProperty.SaveGameProgress(hero.GameProgress);
            //    MsgBoxMgr.ShowMsg_0Btn("保存成功", (string)null, -1, true);
            //}
        }
        public static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        // 金币
        [HarmonyPatch(typeof(Coin), "PickUpOnLocal")]
        public static class CoinPatch
        {

            static bool Prefix(Coin __instance)
            {
                if (!Main.enabled)
                    return true;

                __instance.value *= (int)settings.coinMultiply;
                logger.Log(String.Format("金币加倍 {0}", __instance.value));
                return true;
            }
            static void Postfix(Coin __instance)
            {
                if (!Main.enabled)
                    return;
                __instance.owner.attack_damage_factor = settings.atkMultiply;
            }
        }
        //魂
        [HarmonyPatch(typeof(Soul), "PickUpOnLocal")]
        public static class CoinPatch1
        {

            static bool Prefix(Soul __instance)
            {
                if (!Main.enabled)
                    return true;
                __instance.value *= (int)settings.soulMultiply;
                logger.Log(String.Format("魂加倍 {0}", __instance.value));

                return true;
            }
            static void Postfix(Soul __instance)
            {
                if (!Main.enabled || !settings.soulAddAttack)
                    return;
                __instance.owner.Attribute.Attack += 1;
                logger.Log(String.Format("攻击 {0}", __instance.owner.Attribute.Attack));
            }
        }
        // 食物
        [HarmonyPatch(typeof(Supply), "UseItem")]
        public static class CoinPatch2
        {

            static bool Prefix(Supply __instance)
            {
                if (!Main.enabled)
                    return true;

                __instance.value *= 10;
                logger.Log(String.Format("食物 {0}", __instance.value));

                return true;
            }
        }
        //老虎机
        [HarmonyPatch(typeof(OneArmBandit))]
        public static class CoinPatch3
        {
            [HarmonyPrefix]
            [HarmonyPatch("Use")]
            static bool Prefix1(OneArmBandit __instance)
            {
                if (!Main.enabled)
                    return true;

                __instance.had_award_prob = 100;
                __instance.drop_money_prob = 1;
                __instance.drop_armor_prob = 1;
                __instance.drop_cthulhu_prob = 1;
                __instance.drop_item_prob = 1;
                __instance.drop_weapon_prob = 1;
                __instance.drop_potion_prob = 1;
                __instance.drop_punish_prob = 1;
                __instance.drop_supply_prob = 1;
                __instance.drop_act_props_prob = 1;
                switch (settings.dropType)
                {
                    case OneArmBandit.AwardType.Item:
                        __instance.drop_item_prob = 100;
                        break;
                    case OneArmBandit.AwardType.Armor:
                        __instance.drop_armor_prob = 100;
                        break;
                    case OneArmBandit.AwardType.Weapon:
                        __instance.drop_weapon_prob = 100;
                        break;
                }

                return true;
            }
        }
        //老虎机直接出结果
        [HarmonyPatch(typeof(RollingBar))]
        public static class RollingBarPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("Roll")]
            static bool Prefix1(RollingBar __instance, OneArmBandit ___oneArmBandit)
            {
                if (!Main.enabled)
                    return true;
                ___oneArmBandit.RollingEnd();
                return false;
            }
        }
            //魂上限突破
            [HarmonyPatch(typeof(BagSystem), "AddSoul")]
        public static class CoinPatch4
        {
            static bool Prefix(BagSystem __instance, int value,ref ObscuredInt ___m_soul_secret)
            {
                if (!Main.enabled)
                    return true;
                //__instance.UpdateSoul(___m_soul + value);
                if (!__instance.photonView.isMine)
                    return false;
                ___m_soul_secret += value;
                if (___m_soul_secret <= 0)
                    ___m_soul_secret = 0;
                __instance.photonView.RPC("RpcUpdateSoul", PhotonTargets.All, (object)(int)___m_soul_secret);
                return false;
            }
        }
        public static void ApplySettings()
        {
            //logger.Log(String.Format("ApplySettings {0}", settings.attackDamage1));
            //DataMgr.Instance.HeroProperty.attackDamage1 = settings.attackDamage1;
            //var dressedBySlot1 = wlh.Equipment.GetDressedBySlot(0) as wlh.Weapon;
            //dressedBySlot1.attackDamage *= settings.attackMultiply;
        }
        // 界面配置
        public class Settings : UnityModManager.ModSettings, IDrawable
        {
            [Header("状态")]
            [Draw("金币倍率", Precision = 1, Min = 0), Space(5)] public int coinMultiply = 1;
            [Draw("魂倍率", Precision = 1, Min = 0), Space(5)] public int soulMultiply = 1;
            [Draw("魂获得增加攻击", Precision = 1, Min = 0), Space(5)] public bool soulAddAttack = true;
            [Draw("攻击倍率", Precision = 1, Min = 0), Space(5)] public float atkMultiply = 1f;
            [Draw("老虎机掉落", Precision = 1, Min = 0), Space(5)] public OneArmBandit.AwardType dropType = OneArmBandit.AwardType.Item;

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
    }
}
