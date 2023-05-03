using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using iFActionGame2;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using OpenTK.Windowing.GraphicsLibraryFramework;
using WingUtil.Harmony;

// ！！必须使用该注释区分 using与正文！！
// WingModScript
namespace iFActionScript
{
    /// <summary>
    /// 保存在存档里的配置
    /// </summary>
    public class WingModSavingConfig
    {
        public string key;
        public string value;
    }

    public class WingModSettingJSon
    {
        public int HotKey = IInput.CodeToWinKey(Keys.F4);
        public float MoveSpeed = 1; //移动速度
        public bool CanPenetrate = true; //穿墙
        public bool GiveItemAnyMax = true; //赠送任何东西都是最喜欢
        public bool MakeItemOneTime = true; //一次敲击
        public float MakeItemSpeed = 10; //制作速度
        public float MachineSpeed = 0; //机器生成速度
        public bool TaskNoLimit = true; //任务无上限
        public bool GameTouhuEasy = true; //小游戏投壶最高分
        public bool BuildNoCost = false; //建造不消耗
        public bool QuickCollect = true; //快速搜集
        public bool QuickTool = true; //快速使用工具
        public bool ScienceStudyMin = false; //科技最低要求
        public bool SkillNoCd = false; //技能无cd
        public bool EnemyDrop100 = true; //怪物掉落100%
        public bool ItemNoSub = false; //物品不减
        public Dictionary<string, string> Extensions = new Dictionary<string, string>(); //其他扩展
    }

    public static class WingSourceHarmPatcher
    {
        public static int WINGMOD_SAVING_ID = 459171748;

        /// <summary>
        /// 存档外的独立数据
        /// </summary>
        public static WingModSettingJSon Settings = new WingModSettingJSon();

        /// <summary>
        /// 存档内的MOD数据
        /// </summary>
        public static Dictionary<string, string> ModSaveSettings = new Dictionary<string, string>();

        public static void Patch()
        {
            var p = new Harmony("WingMod.SourcePatcher");
            p.PatchAll();
            RV.ver += " (WingMod v0.0.4)";
            LoadSetting();
            OnPatch();
        }

        public static void OnPatch()
        {
        }

        private static String SaveFile = "WingMod/WingMod.Settings.json";

        static void LoadSetting()
        {
            var fp = IVal.BasePath + SaveFile;
            if (File.Exists(fp))
            {
                var s = File.ReadAllText(fp);
                Settings = JsonConvert.DeserializeObject<WingModSettingJSon>(s);
            }
        }

        public static void SaveSetting()
        {
            File.WriteAllText(IVal.BasePath + SaveFile, JsonConvert.SerializeObject(Settings));
        }

        /// <summary>
        /// 在这里写上数据加载
        /// </summary>
        public static void OnSaveLoad(GMain gmain)
        {
            FileLog.Log("WingSourceHarmPatcher.OnSaveLoad");
        }

        /// <summary>
        /// 在这里写上数据保存
        /// </summary>
        public static void OnSaveWrite(GMain gmain)
        {
            FileLog.Log("WingSourceHarmPatcher.OnSaveWrite");
        }

        [HarmonyPatch]
        public class SourcePatches
        {
            class FakeIWFile : IRWFile
            {
                public FakeIWFile() : base("fake")
                {
                }

                public int ReadInt() => 0;

                public string ReadString() => "";
                public bool ReadBool() => false;
            }

            /// <summary>
            /// 补充MOD变量
            /// </summary>
            // [HarmonyPatch(typeof(DProject), nameof(DProject.initValue))]
            // [HarmonyPostfix]
            // static void DProject_initValue(ref Dictionary<int, object> __result)
            // {
            //     var dv = new DValue(new FakeIWFile());
            //     dv.id = WINGMOD_SAVING_ID;
            //     dv.name = "WINGMOD_SAVING_ID";
            //     dv.type = 2;
            //     dv.defValue = "{}";
            //     dv.staticValue = false;
            //     __result[WINGMOD_SAVING_ID] = dv;
            //     FileLog.Log($"DProject_initValue {dv.id}={dv.name}");
            // }

