using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace WingMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static Action<string> _logger = (Action<string>)(s => { });
        public static Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        public static float mul = 100f;

        [MethodImpl((MethodImplOptions)256)]
        public static void logger(string s, string extra_id = "") => Plugin._logger(extra_id + ":" + s);

        private void Awake()
        {
            Plugin._logger = new Action<string>(this.Logger.LogInfo);
            Plugin.logger("开始注入Awake");
            if (this.Config.Bind<bool>("config", "naodongTryDoMagicEffect", true, "脑洞大爆炸").Value)
            {
                Plugin.logger("patch naodongTryDoMagicEffect");
                Plugin.harmony.PatchAll(typeof(Plugin.naodongTryDoMagicEffect));
                Plugin.logger("naodongTryDoMagicEffect patched");
            }

            if ((double)(Plugin.mul = this.Config.Bind<float>("config", "BrainMgrNaodongAddValue", Plugin.mul, "脑洞层数加成").Value) > 1.0)
            {
                Plugin.logger("patch BrainMgrNaodongAddValue");
                Plugin.harmony.PatchAll(typeof(Plugin.BrainMgrNaodongAddValue));
                Plugin.logger("BrainMgrNaodongAddValue patched");
            }

            if (this.Config.Bind<bool>("config", "NewTrickMgrTryAddTrickByRate", true, "100%获得新特性").Value)
            {
                Plugin.logger("patch NewTrickMgrTryAddTrickByRate");
                Plugin.harmony.PatchAll(typeof(Plugin.NewTrickMgrTryAddTrickByRate));
                Plugin.logger("NewTrickMgrTryAddTrickByRate patched");
            }

            if (this.Config.Bind<bool>("config", "NewTrickMgrGetFaceFightList", true, "使用最优而非随机trick").Value)
            {
                Plugin.logger("patch NewTrickMgrGetFaceFightList");
                Plugin.harmony.PatchAll(typeof(Plugin.NewTrickMgrGetFaceFightList));
                Plugin.logger("NewTrickMgrGetFaceFightList patched");
            }

            if (this.Config.Bind<bool>("config", "NewFaceRewardMgrFaceRewardFetchTimes", true, "索取次数永不耗尽").Value)
            {
                Plugin.logger("patch NewFaceRewardMgrFaceRewardFetchTimes");
                Plugin.harmony.PatchAll(typeof(Plugin.NewFaceRewardMgrFaceRewardFetchTimes));
                Plugin.logger("NewFaceRewardMgrFaceRewardFetchTimes patched");
            }

            if (this.Config.Bind<bool>("config", "WorkWndDoFightValueAdd", true, "其实打工非常赚钱……").Value)
            {
                Plugin.logger("patch WorkWndDoFightValueAdd");
                Plugin.harmony.PatchAll(typeof(Plugin.WorkWndDoFightValueAdd));
                Plugin.logger("WorkWndDoFightValueAdd patched");
            }

            if (this.Config.Bind<bool>("config", "LovingValue", true, "大家都是爱你的").Value)
            {
                Plugin.logger("patch LovingValue");
                Plugin.harmony.PatchAll(typeof(Plugin.NewSocialGirlMgrBeginDate));
                Plugin.harmony.PatchAll(typeof(Plugin.NewSocialBoyMgrBeginDate));
                Plugin.logger("LovingValue patched");
            }

            if (this.Config.Bind<bool>("config", "WaitForSecondsConstructor", true, "10倍速？").Value)
            {
                Plugin.logger("patch WaitForSecondsConstructor");
                Plugin.harmony.PatchAll(typeof(Plugin.WaitForSecondsConstructor));
                Plugin.logger("WaitForSecondsConstructor patched");
            }

            Plugin.logger("Awake完成");
        }

        [HarmonyPatch(typeof(naodong), "TryDoMagicEffect")]
        public static class naodongTryDoMagicEffect
        {
            private static bool Prefix(ref naodong __instance, ref int ___m_magicType)
            {
                if (___m_magicType == 0 || ___m_magicType == 9 || ___m_magicType == 11)
                {
                    __instance.DoMagic_random();
                    ___m_magicType = 2;
                }

                return true;
            }
        }

        [HarmonyPatch]
        public static class BrainMgrNaodongAddValue
        {
            private static float Postfix(float __result) => __result * Plugin.mul;
        }

        [HarmonyPatch(typeof(NewTrickMgr), "TryAddTrickByRate")]
        public static class NewTrickMgrTryAddTrickByRate
        {
            private static bool Prefix(ref int _trickRate)
            {
                _trickRate = 100;
                return true;
            }
        }

        [HarmonyPatch(typeof(NewTrickMgr), "GetFaceFightList")]
        public static class NewTrickMgrGetFaceFightList
        {
            private static bool Prefix(ref List<int> __result, NewTrickMgr __instance)
            {
                List<int> intList;
                if (__instance.LearnedTrickes.Count < 9)
                {
                    intList = new List<int>((IEnumerable<int>)__instance.LearnedTrickes);
                }
                else
                {
                    intList = new List<int>();
                    List<int> list = __instance.LearnedTrickes.OrderBy<int, int>((Func<int, int>)(x => -SingleTon<TableMgr>.Instance.TableTrick.GetItem(x).Attack)).ToList<int>();
                    for (int index1 = 0; index1 < 9; ++index1)
                    {
                        int index2 = 0;
                        intList.Add(list[index2]);
                        list.RemoveAt(index2);
                    }
                }

                __result = intList;
                return false;
            }
        }

        [HarmonyPatch]
        public static class NewFaceRewardMgrFaceRewardFetchTimes
        {
            private static int Postfix(int __result) => Mathf.Max(__result, 1);
        }

        [HarmonyPatch(typeof(WorkWnd), "DoFightValueAdd")]
        public static class WorkWndDoFightValueAdd
        {
            private static void Postfix(ref float ___m_fightingValue) => ___m_fightingValue = (float)(100 * ((int)___m_fightingValue / 100 + 1));
        }

        [HarmonyPatch(typeof(NewSocialGirlMgr), "BeginDate")]
        public static class NewSocialGirlMgrBeginDate
        {
            private static void Postfix(
                ref int ___m_alertValue,
                ref List<NewSocialGirlNpcInfo> ___m_infos)
            {
                ___m_alertValue = 0;
                foreach (NewSocialGirlNpcInfo socialGirlNpcInfo in ___m_infos)
                    socialGirlNpcInfo.LovingValue = Mathf.Max(socialGirlNpcInfo.LovingValue, 90);
            }
        }

        [HarmonyPatch(typeof(NewSocialBoyMgr), "BeginDate")]
        public static class NewSocialBoyMgrBeginDate
        {
            private static void Postfix(
                ref int ___m_alertValue,
                ref List<NewSocialGirlNpcInfo> ___m_npcInfos)
            {
                ___m_alertValue = 0;
                foreach (NewSocialGirlNpcInfo socialGirlNpcInfo in ___m_npcInfos)
                    socialGirlNpcInfo.LovingValue = Mathf.Max(socialGirlNpcInfo.LovingValue, 90);
            }
        }

        [HarmonyPatch]
        public static class WaitForSecondsConstructor
        {
            private static bool Prefix(ref float seconds)
            {
                seconds *= 0.1f;
                return true;
            }
        }
    }
}