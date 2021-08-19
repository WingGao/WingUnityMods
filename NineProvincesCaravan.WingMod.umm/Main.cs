﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AutoData;
using Framework;
using GameEnum;
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
            WingLog.Reset();

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
                ___lbVersion.text = DlgSettings.Version.Ver + " patch by Wing";
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


        // 注册UI
        [HarmonyPatch(typeof(PlayerMgr), "Init")]
        public static class PlayerMgr_Init
        {
            static void Postfix()
            {
                // var myUI = new GUILayout();
                // Camera.current.
            }
        }

        [HarmonyPatch(typeof(PlayerMgr), "ClearData")]
        public static class PlayerMgr_ClearData
        {
            static void Postfix()
            {
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

        // 移动速度5倍
        [HarmonyPatch(typeof(RoleMapCls), "speed", MethodType.Getter)]
        public static class RoleMapCls_speed
        {
            static void Postfix(ref float __result)
            {
                __result *= 5;
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

        // 打印城镇信息，方便调试
        [HarmonyPatch(typeof(TownMgr), "list", MethodType.Getter)]
        public static class TownMgr_list
        {
            static void Prefix(List<TownCls> ____list, out bool __state)
            {
                __state = ____list == null;
            }

            static void Postfix(List<TownCls> ____list, bool __state)
            {
                if (__state && ____list != null)
                {
                    ____list.ForEach(v => { FileLogF.Log("Town {0}[{2}] Pos={1}", v.name, v.position, v.townKey); });
                }
            }
        }

        // 任务显示坐标
        // des在nameTips之前，所有只改这里
        [HarmonyPatch(typeof(MissDataCls), "des", MethodType.Setter)]
        public static class MissDataCls_nameTips
        {
            static void Postfix(MissDataCls __instance, ref string ____nameTips, ref string ____des)
            {
                TownCls targetTown;
                if (__instance is UMissTransportSafe ms)
                {
                    targetTown = ms.targetTown;
                }
                else if (__instance is UMissTransport ms2)
                {
                    targetTown = ms2.targetTown;
                }
                else if (__instance is RMissTransport ms3)
                {
                    targetTown = ms3.targetTown;
                }
                else
                {
                    return;
                }

                ____nameTips += PosStr(targetTown);
                ____des += PosStr(targetTown);
            }
        }

        // 灯谜显示答案
        [HarmonyPatch(typeof(NpcMgr), "GetAsks")]
        public static class NpcMgr_GetAsks
        {
            static void Postfix(ref List<string> __result)
            {
                __result.ForEach(v => v = "X " + v);
            }
        }

        // 制造不消失
        [HarmonyPatch(typeof(global::FurmulaMgr.FurmulaCls), "UpdatePrice")]
        public static class FurmulaMgr_UpdatePrice
        {
            static void Prefix(global::FurmulaMgr.FurmulaCls __instance)
            {
                __instance.count = Math.Max(__instance.count, 3);
            }
        }

        // 制造10倍经验
        [HarmonyPatch(typeof(DlgMake), "GetExp")]
        public static class DlgMake_GetExp
        {
            static void Postfix(ref int __result)
            {
                if (__result > 0) __result *= 10;
            }
        }

        // 制作配方显示
        [HarmonyPatch(typeof(PnlMakeItem), "Load")]
        public static class PnlMakeItem_Load
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
            {
                ILCursor c = new ILCursor(instructions);
                // LogILs(c);
                if (c.TryGotoNext(MoveType.Before,
                    inst => inst.Instruction.MatchCallByName("GameMgr::get_DebugKey")))
                {
                    c.RemoveRange(1);
                    c.Next.Instruction.opcode = OpCodes.Br;
                }

                // LogILs(c, "Patched");
                return c.Context.AsEnumerable();
            }
        }

        // 武器配方显示
        [HarmonyPatch(typeof(PnlMakeWeaponItem), "Load")]
        public static class PnlMakeWeaponItem_Load
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
            {
                ILCursor c = new ILCursor(instructions);
                if (c.TryGotoNext(MoveType.Before,
                    inst => inst.Instruction.MatchCallByName("GameMgr::get_DebugKey")))
                {
                    c.RemoveRange(1);
                    c.Next.Instruction.opcode = OpCodes.Br;
                }

                return c.Context.AsEnumerable();
            }
        }

        // 添加坐标到人物信息
        [HarmonyPatch(typeof(PnlKnowNpcItem), "OnInHandle")]
        public static class PnlKnowNpcItem_OnInHandle
        {
            static void Postfix(PnlKnowNpcItem __instance, UICLabel ___lbName, bool enter)
            {
                if (enter)
                {
                    var dlg = BaseInstance<DlgMgr>.Instance.GetOpen(DlgMgr.EnumDlg.DlgItemDes) as DlgItemDes;
                    var lbDes = AccessTools.Field(typeof(DlgItemDes), "lbDes").GetValue(dlg) as UICLabel;
                    lbDes.text += $"\n在{GameComm.GetPlaceStr(__instance.role)}{PosStr(__instance.role.position)}";
                }
            }
        }

        // 书价格
        [HarmonyPatch(typeof(BookMgr), "LoadData")]
        public static class BookMgr_LoadData
        {
            static void Postfix()
            {
                foreach (KeyValuePair<int, BookData> keyValuePair in BaseInstance<GameConfig>.Instance.DBook)
                {
                    int key = keyValuePair.Key;
                    BookData bookData = keyValuePair.Value;
                    bookData.key = key;
                    bookData.price = Math.Min(100000, bookData.price); //最高10w
                    var item = AutoData.Item.GetForId(bookData.key);
                    item.price = bookData.price;
                    FileLogF.Log($"修改书的价格 {bookData.name} ${item.price}");
                }
            }
        }


        public static String PosStr(ObjectBase town)
        {
            return PosStr(town.position);
        }

        public static String PosStr(Vector3 position)
        {
            return $"({position.x:F1}, {position.y:F1})";
        }


        static void DlgItemDes_Show(UIBase uic, string title, string des, int width = 0)
        {
            AccessTools.Method(typeof(DlgItemDes), "Show",
                    new Type[] { typeof(UIBase), typeof(string), typeof(string), typeof(int) })
                .Invoke(null, new object[] { uic, title, des, width });
        }

        static void DlgItemDes_Close()
        {
            var method = AccessTools.Method(typeof(DlgItemDes), "Close");
            method.Invoke(null, null);
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