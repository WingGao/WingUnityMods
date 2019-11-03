using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityModManagerNet;

namespace TheEscapists2Mod
{
    public static  class Main
    {
        public static bool enabled;
         static  Mod myMod;
        public static UnityModManager.ModEntry.ModLogger logger;
        // 输入参数
        public static float moveSpeedMult = 2; //移动速度
        public static float stateIncrMult = 5; //状态增加倍率
        public static float stateDescMult = 0.3f; //状态增减少倍率
        // Send a response to the mod manager about the launch status, success or not.
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            if(Main.myMod == null)
            {
                myMod = new Mod();
            }
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            modEntry.OnGUI = OnGUI;
            modEntry.OnToggle = OnToggle;
            logger = modEntry.Logger;
            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            return true;
        }

        public  static void OnGUI(UnityModManager.ModEntry obj)
        {
            GUILayout.Label("移动速度倍数", GUILayout.Width(100f));
            var speed = GUILayout.TextField(moveSpeedMult.ToString(), GUILayout.Width(100f));
            GUILayout.Label("状态增加倍率", GUILayout.Width(100f));
            var stateIncrMultN = GUILayout.TextField(stateIncrMult.ToString(), GUILayout.Width(100f));
            GUILayout.Label("状态增减少倍率", GUILayout.Width(100f));
            var stateDescMultN = GUILayout.TextField(stateDescMult.ToString(), GUILayout.Width(100f));

            if (GUILayout.Button("Apply"))
            {
                float.TryParse(speed, out moveSpeedMult);
                float.TryParse(stateIncrMultN, out stateIncrMult);
                float.TryParse(stateDescMultN, out stateDescMult);
                //Player.health = h;
                //Player.weapon.ammo = a;
            }
        }


        [HarmonyPatch(typeof(CharacterStats), "ModifyStat_Internal")]
        static class CharacterStats_ModifyStat_Internal
        {
            static bool Prefix(CharacterStats __instance,ref float change, ref HiddenFloat stat, HiddenFloat ___m_Heat)
            {
                if (!Main.enabled || !__instance.m_bIsPlayer)
                    return true;
                logger.Log(String.Format("CharacterStats-change {0} {1}", change, stat.GetValue()));
                if(___m_Heat == stat) //跳过警戒
                {
                    logger.Log(String.Format("CharacterStats-change maybe heat"));
                    return true;
                }
                if(change > 0)
                {
                    change *= stateIncrMult;
                }else
                {
                    change *= stateDescMult;
                }
               
                return true;
            }
        }
        // 移动速度
        [HarmonyPatch(typeof(CharacterMovement), "GetMaxSpeed")]
        static class CharacterMovement_GetMaxSpeed
        {
            static void Postfix(CharacterMovement __instance, ref float __result)
            {
                if (!Main.enabled || !__instance.m_Character.IsPlayer())
                    return ;
                var newSpeed = __result * moveSpeedMult;
                //logger.Log(String.Format("CharacterMovement.GetMaxSpeed old={0} new={1}", __result, newSpeed));
                __result = newSpeed;
                return ;
            }
        }
        // 桌子打开速度
        [HarmonyPatch(typeof(DeskInteraction), "Init")]
        static class DeskInteraction_Init
        {
            static void Postfix(DeskInteraction __instance)
            {
                if (!Main.enabled)
                    return;
                __instance.m_DeskOpeningTime = 0.1f;
            }
        }

    }
}
