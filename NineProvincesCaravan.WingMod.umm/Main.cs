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
using Object = UnityEngine.Object;

namespace WingMod
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

        [HarmonyPatch(typeof(GameMgr), "Update")]
        public static class GameMgr_Update
        {
            static void Prefix()
            {
                IsKeyShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                // WingLog.Log($"IsKeyShiftDown={IsKeyShiftDown}");
            }
        }

        // 测试 
        [HarmonyPatch(typeof(PnlBase), "OnShow")]
        public static class PnlBase_OnShow
        {
            static void Prefix(PnlBase __instance)
            {
                LogF($"{__instance} OnShow");
            }
        }

        // 测试 
        [HarmonyPatch(typeof(UIManage), "Show")]
        public static class UIManage_Show
        {
            static void Prefix(UIDlg dlg)
            {
                LogF($"{dlg} Show");
            }
        }

        [HarmonyPatch(typeof(DlgSettings), "OnUpdate")]
        static class DlgSettings_Patch
        {
            static void Postfix(DlgSettings __instance, ref UICLabel ___lbVersion)
            {
                ___lbVersion.text = DlgSettings.Version.Ver + " patch by Wing";
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

        // 仓库按住shift点确认不减商品
        [HarmonyPatch(typeof(PnlStoreDes), "OnApply")]
        public static class PnlStoreDes_OnApply
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                ILCursor c = new ILCursor(instructions);
                // LogILs(c);
                var cnt = 0;
                var patch = AccessTools.Method(typeof(PnlStoreDes_OnApply), "PatchChange");
                while (c.TryGotoNext(MoveType.After,
                    inst => inst.Instruction.MatchCallByName("ItemStore::get_change")))
                {
                    cnt++;
                    if (cnt == 3 || cnt == 4) //取neg
                    {
                        c.Index++;
                    }
                    else if (cnt > 4)
                    {
                        break;
                    }

                    if (cnt > 1)
                    {
                        c.Emit(OpCodes.Call, patch);
                    }
                }

                return c.Context.AsEnumerable();
            }

            static int PatchChange(int change)
            {
                if (IsKeyShiftHeld) return Math.Abs(change);
                return change;
            }
        }

        // 工艺典籍不消耗
        [HarmonyPatch(typeof(PlayerMgr), "UseResCraft")]
        public static class PlayerMgr_UseResCraft
        {
            static void Prefix(PlayerMgr __instance, int res)
            {
                if (!__instance.save.dicResCraft.ContainsKey(res))
                    return;
                __instance.save.dicResCraft[res]++;
            }
        }

        // 产业建造条件限制
        [HarmonyPatch(typeof(DlgSelectIndustry), "OnCheckBuild")]
        public static class DlgSelectIndustry_OnCheckBuild
        {
            static void Prefix(DlgSelectIndustry __instance, IndustryData ___selectIndusty, ResYieldData ___selectRes)
            {
                var items = __instance.dicIndusDisRes[___selectIndusty.key][___selectRes.res].Where(disType =>
                {
                    if (disType == DlgSelectIndustry.DisType.Climate && settings.IndustryIgnoreClimate) return false;
                    if (disType == DlgSelectIndustry.DisType.MaxIndus && settings.IndustryIgnoreMax) return false;
                    return true;
                }).ToList();
                __instance.dicIndusDisRes[___selectIndusty.key][___selectRes.res] = items;
            }
        }

        // 商会内成员奖赏降低金钱
        [HarmonyPatch(typeof(DlgChamberRoleFriend), "GetMoneyUp")]
        public static class DlgChamberRoleFriend_GetMoneyUp
        {
            static void Postfix(DlgChamberRoleFriend.RoleFriendUp roleup, ref int __result)
            {
                if (__result > 0) __result = roleup.friendup;
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
                    // FileLogF.Log("hotLabel {0}", hotLabelInstr);
                    hotLabelInstr.Instruction.WithLabels(hotLabel);
                    c.Next.Instruction.opcode = OpCodes.Br;
                    c.Next.Instruction.operand = hotLabel;
                }

                return c.Context.AsEnumerable();
            }
        }

        // 城镇列表显示商品
        [HarmonyPatch(typeof(PnlKnowTownInfo), "OnRefresh")]
        public static class PnlKnowTownInfo_OnRefresh
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
            {
                ILCursor c = new ILCursor(instructions);
                if (c.TryGotoNext(MoveType.Before,
                    inst => inst.Instruction.MatchCallByName("TradeMgr::GetHot")))
                {
                    c.Index += 4;
                    var hotLabel = gen.DefineLabel();
                    var hotLabelInstr = c.Context.Instructions[c.Index + 6];
                    // Debug.Log("hotLabel {0}", hotLabelInstr);
                    hotLabelInstr.Instruction.WithLabels(hotLabel);
                    c.Next.Instruction.opcode = OpCodes.Br;
                    c.Next.Instruction.operand = hotLabel;
                }

                return c.Context.AsEnumerable();
            }
        }

        // NPC列表显示性别
        [HarmonyPatch(typeof(PnlKnowNpcItem), "OnRefresh")]
        public static class PnlKnowNpcItem_OnRefresh
        {
            static void Postfix(PnlKnowNpcItem __instance, UICLabel ___lbName)
            {
                ___lbName.text = (__instance.role.rsave.sex == Sex.Female ? "女" : "男") + "|" + ___lbName.text;
            }
        }

        // npc列表支持性别排序
        [HarmonyPatch(typeof(DlgSelectRoles), "OrderBy")]
        public static class DlgSelectRoles_OrderBy
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
            {
                ILCursor c = new ILCursor(instructions);
                // LogILs(c);
                if (c.TryGotoNext(MoveType.Before,
                    inst => inst.Instruction.MatchCallByName("RoleBase::get_ikey")))
                {
                    c.Next.Instruction.opcode = OpCodes.Call;
                    c.Next.Instruction.operand = AccessTools.Method(typeof(DlgSelectRoles_OrderBy), "GetIkey");
                }

                // LogILs(c, "Patched");
                return c.Context.AsEnumerable();
            }

            static int GetIkey(RoleCls role)
            {
                if (role.rsave.sex == Sex.Female)
                {
                    return 10000000 + role.ikey;
                }
                else
                {
                    return role.ikey;
                }
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

        // 等待的完成时间为0.3f
        [HarmonyPatch(typeof(DlgWaitLoading), "OnFinsh")]
        public static class DlgWaitLoading_OnFinsh
        {
            static void Postfix(ref float ___timeTotal)
            {
                ___timeTotal = 0.3f;
            }
        }

        // 策略必成功
        [HarmonyPatch(typeof(ChamberCls), "OnTacticsFailure")]
        public static class ChamberCls_OnTacticsFailure
        {
            static bool Prefix(ChamberCls __instance, TacticsCls tac)
            {
                if (__instance.isPlayer)
                {
                    AccessTools.Method(typeof(ChamberCls), "OnTacticsComplete").Invoke(__instance, new object[] { tac });
                    return false;
                }

                return true;
            }
        }

        // 转运只需1天
        [HarmonyPatch]
        public static class DlgChamberTransport_OnEnterTransport
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(DlgChamberTransport), "OnEnterTransport");
                yield return AccessTools.Method(typeof(DlgMenuStore), "OnEnterTransport");
                yield return AccessTools.Method(typeof(DlgMenuStore), "OnEnterTransportFest");
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
            {
                ILCursor c = new ILCursor(instructions);

                if (original.Name == "OnEnterTransportFest")
                {
                    if (c.TryGotoNext(MoveType.Before,
                        inst => inst.Instruction.MatchCallByName("System.Linq.Enumerable::OrderBy")))
                    {
                        LogF("find OnEnterTransportFest");
                        c.Index += 2;
                        c.Emit(OpCodes.Call, AccessTools.Method(typeof(DlgChamberTransport_OnEnterTransport), "OnEnterTransportFest_OrderBy"));
                    }
                }

                if (c.TryGotoNext(MoveType.After,
                    inst => inst.Instruction.MatchCallByName("GameComm::GetDisDay")))
                {
                    c.Emit(OpCodes.Pop, null);
                    c.Emit(OpCodes.Ldc_I4_1, null);
                }

                // LogILs(c, "Patched");
                return c.Context.AsEnumerable();
            }

            // 重新排序，将主会移到最后
            private static List<(IBuildBag, int)> OnEnterTransportFest_OrderBy(List<(IBuildBag, int)> list)
            {
                ChamberCls chamberMain =  BaseInstance<ChamberMgr>.Instance.GetChamberMain(PlayerCaravan.Caravan.chamber.masterKey);
                var mainIdx = list.FindIndex(x => x.Item1.townKey == chamberMain.townKey);
                if (mainIdx >= 0)
                {
                    list.Add(list[mainIdx]);
                    list.RemoveAt(mainIdx);
                }
                LogF($"OnEnterTransportFest_OrderBy {chamberMain.name} {chamberMain.townKey} idx={mainIdx} {String.Join(", ",list.Select(x=>x.Item1.townKey))}");
                return list;
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

        // 影响值不掉
        [HarmonyPatch(typeof(RoleMapCls), "PlusInf")]
        public static class RoleMapCls_PlusInf
        {
            static void Prefix(RoleMapCls __instance, ref int v)
            {
                if (settings.InfDecEnable)
                {
                    if (v <= 0 && __instance.isPlayer) v = 5;
                }
            }
        }

        // 移动速度 5倍
        [HarmonyPatch(typeof(RoleMapCls), "speed", MethodType.Getter)]
        public static class RoleMapCls_speed
        {
            static void Postfix(RoleMapCls __instance, ref float __result)
            {
                if (__instance.key == PlayerCaravan.Caravan.key)
                    __result *= settings.MySpeed;
            }
        }

        // 基础移动速度
        [HarmonyPatch(typeof(RoleMapCls), "speedBase", MethodType.Getter)]
        public static class RoleMapCls_speedBase
        {
            static void Postfix(RoleMapCls __instance, ref float __result)
            {
                if (__instance.isPlayerCaravan || __instance.isChamberPlayer)
                    __result *= settings.BaseSpeed;
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
                    ____list.ForEach(v => { LogF($"Town {v.name}[{v.townKey}] Pos={v.position}"); });
                }
            }
        }

        // 城镇不掉关系
        [HarmonyPatch(typeof(TownCls), "AddFriend")]
        public static class TownCls_AddFriend
        {
            static void Prefix(ref float v)
            {
                if (v < 0) v = 1;
            }
        }

        // 探索产业必成功
        [HarmonyPatch(typeof(ExploreMgr), "CheckIndusExplore")]
        public static class ExploreMgr_CheckIndusExplore
        {
            static void Postfix(ref bool __result)
            {
                __result = true;
            }
        }

        // 拍卖底价拿
        [HarmonyPatch(typeof(DlgGamePaimai), "LoadGood")]
        public static class DlgGamePaimai_LoadGood
        {
            static void Postfix(ref Dictionary<ItemType, float[]> ___dicMyPricePre)
            {
                foreach (var keyValuePair in ___dicMyPricePre)
                {
                    keyValuePair.Value[0] = 0f;
                    keyValuePair.Value[1] = 0.5f;
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
                if (__result > 0) __result = (int)(__result * settings.MakeExpMul);
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
                    LogF($"修改书的价格 {bookData.name} ${item.price}");
                }
            }
        }

        //城镇快进
        // [HarmonyPatch(typeof(DlgTownTips), "ClickItem", typeof(TownCls))]
        // public static class DlgTownTips_ClickItem
        // {
        //     public static System.Action buildOnEnter(System.Action originAction)
        //     {
        //         FileLogF.Log("buildOnEnter");
        //         return () => { FileLogF.Log("OnEnter"); };
        //     }
        //
        //     static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
        //     {
        //         ILCursor c = new ILCursor(instructions);
        //         LogILs(c);
        //         // 注入到onEnter函数
        //         if (c.TryGotoNext(MoveType.Before,
        //             inst => inst.Instruction.opcode == OpCodes.Ldftn &&
        //                     inst.Instruction.FormatArgument().EndsWith("b__0()")))
        //         {
        //             
        //             var refOnEnter = AccessTools.Method(typeof(DlgTownTips_ClickItem), "buildOnEnter");
        //             c.RemoveRange(2);
        //             c.Prev.Instruction.opcode = OpCodes.Call;
        //             c.Prev.Instruction.operand = refOnEnter;
        //         }
        //
        //         return c.Context.AsEnumerable();
        //     }
        // }
        [HarmonyPatch(typeof(RunNpcMgr), "PushCmd")]
        public static class RunNpcMgr_PushCmd
        {
            static bool Prefix(RunNpcMgr __instance, CmdBaseData cmd, bool isplayer)
            {
                // WingLog.Log(
                //     $"RunNpcMgr_PushCmd 1 key={IsKeyShiftHeld} cmd={cmd} isplayer={isplayer} PC.rolekey={PlayerCaravan.Caravan.roleKey}");
                if (IsKeyShiftHeld && cmd is SNpcCmdMove mm && mm.key == PlayerCaravan.Caravan.roleKey && isplayer)
                {
                    // 停止自动
                    PlayerMgr.isAutoMoveTown = false;
                    __instance.ClearCmd(mm.key);
                    AccessTools.Method(typeof(SNpcCmdMove), "OnEndMove").Invoke(mm, new object[] { false });
                    var town = mm.town;
                    LogF($"RunNpcMgr_PushCmd current={PlayerCaravan.Caravan.position} to={town.position}");
                    //shift按下 立即移动
                    var toPos = new Vector3(town.position.x, town.position.y, PlayerCaravan.Caravan.position.z);
                    PlayerCaravan.Caravan.position = toPos;
                    PlayerCaravan.Caravan.toPos = toPos;
                    PlayerCaravan.Caravan.OnRefshPos();
                    return false;
                }

                return true;
            }
        }

        // 查找物品的城市支撑快速移动
        [HarmonyPatch(typeof(DlgMenuGoods), "OnDgTownsCreateItem")]
        public static class DlgMenuGoods_OnDgTownsCreateItem
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
            {
                ILCursor c = new ILCursor(instructions);
                if (c.TryGotoNext(MoveType.Before,
                    inst => inst.Instruction.opcode == OpCodes.Stfld))
                {
                    c.Emit(OpCodes.Callvirt,
                        AccessTools.Method(typeof(DlgMenuGoods_OnDgTownsCreateItem), "PatchHandler"));
                }

                return c.Context.AsEnumerable();
            }

            static System.Action PatchHandler(System.Action origin)
            {
                return () =>
                {
                    origin();
                    // get town windows
                    DlgTownTips dlgTownTips =
                        (DlgTownTips)BaseInstance<DlgMgr>.Instance.GetOpen(DlgMgr.EnumDlg.DlgTownTips);
                    if (dlgTownTips != null)
                    {
                        // set tip allow autoMove
                        AccessTools.Field(typeof(DlgTownTips), "sType").SetValue(dlgTownTips, 0);
                    }
                };
            }
        }

        #region 商会工作

        // 商队交易城镇
        [HarmonyPatch(typeof(CCmdTradeGood), "GetTradeTown")]
        public static class CCmdTradeGood_GetTradeTown
        {
            static void Postfix(RoleMapCls role, List<TownCls> __result)
            {
                // 打印日志
                LogF($"CCmdTradeGood_GetTradeTown {role.name} : {String.Join(",", __result.Select(x => x.name))}");
            }
        }

        // 清空库存
        [HarmonyPatch(typeof(CACClearStore), "GetChamberList")]
        public static class CACClearStore_GetChamberList
        {
            static void Postfix(ref List<ChamberCls> __result)
            {
                if (__result.Count == 1 || !settings.JobClearStoreIgnoreMain) return;
                __result = __result.Where(x => !x.isMainChamber).ToList();
                LogF($"清理库存 候选 {String.Join(",", __result.Select(x => x.name))}");
            }
        }

        #endregion


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

        static void LogF(string str)
        {
            UnityModManager.Logger.Log(str);
        }
    }
}