using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using iFActionGame2;
using iFActionGame2.js;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using OpenTK.Windowing.GraphicsLibraryFramework;
using WingUtil.Harmony;

namespace iFActionScript
{
    public class WingModSettingJSon
    {
        public float MoveSpeed = 1; //移动速度
        public bool GiveItemAnyMax = true; //赠送任何东西都是最喜欢
    }

    public static class WingSourceHarmPatcher
    {
        public static WingModSettingJSon Settings = new WingModSettingJSon();

        public static void Patch()
        {
            var p = new Harmony("WingMod.SourcePatcher");
            p.PatchAll();
            RV.ver += " (WingMod v0.0.1)";
            LoadSetting();
        }

        private static String SaveFile = "WingMod.Settings.json";

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
            /// <param name="___x"></param>
            /// <param name="___move"></param>
            [HarmonyPatch(typeof(CBar), MethodType.Setter)]
            [HarmonyPatch("x")]
            [HarmonyPostfix]
            static void CBar_x(CBar __instance)
            {
                if (__instance.move != null) __instance.move.x = __instance.back.x + __instance.bCof.width - __instance.move.width / 2;
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
            static void bSpeedPatch(ref double __result)
            {
                __result *= Settings.MoveSpeed;
            }
        }

        [HarmonyPatch(typeof(NPCBase))]
        public class NPCBasePatch
        {
            /// <summary>
            /// 送礼
            /// </summary>
            [HarmonyPatch(nameof(NPCBase.giftDo))]
            // [HarmonyPostfix]
            // static void t()
            // {
            //     
            // }
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> giftDoPatch(IEnumerable<CodeInstruction> instructions)
            {
                ILCursor cursor = new ILCursor(instructions);
                // 基础好感30
                if (cursor.TryGotoNext(it => it.Instruction.MatchLdsfld("iFActionScript.RV::NowTime")))
                {
                    cursor.Emit(OpCodes.Ldc_I4_S, 30);
                    cursor.Emit(OpCodes.Stloc_0);
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
                if (IInput.isKeyDown(IInput.CodeToWinKey(Keys.F2)))
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
                AccessTools.Method(typeof(WMenuSys), "drawButtonText").Invoke(__instance, new object[] {modButton.getText(), "WingMod"});
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
    }

    /// <summary>
    /// mod设置
    /// </summary>
    class WModSetting : WBase
    {
        private IViewport view;
        private FloatSlider moveSpeedBar;
        private WingCheckBox giveItemAnyMaxBox;
        private ISprite textSprit;
        private ISprite back;
        CloseBtn closeBtn = new CloseBtn();

        private int _z = 3000;
        private int _textX = 15;
        private int _textLineHeight = 20;
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
            textSprit = new ISprite(560, 583, IColor.Transparent(), view);
            back = new ISprite(RF.LoadBitmap("System/Setting/settingBack.png"));
            back.z = _z - 1;
            drawUIText();
        }

        void drawUIText()
        {
            textSprit.clearBitmap();
            var line = 0;
            moveSpeedBar.x = 95 + 10;
            moveSpeedBar.y = line * _textLineHeight + 5;
            drawText(WingSourceHarmPatcher.Settings.MoveSpeed.ToString("0.0"), line * _textLineHeight, 330);
            drawText("移动速度：", line++ * _textLineHeight);
            var giveItemTxt = "赠送任何东西都是最喜欢：";
            giveItemAnyMaxBox.Pos((int) IFont.getWidth(giveItemTxt, 16) + 40, line * _textLineHeight, _z);
            drawText(giveItemTxt, line++ * _textLineHeight);
        }

        void drawText(string t, int y, int? x = null)
        {
            textSprit.drawTextQ(t, x.GetValueOrDefault(_textX), y, RV.colT1, 16);
        }

        public override bool update()
        {
            if (base.update()) return true;
            if (IInput.isKeyDown(IInput.CodeToWinKey(Keys.Escape)) || IInput.isKeyDown(IInput.CodeToWinKey(Keys.F2)) || closeBtn.update())
            {
                dispose();
                return true;
            }

            if (moveSpeedBar.updateCtrl()) return true;
            if (giveItemAnyMaxBox.update())
            {
                WingSourceHarmPatcher.Settings.GiveItemAnyMax = giveItemAnyMaxBox.select;
                return true;
            }

            return false;
        }

        public override void dispose()
        {
            base.dispose();
            back.disposeMin();
            textSprit.dispose();
            closeBtn.disposeMin();
            moveSpeedBar.disposeMin();
            giveItemAnyMaxBox.disposeMin();
            RV.NowTime.timeLock = preTimeLock; //时间状态回复
            WingSourceHarmPatcher.SaveSetting();
        }

        /// <summary>
        /// 滑块
        /// </summary>
        class FloatSlider : CBar
        {
            public FloatSlider(IViewport view, double v, double vMax) : base(RF.LoadCache("System/Setting/volBar_0.png"), RF.LoadCache("System/Setting/volBar_1.png"), 0, view)
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