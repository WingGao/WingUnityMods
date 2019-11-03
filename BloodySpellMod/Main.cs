
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

        public static void Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            modEntry.OnGUI = OnGUI;
            modEntry.OnToggle = OnToggle;
            logger = modEntry.Logger;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            return true;
        }

        public  static void OnGUI(UnityModManager.ModEntry obj)
        {
            if (GUILayout.Button("立即存档"))
            {
                var hero = DataMgr.Instance.HeroProperty;
                HeroProperty.SaveSceneName(hero.SceneName);
                HeroProperty.SaveGameProgress(hero.GameProgress);
            }
        }


        //[HarmonyPatch(typeof(CharacterStats), "ModifyStat_Internal")]
        //static class CharacterStats_ModifyStat_Internal
        //{
        //    static bool Prefix(CharacterStats __instance,ref float change, ref HiddenFloat stat, HiddenFloat ___m_Heat)
        //    {
        //        if (!Main.enabled || !__instance.m_bIsPlayer)
        //            return true;
        //        logger.Log(String.Format("CharacterStats-change {0} {1}", change, stat.GetValue()));
        //        if(___m_Heat == stat) //跳过警戒
        //        {
        //            logger.Log(String.Format("CharacterStats-change maybe heat"));
        //            return true;
        //        }
        //        if(change > 0)
        //        {
        //            change *= stateIncrMult;
        //        }else
        //        {
        //            change *= stateDescMult;
        //        }
               
        //        return true;
        //    }
        //}
        //// 移动速度
        //[HarmonyPatch(typeof(CharacterMovement), "GetMaxSpeed")]
        //static class CharacterMovement_GetMaxSpeed
        //{
        //    static void Postfix(CharacterMovement __instance, ref float __result)
        //    {
        //        if (!Main.enabled || !__instance.m_Character.IsPlayer())
        //            return ;
        //        var newSpeed = __result * moveSpeedMult;
        //        //logger.Log(String.Format("CharacterMovement.GetMaxSpeed old={0} new={1}", __result, newSpeed));
        //        __result = newSpeed;
        //        return ;
        //    }
        //}
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
