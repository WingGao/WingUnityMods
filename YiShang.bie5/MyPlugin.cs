using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HarmonyLib.Tools;
using UnityEngine;
using WingUtil.Harmony;

namespace YiShang.WingMod.bie5
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class MyPlugin : BaseUnityPlugin
    {
        public const string GUID = "WingMod";
        public const string NAME = "WingMod";
        public const string VERSION = "0.0.1";
        public static Harmony harmony = new Harmony("WingMod");
        public static MyPlugin Instance;
        private ConfigEntry<int> CnfOreDropMul; //矿石掉落倍率
        private ConfigEntry<float> CnfMoveSpeedMul; //移动速度

        public static void Log(String s)
        {
            Instance.Logger.LogInfo(s);
        }

        private void Awake()
        {
            Instance = this;
            HarmonyFileLog.Enabled = true;
            // Plugin startup logic
            Logger.LogInfo($"Plugin WingMod is loaded!");
            harmony.PatchAll(typeof(MyPatcher));
            CnfOreDropMul = Config.Bind("Global", "CnfOreDropMul", 1, "Rocks drop multiplier");
            CnfMoveSpeedMul = Config.Bind("Global", "CnfMoveSpeedMul", 1f, "Movement speed multiplier");
            //手动patch
            // var randomArray_RandomItem = AccessTools.Method(typeof(RandomArray), nameof(RandomArray.RandomItem), new Type[] {typeof(int).MakeByRefType()});
            // harmony.Patch(randomArray_RandomItem, null, new HarmonyMethod(AccessTools.Method(typeof(MyPatcher), nameof(MyPatcher.RandomArray_Patch))));

            // UniverseLib.Universe.Init(() => MyUI.Init());
        }


        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
            // Config.Clear();
        }

        private void Update()
        {
            // if (MyUI.uiBase != null)
            // {
            //     if (InputManager.GetKeyDown(KeyCode.F2))
            //         MyUI.Instance.Toggle();
            // }
        }

        [HarmonyPatch]
        static class MyPatcher
        {
            /// <summary>
            /// 任务无上限
            /// </summary>
            /// <param name="instructions"></param>
            /// <returns></returns>
            [HarmonyPatch(typeof(getRenwuWindowClass), nameof(getRenwuWindowClass.getButtonFunc))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> getButtonFunc_Patch(IEnumerable<CodeInstruction> instructions)
            {
                ILCursor cursor = new ILCursor(instructions);
                // cursor.LogTo(Log,"getButtonFunc");
                if (cursor.TryGotoNext(i => i.Instruction.MatchCallByName("System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>::get_Count")))
                {
                    cursor.Index += 1;
                    cursor.Next.Instruction.opcode = OpCodes.Ldc_I4_S;
                    cursor.Next.Instruction.operand = 9999;
                }

                cursor.LogTo(Log, "getButtonFunc_Patch");
                return cursor.Context.AsEnumerable();
            }

            /// <summary>
            /// 讨价还价-简单
            /// </summary>
            [HarmonyPatch(typeof(BargainWindowClass), nameof(BargainWindowClass.clickPauseButton))]
            [HarmonyPrefix]
            static void clickPauseButton_Patch(BargainWindowClass __instance,float ___width3)
            {
                var p = __instance.buoy.transform.localPosition; // = new Vector3((float) (___width3 / 2.0 - 1), __instance, 0);
                p.x = (float) (___width3 / 2.0 - 1);
                __instance.buoy.transform.localPosition = p;
            }
        }
    }
}