            /// <summary>
            /// 显示Log
            /// </summary>
            [HarmonyPatch(typeof(RF), nameof(RF.Log))]
            [HarmonyPatch(new[] {typeof(string)})]
            [HarmonyPostfix]
            static void RF_Log(string str)
            {
                Console.WriteLine(str);
            }

            [HarmonyPatch(typeof(RF), nameof(RF.Log))]
            [HarmonyPatch(new[] {typeof(string), typeof(object[])})]
            [HarmonyPostfix]
            static void RF_Log2(string format, object[] objs)
            {
                Console.WriteLine(String.Format(format, objs));
            }

            /// <summary>
            /// 修复CBar的bug
            /// </summary>
            [HarmonyPatch(typeof(CBar), MethodType.Setter)]
            [HarmonyPatch("x")]
            [HarmonyPostfix]
            static void CBar_x(CBar __instance)
            {
                if (__instance.move != null) __instance.move.x = __instance.back.x + __instance.bCof.width - __instance.move.width / 2;
            }

            /// <summary>
            /// 敲击1次
            /// </summary>
            /// <param name="__instance"></param>
            [HarmonyPatch(typeof(WBuildMakeTable), "makeItem")]
            [HarmonyPrefix]
            static void makeItemPatch(WBuildMakeTable __instance, bool ___canMake, ref int ___nowMKT, LItem ___nowSelect, ref int ___makeTimeMax)
            {
                if (Settings.MakeItemOneTime && ___nowSelect != null && ___canMake)
                {
                    ___nowMKT = (___nowSelect.tag as GDFormula).makeTimes;
                    ___makeTimeMax = (int) (30 - Settings.MakeItemSpeed);
                }
            }

            /// <summary>
            /// 快速搜集
            /// </summary>
            [HarmonyPatch(typeof(WCollect), "init")]
            [HarmonyPrefix]
            static void WCollectInitPatch(WCollect __instance, ref int ___max)
            {
                if (Settings.QuickCollect)
                {
                    ___max = 3;
                }
            }

            private static int touhuPreStep;

            /// <summary>
            /// 小游戏投壶最高分
            /// </summary>
            /// <param name="__instance"></param>
            [HarmonyPatch(typeof(GLMapMinGameTouhu), nameof(GLMapMinGameTouhu.update))]
            [HarmonyPrefix]
            static void GLMapMinGameTouhu_update_patch(GLMapMinGameTouhu __instance, int ___step, ref double ___yS, ref double ___xS, ISprite ___nowArrow)
            {
                if (Settings.GameTouhuEasy && ___step == 2 && touhuPreStep != 2) //刚进入弹道阶段
                {
                    ___xS = ___nowArrow.x - 244;
                    ___yS = ___nowArrow.y - 215;
                }

                touhuPreStep = ___step;
            }

            /// <summary>
            /// 小游戏龙舟最高分
            /// </summary>
            /// <param name="__instance"></param>
            [HarmonyPatch(typeof(GLMapMinGameLongZhou), nameof(GLMapMinGameLongZhou.update))]
            [HarmonyPrefix]
            static void GLMapMinGameTouhu_update_patch(ISprite ___qiu, ISprite ___bar, ref bool ___keyOverL)
            {
                if (Settings.GameTouhuEasy)
                {
                    ___qiu.x = ___bar.x + 20; //定住球
                    ___keyOverL = false;
                }
            }

            /// <summary>
            /// 中秋最高分
            /// </summary>
            /// <param name="__instance"></param>
            [HarmonyPatch(typeof(WZhongQiuItem), nameof(WZhongQiuItem.overDo))]
            [HarmonyPostfix]
            static void WZhongQiuItem_overDo_patch()
            {
                if (Settings.GameTouhuEasy)
                {
                    RV.GameData.value[178] = 1;
                }
            }

