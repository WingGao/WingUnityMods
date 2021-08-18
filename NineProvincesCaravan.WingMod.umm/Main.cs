using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Framework;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using WingUtil;
using WingUtil.Harmony;

namespace WingMod
{
    static class Main
    {
        public static UnityModManager.ModEntry mod;

        static void Load(UnityModManager.ModEntry modEntry)
        {
            Harmony.DEBUG = true;
            FileLog.Reset();

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            mod = modEntry;
            modEntry.OnToggle = OnToggle;

            WingLog.Log("hello wing load");
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

        static void Run()
        {
            WingLog.Log("hello wing run");
        }

        [HarmonyPatch(typeof(DlgSettings), "OnUpdate")]
        static class DlgSettings_Patch
        {
            static void Postfix(DlgSettings __instance, ref UICLabel ___lbVersion)
            {
                ___lbVersion.text = "V_Wing";
            }
        }

        // 测试IL
        [HarmonyPatch(typeof(DlgSettings), "UpdateFps")]
        public static class DlgSettings_UpdateFps_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                ILCursor c = new ILCursor(instructions);
                if (c.TryGotoNext(MoveType.Before,
                    inst => inst.Instruction.MatchCallByName("Framework.UICLabel::set_text")))
                {
                    FileLogF.Log("Find Framework.UICLabel::set_text");
                    c.Next.Previous.Previous.Instruction.operand = "f3";
                    // c.Emit(OpCodes.Ldarg_0);
                    // c.Emit(OpCodes.Ldstr, "x");
                    // c.Next.Instruction.opcode = OpCodes.Nop;
                    // c.Next.Instruction.operand = null;
                }

                return c.Context.AsEnumerable();
            }
        }

        // 城镇显示商品信息
        [HarmonyPatch(typeof(PnlTownTips), "OnTownTip")]
        public static class PnlTownTips_OnTownTip_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
            {
                ILCursor c = new ILCursor(instructions);
                if (c.TryGotoNext(MoveType.Before,
                    inst => inst.Instruction.MatchCallByName("TradeMgr::GetHot")))
                {
                    /**
                     *
        IL_01c4: callvirt     instance class GoodHotCls TradeMgr::GetHot(string, int32)  //find
        IL_01c9: stloc.s      hot
        IL_01cb: ldloc.s      hot
        IL_01cd: brfalse.s    IL_020b
        IL_01cf: ldloc.s      hot //inst +4
        IL_01d1: callvirt     instance bool GoodHotCls::get_know()
        IL_01d6: brfalse.s    IL_020b
        IL_01d8: ldloc.s      hot
        IL_01da: callvirt     instance bool GoodHotCls::get_up()
        IL_01df: brtrue.s     IL_020b
        IL_01e1: ldloc.s      hot  //定义label
        IL_01e3: callvirt     instance bool GoodHotCls::get_up()
        IL_01e8: brtrue.s     IL_01f1
        IL_01ea: ldstr        "<color=green>↓</color>"
                     */
                    c.Index += 4;
                    var hotLabel = gen.DefineLabel();
                    var hotLabelInstr = c.Context.Instructions[c.Index + 6];
                    FileLogF.Log("hotLabel {0}", hotLabelInstr);
                    hotLabelInstr.Instruction.WithLabels(hotLabel);
                    c.Next.Instruction.opcode = OpCodes.Br;
                    c.Next.Instruction.operand = hotLabel;
                }

                return c.Context.AsEnumerable();
            }
        }

        // 跳过小游戏
        [HarmonyPatch(typeof(LittleGameMgr), "GameStart")]
        public static class LittleGameMgr_GameStart_Patch
        {
            static bool Prefix(GameType type, float diff, HandleGameResult handle)
            {
                if (handle == null) return true;
                DlgWaitLoading.Show("跳过小游戏...", (System.Action)(() =>
                {
                    switch (type)
                    {
                        case GameType.Talks:
                            handle(true, 1, 100);
                            break;
                        case GameType.ClickStar:
                            // 获得背包上线~50
                            handle(true, 1, 5 * 100, null);
                            break;
                        default:
                            handle(true, 1);
                            break;
                    }
                }), 0.3f);
                return false;
            }
        }

        // 好友不减
        //public float PlusFriend(string npckey, float value, string npc2key = null, bool tip = true, bool plus = false)
        [HarmonyPatch(typeof(NpcMgr), "PlusFriend",
            new Type[] { typeof(String), typeof(float), typeof(string), typeof(bool), typeof(bool) })]
        public static class NpcMgr_PlusFriend_Patch
        {
            static void Prefix(ref float value)
            {
                if (value > 0) value = 30;
                else value = -1;
            }
        }

        // 疲劳降低10倍
        [HarmonyPatch(typeof(RoleMapCls), "PlusTired")]
        public static class RoleMapCls_PlusTired
        {
            static void Prefix(ref float v)
            {
                if (v > 0) v /= 10;
                else v *= 2;
            }
        }
        // 必逃跑
        [HarmonyPatch(typeof(DlgFight), "GetRunPre")]
        public static class DlgFight_GetRunPre
        {
            static void Postfix(ref float __result)
            {
                __result = 100f;
            }
        }

        public static void LogILs(ILCursor c, string tag = "IL")
        {
            WingLog.Log("=== {0} BEGIN ===", tag);
            for (int i = 0; i < c.Instrs.Count; i++)
            {
                var v = c.Instrs[i];
                var inst = v.Instruction;
                // WingLog.Log("> {0}", inst.FormatArgument());
                WingLog.Log("{0} @ {1}", i, v.Instruction.ToString());
            }

            WingLog.Log("=== {0} END ===", tag);
        }
    }
}