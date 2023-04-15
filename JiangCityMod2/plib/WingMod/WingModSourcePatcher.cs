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
    public class WingModSettingJSon
    {
        public int HotKey = IInput.CodeToWinKey(Keys.F4);
        public float MoveSpeed = 1; //移动速度
        public bool CanPenetrate = true; //穿墙
        public bool GiveItemAnyMax = true; //赠送任何东西都是最喜欢
        public bool MakeItemOneTime = true; //一次敲击
        public bool TaskNoLimit = true; //任务无上限
        public bool GameTouhuEasy = true; //小游戏投壶最高分
        public bool BuildNoCost = false; //建造不消耗
        public Dictionary<string, string> Extensions = new Dictionary<string, string>(); //其他扩展
    }

    public static class WingSourceHarmPatcher
    {
        public static WingModSettingJSon Settings = new WingModSettingJSon();

        public static void Patch()
        {
            var p = new Harmony("WingMod.SourcePatcher");
            p.PatchAll();
            RV.ver += " (WingMod v0.0.2)";
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

        [HarmonyPatch]
        public class SourcePatches
        {
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
                    ___makeTimeMax = 20;
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
                    cursor.Emit(old.opcode, old.operand);
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
                        LScript.toNMap(___nowNpc.nowMapId.ToString(), x.ToString(), y.ToString(), "0");
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
    }

    /// <summary>
    /// mod设置
    /// </summary>
    class WModSetting : WBase
    {
        private IViewport view;
        private FloatSlider moveSpeedBar;
        private WingCheckBox giveItemAnyMaxBox;
        private WingCheckBox canPenetrateBox;
        private WingCheckBox makeItemOnceBox;
        private WingCheckBox taskLimitBox;
        private WingCheckBox gameTouhuBox;
        private WingCheckBox buildNoCostBox;
        private IButton hourDecBtn;
        private IButton hourIncBtn;
        private ISprite textSprit;
        private ISprite back;
        CloseBtn closeBtn = new CloseBtn();
        private List<WingCheckBox> checkBoxes = new List<WingCheckBox>();
        private List<IButton> buttons = new List<IButton>();

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
            moveSpeedBar = new FloatSlider(view, WingSourceHarmPatcher.Settings.MoveSpeed, 10);
            moveSpeedBar.onChangeValue += value =>
            {
                WingSourceHarmPatcher.Settings.MoveSpeed = (float) value;
                drawUIText();
            };
            giveItemAnyMaxBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.GiveItemAnyMax);
            canPenetrateBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.CanPenetrate);
            makeItemOnceBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.MakeItemOneTime);
            taskLimitBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.TaskNoLimit);
            buildNoCostBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.BuildNoCost);
            gameTouhuBox = new WingCheckBox(view, WingSourceHarmPatcher.Settings.GameTouhuEasy);

            hourDecBtn = new IButton(RF.LoadCache("System/Setting/minus_0.png"), RF.LoadCache("System/Setting/minus_1.png"), "", view);
            hourIncBtn = new IButton(RF.LoadCache("System/Setting/add_0.png"), RF.LoadCache("System/Setting/add_1.png"), "", view);
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
            drawText("时间调整(1小时)：", line * _textLineHeight);
            hourDecBtn.x = 160;
            hourIncBtn.x = hourDecBtn.x + 40;
            hourDecBtn.y = hourIncBtn.y = line * _textLineHeight;
            line++;
            moveSpeedBar.x = 95 + 10;
            moveSpeedBar.y = line * _textLineHeight + 5;
            drawText(WingSourceHarmPatcher.Settings.MoveSpeed.ToString("0.0"), line * _textLineHeight, 330);
            drawText("移动速度：", line++ * _textLineHeight);
            drawCheckBox("穿墙：", canPenetrateBox, line++);
            drawCheckBox("赠送任何东西都是最喜欢(需重启)：", giveItemAnyMaxBox, line++);
            drawCheckBox("制作敲击1次：", makeItemOnceBox, line++);
            drawCheckBox("任务无上限(需重启)：", taskLimitBox, line++);
            drawCheckBox("建造不消耗：", buildNoCostBox, line++);
            drawCheckBox("小游戏投壶最高分：", gameTouhuBox, line++);
        }

        void drawCheckBox(string text, WingCheckBox box, int line)
        {
            box.Pos((int) IFont.getWidth(text, 16) + 40, line * _textLineHeight, _z);
            drawText(text, line * _textLineHeight);
            checkBoxes.Add(box);
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

            if (moveSpeedBar.updateCtrl()) return true;
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
            RV.NowTime.timeLock = preTimeLock; //时间状态回复
            WingSourceHarmPatcher.SaveSetting();
        }

        /// <summary>
        /// 滑块
        /// </summary>
        class FloatSlider : CBar
        {
            public FloatSlider(IViewport view, double v, double vMax) : base(RF.LoadCache("System/Setting/volBar_0.png"),
                RF.LoadCache("System/Setting/volBar_1.png"), 0, view)
            {
                setValue(v, vMax); //注意顺序
                setMove(RF.LoadCache("System/Setting/volBtn_0.png"), RF.LoadCache("System/Setting/volBtn_1.png"));
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