            /// <summary>
            /// 建造不消耗
            /// </summary>
            /// <param name="__instance"></param>
            [HarmonyPatch(typeof(CBuildMsg), nameof(CBuildMsg.build))]
            [HarmonyPrefix]
            static bool CBuildMsg_build_patch(CBuildMsg __instance)
            {
                if (Settings.BuildNoCost)
                {
                    if (__instance.canBuild) __instance.reDraw();
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(GMain))]
        public class GMainPatch
        {
            //存档加载后
            [HarmonyPatch(MethodType.Constructor, new[] {typeof(string)})]
            [HarmonyPostfix]
            [HarmonyPriority(Priority.First)]
            static void OnSaveLoad(GMain __instance)
            {
                if (__instance.value.ContainsKey(WINGMOD_SAVING_ID))
                {
                    var s = __instance.value[WINGMOD_SAVING_ID] as string;
                    FileLog.Log($"WINGMOD_SAVING: {s}");
                    try
                    {
                        ModSaveSettings = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else
                {
                    FileLog.Log($"WINGMOD_SAVING: null");
                }

                if (ModSaveSettings == null) ModSaveSettings = new Dictionary<string, string>();

                WingSourceHarmPatcher.OnSaveLoad(__instance);
            }

            //写入存档
            [HarmonyPatch(nameof(GMain.write))]
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            static void OnSaveWrite(GMain __instance)
            {
                WingSourceHarmPatcher.OnSaveWrite(__instance);
                var s = JsonConvert.SerializeObject(ModSaveSettings);
                FileLog.Log($"OnSaveWrite: {s}");
                __instance.value[WINGMOD_SAVING_ID] = s;
            }

            /// <summary>
            /// 任务无上限
            /// </summary>
            [HarmonyPatch(nameof(GMain.addTask))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> addTaskPatch(IEnumerable<CodeInstruction> instructions)
            {
                // return instructions;
                ILCursor cursor = new ILCursor(instructions);
                // org: if(num >= 3)
                if (cursor.TryGotoNext(it => it.Instruction.opcode == OpCodes.Ldc_I4_3) && Settings.TaskNoLimit)
                {
                    cursor.Next.Instruction.opcode = OpCodes.Ldc_I4;
                    cursor.Next.Instruction.operand = 9999;
                }

                return cursor.Context.AsEnumerable();
            }

            /// <summary>
            /// 快速工具
            /// </summary>
            [HarmonyPatch(nameof(GMain.toolTimes), MethodType.Getter)]
            [HarmonyPostfix]
            static void toolTimesPatch(ref double __result)
            {
                if (Settings.QuickTool) __result = 30;
            }

            /// <summary>
            /// 物品减少
            /// </summary>
            [HarmonyPatch(nameof(GMain.subItem))]
            [HarmonyPrefix]
            static bool subItemPatch(int id)
            {
                if (id == 980) return false; //天机石不消耗
                if (Settings.ItemNoSub && RV.GameData.getItemNum(id) > 1) return false; //大于1 不减
                return true;
            }
        }

        [HarmonyPatch(typeof(SMain))]
        public class SMainPatch
        {
            [HarmonyPatch(nameof(SMain.init))]
            [HarmonyPrefix]
            static void init_patch(int ___startId)
            {
                FileLog.Log($"SMain.init mapId={___startId}");
            }
        }

        [HarmonyPatch(typeof(LActorEx))]
        public class LActorExPatch
        {
            /// <summary>
            /// 移动速度
            /// </summary>
            /// <param name="__result"></param>
            [HarmonyPatch(nameof(LActorEx.bSpeed))]
            [HarmonyPostfix]
            static void bSpeedPatch(LActorEx __instance, ref double __result)
            {
                __result *= Settings.MoveSpeed;
                __instance.characters.canPenetrate = Settings.CanPenetrate;
            }

            /// <summary>
            /// 技能无cd
            /// </summary>
            [HarmonyPatch(nameof(LActorEx.updateCtrl))]
            [HarmonyPostfix]
            static void updateCtrlPatch(LActorEx __instance)
            {
                if (RV.isBattle && Settings.SkillNoCd)
                {
                    if (RV.HUD.QBattle.cd[1] > 1)
                    {
                        RV.HUD.QBattle.cd[1] = 1;
                    }
                }
            }
        }


        [HarmonyPatch(typeof(NPCBase))]
        public class NPCBasePatch
        {
            /// <summary>
            /// 送礼
            /// </summary>
            [HarmonyPatch(nameof(NPCBase.giftDo))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> giftDoPatch(IEnumerable<CodeInstruction> instructions)
            {
                // return instructions;
                ILCursor cursor = new ILCursor(instructions);
                // 基础好感30
                if (cursor.TryGotoNext(it => it.Instruction.MatchLdsfld("iFActionScript.RV::NowTime")) && Settings.GiveItemAnyMax)
                {
                    // 为了保留label
                    var old = cursor.Next.Instruction.Clone();
                    cursor.Next.Instruction.opcode = OpCodes.Ldc_I4_S;
                    cursor.Next.Instruction.operand = 30;
                    cursor.Index += 1;
                    cursor.Emit(OpCodes.Stloc_0);
                    cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(NPCBasePatch), nameof(AddTel)));
                    cursor.Emit(old.opcode, old.operand);
                }

                return cursor.Context.AsEnumerable();
            }

            static void AddTel()
            {
                //送礼额外加18点经验
                RV.GameData.occ[1].addExp(18, 2);
            }
        }


        [HarmonyPatch(typeof(LEnemy))]
        public class LEnemyPatch
        {
            /// <summary>
            /// 怪物必掉落
            /// </summary>
            [HarmonyPatch(nameof(LEnemy.addItem))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> addItem_patch(IEnumerable<CodeInstruction> instructions)
            {
                // return instructions;
                ILCursor cursor = new ILCursor(instructions);
                if (cursor.TryGotoNext(it => it.Instruction.MatchCallByName("iFActionScript.RF::ProbabilityHit")) && Settings.EnemyDrop100)
                {
                    cursor.Index += 1;
                    cursor.Next.Instruction.opcode = OpCodes.Pop;
                }

                return cursor.Context.AsEnumerable();
            }
        }

        /// <summary>
        /// 设置窗口
        /// </summary>
        [HarmonyPatch(typeof(CTime))]
        public class CTimePatch
        {
            [HarmonyPatch("updateKey")]
            [HarmonyPostfix]
            static void updateKey_Patch()
            {
                if (IInput.isKeyDown(Settings.HotKey))
                {
                    RF.ShowWin(new WModSetting());
                    return;
                }
            }
        }

        //所有机器
        [HarmonyPatch(typeof(GBuildMachine))]
        public class GBuildMachinePatch
        {
            /// <summary>
            /// 修改机器生成速度
            /// </summary>
            /// <param name="__instance"></param>
            [HarmonyPatch(nameof(GBuildMachine.updateS))]
            [HarmonyPostfix]
            static void updateS_Patch(GBuildMachine __instance)
            {
                update_Patch(__instance);
            }

            [HarmonyPatch(nameof(GBuildMachine.update))]
            [HarmonyPostfix]
            static void update_Patch(GBuildMachine __instance)
            {
                if (__instance.nowFormula != null)
                {
                    if (__instance.isCanMake() == 1 && __instance.nowTime > 0)
                    {
                        __instance.nowTime += __instance.nowFormula.time * Settings.MachineSpeed / 100;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(WMenuSys))]
        public class WMenuSysPatch
        {
            private static IButton modButton;

            [HarmonyPatch(MethodType.Constructor, typeof(WMenu))]
            [HarmonyPostfix]
            static void ctro_Patch(WMenuSys __instance)
            {
                modButton = new IButton(RF.LoadCache("System/Menu/button_system_0.png"), RF.LoadCache("System/Menu/button_system_1.png"), " ");
                AccessTools.Method(typeof(WMenuSys), "drawButtonText").Invoke(__instance, new object[] {modButton.getText(), "WingMod(F4)"});
                modButton.z = 2601;
            }

            [HarmonyPatch(nameof(WMenuSys.update))]
            [HarmonyPostfix]
            static void update_Patch(WMenuSys __instance, ref bool __result)
            {
                if (!__result && modButton.update())
                {
                    var win = new WModSetting();
                    __instance.showMinWindow(win);
                    __result = true;
                }
            }

            [HarmonyPatch(nameof(WMenuSys.dispose))]
            [HarmonyPostfix]
            static void dispose_Patch()
            {
                modButton.disposeMin();
            }

            [HarmonyPatch(nameof(WMenuSys.sizeChange))]
            [HarmonyPostfix]
            static void sizeChange_Patch(IButton ___setButton)
            {
                modButton.zoomX = modButton.zoomY = ___setButton.zoomX;
                modButton.x = ___setButton.x + ___setButton.width + 10;
                modButton.y = ___setButton.y;
            }
        }


        [HarmonyPatch(typeof(WMenuSocial))]
        public class WMenuSocialPatch
        {
            private static List<IButton> teleportBtns = new List<IButton>();
            private static IButton teleportBtn;

            /// <summary>
            /// 添加传送按钮
            /// </summary>
            [HarmonyPatch(MethodType.Constructor)]
            [HarmonyPostfix]
            static void ctor_patch(WMenuSocial __instance, IViewport ___viewR)
            {
                teleportBtn = new WModSetting.TextSmallBtn(null, "传送");
                teleportBtn.z = 2503;
            }

            [HarmonyPatch(nameof(WMenuSocial.update))]
            [HarmonyPostfix]
            static void update_patch(ref bool __result, NPCBase ___nowNpc)
            {
                if (!__result)
                {
                    if (teleportBtn.update())
                    {
                        int x = (int) ___nowNpc.nowX;
                        int y = (int) ___nowNpc.nowY;
                        FileLog.Log($"传送到{___nowNpc.name} nowMapId={___nowNpc.nowMapId} x={x} y={y}");
                        if (___nowNpc.nowMapId == 1)
                        {
                            var toMap = RF.LoadMap(___nowNpc.nowMapId);
                            FileLog.Log($"无法传送 {toMap.name}");
                            RF.showTips("无法传送");
                        }
                        else
                        {
                            // x = y = 0;
                            LScript.toNMap(___nowNpc.nowMapId.ToString(), (x + 20).ToString(), y.ToString(), "0");
                        }
                    }
                    else return;

                    __result = true;
                }
            }

            [HarmonyPatch(nameof(WMenuSocial.dispose))]
            [HarmonyPostfix]
            static void dispose_patch()
            {
                teleportBtn.disposeMin();
            }

            [HarmonyPatch(nameof(WMenuSocial.sizeChange))]
            [HarmonyPostfix]
            static void sizeChange_patch(ISprite ___drawRight)
            {
                teleportBtn.x = ___drawRight.x;
                teleportBtn.y = ___drawRight.y + 10;
                teleportBtn.zoomX = teleportBtn.zoomY = ___drawRight.zoomX;
            }
        }

        [HarmonyPatch(typeof(WOreListNew))]
        public class WOreListPatch
        {
            /// <summary>
            /// 任务无上限
            /// </summary>
            [HarmonyPatch("makeButton")]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> makeButtonPatch(IEnumerable<CodeInstruction> instructions)
            {
                // return instructions;
                ILCursor cursor = new ILCursor(instructions);
                // org: b2.enable = GLNode.isCheck(22);
                if (cursor.TryGotoNext(it => it.Instruction.MatchCallByName("iFActionScript.GLNode::isCheck")))
                {
                    cursor.Index -= 2;
                    cursor.RemoveRange(4);
                }

                return cursor.Context.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(WScienceMin))]
        public class WScienceMinPatch
        {
            /// <summary>
            /// 科技低要求
            /// </summary>
            [HarmonyPatch(nameof(WScienceMin.canStudy))]
            [HarmonyPostfix]
            static void canStudyPatch(ref int __result)
            {
                // if (Settings.ScienceStudyMin) __result = 0;
            }

            [HarmonyPatch(nameof(WScienceMin.studyDo))]
            [HarmonyPrefix]
            static void studyDoPatch(GDScience ___data)
            {
                if (Settings.ScienceStudyMin) //补齐扣除的晶能
                {
                    for (int i = 0; i < ___data.price.Length; i++)
                    {
                        RV.GameData.mana[i] += ___data.price[i];
                    }
                }
            }
        }
    }

    /// <summary>
    /// mod设置
    /// </summary>
    class WModSetting : WBase
    {
        private IViewport view;
        private FloatSlider moveSpeedBar;
        private FloatSlider makeSpeedBar;
        private FloatSlider machineSpeedBar;
        private WingCheckBox giveItemAnyMaxBox;
        private WingCheckBox canPenetrateBox;
        private WingCheckBox makeItemOnceBox;
        private WingCheckBox taskLimitBox;
        private WingCheckBox gameTouhuBox;
        private WingCheckBox buildNoCostBox;
        private WingCheckBox quickCollectBox;
        private WingCheckBox quickToolBox;
        private WingCheckBox ScienceStudyMinBox;
        private WingCheckBox SkillNoCdBox;
        private WingCheckBox EnemyDrop100Box;
        private WingCheckBox ItemNoSubBox;
        private IButton backHomeBtn;
        private IButton hourDecBtn;
        private IButton hourIncBtn;
        private ISprite textSprit;
        private ISprite back;
        CloseBtn closeBtn = new CloseBtn();
        private List<WingCheckBox> checkBoxes = new List<WingCheckBox>();
        private List<IButton> buttons = new List<IButton>();
        private List<FloatSlider> sliders = new List<FloatSlider>();
        private List<ISprite> sprites = new List<ISprite>();

        private int _z = 3000;
        private int _textX = 15;
        private int _textLineHeight = 30;
        private bool preTimeLock = RV.NowTime.timeLock;

        public WModSetting()
        {
            RV.NowTime.timeLock = true;
            closeBtn.z = _z;
            closeBtn.x = 500;
            view = new IViewport(26, 86, 560, 583);
            view.z = _z;
            moveSpeedBar = new FloatSlider(view, (v) =>
            {
                if (v.HasValue) WingSourceHarmPatcher.Settings.MoveSpeed = v.Value;
                return WingSourceHarmPatcher.Settings.MoveSpeed;
            }, 10);
            makeSpeedBar = new FloatSlider(view, (v) =>
            {
                if (v.HasValue) WingSourceHarmPatcher.Settings.MakeItemSpeed = v.Value;
                return WingSourceHarmPatcher.Settings.MakeItemSpeed;
            }, 29);
            machineSpeedBar = new FloatSlider(view, (v) =>
            {
                if (v.HasValue) WingSourceHarmPatcher.Settings.MachineSpeed = v.Value;
                return WingSourceHarmPatcher.Settings.MachineSpeed;
            }, 100);


            giveItemAnyMaxBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.GiveItemAnyMax);
            canPenetrateBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.CanPenetrate);
            makeItemOnceBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.MakeItemOneTime);
            taskLimitBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.TaskNoLimit);
            buildNoCostBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.BuildNoCost);
            gameTouhuBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.GameTouhuEasy);
            quickCollectBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.QuickCollect);
            quickToolBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.QuickTool);
            ScienceStudyMinBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.ScienceStudyMin);
            SkillNoCdBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.SkillNoCd);
            EnemyDrop100Box = new WingCheckBox(view, WingSourceHarmPatcher.Settings.EnemyDrop100);
            ItemNoSubBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.ItemNoSub);

            hourDecBtn = new IButton(RF.LoadCache("System/Setting/minus_0.png"), RF.LoadCache("System/Setting/minus_1.png"), "", view);
            hourIncBtn = new IButton(RF.LoadCache("System/Setting/add_0.png"), RF.LoadCache("System/Setting/add_1.png"), "", view);
            backHomeBtn = new TextSmallBtn(view, "回家");
            buttons.Add(backHomeBtn);
            buttons.Add(hourDecBtn);
            buttons.Add(hourIncBtn);
            buttons.ForEach(x => x.z = _z);
            textSprit = new ISprite(560, 583, IColor.Transparent(), view);
            back = new ISprite(RF.LoadCache("System/Setting/settingBack.png"));
            back.z = _z - 1;
            drawUIText();
        }

        void drawUIText()
        {
            textSprit.clearBitmap();
            var line = 0;
            drawText("功能：", line * _textLineHeight);
            backHomeBtn.x = 100;
            backHomeBtn.y = line * _textLineHeight;
            line++;
            drawText("时间调整(1小时)：", line * _textLineHeight);
            hourDecBtn.x = 160;
            hourIncBtn.x = hourDecBtn.x + 40;
            hourDecBtn.y = hourIncBtn.y = line * _textLineHeight;
            line++;
            // moveSpeedBar.x = 95 + 10;
            // moveSpeedBar.y = line * _textLineHeight + 5;
            // drawText(WingSourceHarmPatcher.Settings.MoveSpeed.ToString("0.0"), line * _textLineHeight, 330);
            // drawText("移动速度：", line++ * _textLineHeight);
            drawSlider("移动速度：", moveSpeedBar, line++);
            drawCheckBox("穿墙：", canPenetrateBox, line++);
            drawCheckBox("技能无CD：", SkillNoCdBox, line++);
            drawCheckBox("物品不减（数量大于1）：", ItemNoSubBox, line++);
            drawCheckBox("怪物必掉落(需重启)：", EnemyDrop100Box, line++);
            drawCheckBox("赠送任何东西都是最喜欢(需重启)：", giveItemAnyMaxBox, line++);
            drawCheckBox("制作敲击1次：", makeItemOnceBox, line++);
            drawSlider("制作速度：", makeSpeedBar, line++);
            drawSlider("机器生产速度：", machineSpeedBar, line++);
            drawCheckBox("任务无上限(需重启)：", taskLimitBox, line++);
            drawCheckBox("建造不消耗：", buildNoCostBox, line++);
            drawCheckBox("科技不消耗：", ScienceStudyMinBox, line++);
            drawCheckBox("快速搜集：", quickCollectBox, line++);
            drawCheckBox("快速砍树/破石：", quickToolBox, line++);
            drawCheckBox("小游戏投壶/龙舟/中秋最高分：", gameTouhuBox, line++);
        }

        void drawCheckBox(string text, WingCheckBox box, int line)
        {
            box.Pos((int) IFont.getWidth(text, 16) + 40, line * _textLineHeight, _z);
            drawText(text, line * _textLineHeight);
            checkBoxes.Add(box);
        }

        void drawSlider(string text, FloatSlider slider, int line)
        {
            slider.x = (int) IFont.getWidth(text, 16) + 40;
            slider.y = line * _textLineHeight;
            drawText(text, line * _textLineHeight);
            var lab = new WingText(slider.nowValue.ToString("0.0"), view);
            lab.x = slider.x + slider.width + 20;
            lab.y = slider.y;
            slider.onChangeValue += (v) => lab.text = v.ToString("0.0");
            sliders.Add(slider);
            sprites.Add(lab);
        }

        void drawText(string t, int y, int? x = null)
        {
            textSprit.drawTextQ(t, x.GetValueOrDefault(_textX), y, RV.colT1, 16);
        }

        public override bool update()
        {
            if (base.update()) return true;
            if (IInput.isKeyDown(IInput.CodeToWinKey(Keys.Escape)) || IInput.isKeyDown(WingSourceHarmPatcher.Settings.HotKey) || closeBtn.update())
            {
                dispose();
                return true;
            }

            if (sliders.FirstOrDefault(s => s.updateCtrl()) != null) return true;
            if (checkBoxes.FirstOrDefault(box =>
                {
                    if (box.update())
                    {
                        if (box == giveItemAnyMaxBox) WingSourceHarmPatcher.Settings.GiveItemAnyMax = box.select;
                        else if (box == canPenetrateBox) WingSourceHarmPatcher.Settings.CanPenetrate = box.select;
                        else if (box == makeItemOnceBox) WingSourceHarmPatcher.Settings.MakeItemOneTime = box.select;
                        else if (box == taskLimitBox) WingSourceHarmPatcher.Settings.TaskNoLimit = box.select;
                        else if (box == buildNoCostBox) WingSourceHarmPatcher.Settings.BuildNoCost = box.select;
                        else if (box == gameTouhuBox) WingSourceHarmPatcher.Settings.GameTouhuEasy = box.select;
                        else if (box == quickCollectBox) WingSourceHarmPatcher.Settings.QuickCollect = box.select;
                        else if (box == quickToolBox) WingSourceHarmPatcher.Settings.QuickTool = box.select;
                        else if (box == ScienceStudyMinBox) WingSourceHarmPatcher.Settings.ScienceStudyMin = box.select;
                        else if (box == SkillNoCdBox) WingSourceHarmPatcher.Settings.SkillNoCd = box.select;
                        else if (box == EnemyDrop100Box) WingSourceHarmPatcher.Settings.EnemyDrop100 = box.select;
                        else if (box == ItemNoSubBox) WingSourceHarmPatcher.Settings.ItemNoSub = box.select;
                        else return false;
                        return true;
                    }

                    return false;
                }) != null) return true;
            if (buttons.FirstOrDefault(btn =>
                {
                    if (btn.update())
                    {
                        if (btn == hourIncBtn) RV.NowTime.addHour();
                        else if (btn == hourDecBtn) RV.NowTime.hour--;
                        else if (btn == backHomeBtn) RF.goHome();
                        else return false;

                        return true;
                    }

                    return false;
                }) != null) return true;
            return false;
        }

        public override void dispose()
        {
            base.dispose();
            view.dispose();
            back.disposeMin();
            textSprit.dispose();
            closeBtn.disposeMin();
            moveSpeedBar.disposeMin();
            checkBoxes.ForEach(x => x.disposeMin());
            buttons.ForEach(x => x.disposeMin());
            sliders.ForEach(x => x.disposeMin());
            sprites.ForEach(x => x.disposeMin());
            RV.NowTime.timeLock = preTimeLock; //时间状态回复
            WingSourceHarmPatcher.SaveSetting();
        }

        /// <summary>
        /// 滑块
        /// </summary>
        class FloatSlider : CBar
        {
            public FloatSlider(IViewport view, Func<float?, float> change, double vMax) : base(RF.LoadCache("System/Setting/volBar_0.png"),
                RF.LoadCache("System/Setting/volBar_1.png"), 0, view)
            {
                setValue(change(null), vMax); //注意顺序
                setMove(RF.LoadCache("System/Setting/volBtn_0.png"), RF.LoadCache("System/Setting/volBtn_1.png"));
                this.onChangeValue += (cv) => { change((float) cv); };
            }
        }

        class CloseBtn : IButton
        {
            public CloseBtn() : base(RF.LoadCache("System/buttonClose_0.png"), RF.LoadCache("System/buttonClose_1.png"))
            {
            }
        }

        public class TextSmallBtn : IButton
        {
            public TextSmallBtn(IViewport viewport, string text) : base(RF.LoadBitmap("System/Bag/buttonMin1_0.png"),
                RF.LoadBitmap("System/Bag/buttonMin1_1.png"), " ", viewport)
            {
                var w = RF.GetFW(text, 12);
                drawTitleQ(text, RV.colT1, 12);
            }
        }

        class WingText : ISprite
        {
            private string _text;

            public string text
            {
                get => _text;
                set
                {
                    _text = value;
                    clearBitmap();
                    drawTextQ(_text, 0, 0, RV.colT1, 16);
                }
            }

            public WingText(string text, IViewport viewport = null) : base(300, 50, IColor.Transparent(), viewport)
            {
                this.text = text;
            }
        }

        class WingCheckBox : CCheck
        {
            private ISprite textSp;

            public WingCheckBox(IViewport view, bool isTrue) : base(RF.LoadCache("System/check_0.png"), RF.LoadCache("System/check_1.png"),
                RF.LoadCache("System/check_2.png"), 0, 0, view)
            {
                this.select = isTrue;
                var txt = "开启";
                textSp = new ISprite((int) IFont.getWidth(txt, 16) + 3, 20, IColor.Transparent(), view);
                textSp.drawTextQ(txt, 0, 0, RV.colT1, 16);
            }

            public void Pos(int xv, int yv, int? zv = null)
            {
                textSp.y = yv;
                textSp.x = xv + 20;
                this.x = xv;
                this.y = yv;
                if (zv.HasValue) this.z = zv.Value;
                textSp.z = this.z;
            }

            public void disposeMin()
            {
                base.dispose();
                textSp.dispose();
            }
        }
    }
}