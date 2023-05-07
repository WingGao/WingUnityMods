using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using WingUtil.Harmony;
using WingUtil.UnityModManager;
using Object = UnityEngine.Object;

namespace WingMod
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        // [Header("修改")] 
        [Draw("行动不减")] public bool ActionUnlimited = false;

        [Draw("心情不减")] public bool MoodUnlimited = false;

        [Draw("约会氛围倍数", Precision = 1, Max = 10)]
        public float NpcMoodMul = 2;

        [Draw("感悟倍数", Precision = 1, Min = 1, Max = 50, Type = DrawType.Slider)]
        public float DauInspireMul = 10;

        [Draw("声望倍数", Precision = 1, Min = 1, Max = 50, Type = DrawType.Slider)]
        public float DauFrameMul = 10;

        [Draw("冒险一击必杀")] public bool FightOneHit = true;
        //
        // [Draw("见闻掉落无限制")] public bool DropStoryUnlimited = true;


        public void OnInit()
        {
        }


        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void OnChange()
        {
        }
    }

    static class Main
    {
        #region 通用配置

        public static UnityModManager.ModEntry mod;

        // 配置
        public static Settings settings;

        public static bool Enable => mod.Enabled;

        /// <summary>
        /// UMM只能加载WingMod.dll，所以需要手动加载其他dll
        /// </summary>
        public static void LoadWingDlls()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name.StartsWith("WingUtil."))
                {
                    var fp = Path.Combine(Directory.GetCurrentDirectory(), "Mods", "WingMod", args.Name.Split(',')[0] + ".dll");
                    // LogF($"AssemblyResolve {args.Name} {fp}");
                    return Assembly.LoadFile(fp);
                }
                else
                {
                    return null;
                }
            };
        }

        /// <summary>
        /// 加载
        /// https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
        /// </summary>
        /// <param name="modEntry"></param>
        static void Load(UnityModManager.ModEntry modEntry)
        {
            LoadWingDlls();
            settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            settings.OnInit();

            Harmony.DEBUG = true;
            FileLog.Reset();

            mod = modEntry;
            var harmony = new Harmony(mod.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            LogF("WingMod load");
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Draw(modEntry);
            if (DauSys.Instan == null)
            {
                GUILayout.Label("未加载存档");
                return;
            }

            var btnTexts = new[] {"-1", "-5", "-50", "+1", "+5", "+50"};


            GUILayout.Label("属性");
            //核心属性
            foreach (int ts in Enum.GetValues(typeof(InAttri)))
            {
                WingUnityDraw.DrawButtonGroup(Constant.inAttriCh[ts], btnTexts, i =>
                {
                    var t = btnTexts[i];
                    var changeVal = int.Parse(t);
                    LogF($"{ts} {changeVal}");
                    DauSys.AddInAttri(ts, changeVal, true);
                });
            }

            //基础属性
            foreach (int ts in Enum.GetValues(typeof(Nature)))
            {
                WingUnityDraw.DrawButtonGroup(DataSys.Instan.dataCfg.natureCh[ts], btnTexts, i =>
                {
                    var t = btnTexts[i];
                    var changeVal = int.Parse(t);
                    LogF($"{ts} {changeVal}");
                    DauSys.AddNature(ts, changeVal, true);
                });
            }
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* active or inactive */)
        {
            if (value)
            {
                Run(); // Perform all necessary steps to start mod.
            }
            else
            {
                Stop(); // Perform all necessary steps to stop mod.
            }

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

        static void Stop()
        {
            LogF("WingMod Stop");
        }

        static void LogF(string str)
        {
            UnityModManager.Logger.Log(str, "[WingMod] ");
        }

        // [HarmonyPatch(typeof(Debug))]
        public static class UnityDebugPatch
        {
            private static ILogger MyLogger => new Logger(new MyDebugLogHandler());

            [HarmonyPostfix]
            [HarmonyPatch("unityLogger", MethodType.Getter)]
            public static void LogGet(ref ILogger __result, ref ILogger ___s_DefaultLogger, ref ILogger ___s_Logger)
            {
                ___s_DefaultLogger = MyLogger;
                ___s_Logger = MyLogger;
                __result = MyLogger;
            }

            [HarmonyPostfix]
            [HarmonyPatch("IsLoggingEnabled")]
            public static void IsLoggingEnabledGet(ref bool __result)
            {
                __result = true;
            }

            class MyDebugLogHandler : ILogHandler
            {
                public void LogFormat(UnityEngine.LogType logType, Object context, string format, params object[] args)
                {
                    // StackTrace st = new StackTrace(3, true);
                    /**
                     *  at UnityEngine.Logger.Log (UnityEngine.LogType logType, System.Object message) [0x00000] in <3d993dea89b649118f5e3c1a995c56fc>:0  skip2
  at UnityEngine.Debug.Log (System.Object message) [0x00000] in <3d993dea89b649118f5e3c1a995c56fc>:0 skip3
  at PlayerPrefControl.StartFromBegining () [0x00000] in <a311cad17a3041fba8282ec312c03a2e>:0 
  at StageControl.StageControl.Start_Patch1 (StageControl ) [0x00000] in <a311cad17a3041fba8282ec312c03a2e>:0  InitObjs!8.762533
                     */
                    StackFrame callStack = new StackFrame(3, true);
                    // WingLog.GetStackFrame(4)
                    UnityModManager.Logger.Log(String.Format(format, args),
                        $"[{logType}] [] ");
                }

                public void LogException(Exception exception, Object context)
                {
                    UnityModManager.Logger.LogException(" ", exception, "[Error]");
                }
            }
        }

        #endregion

        #region 该游戏的修改

        private static string[] FaWishScoreCn = new[] {"爱我", "可爱", "睿智", "勇敢", "可怕", "笨", "逊"};

        [HarmonyPatch(typeof(DauSys))]
        static class DauSysPatch
        {
            /// <summary>
            /// 心情不减
            /// </summary>
            [HarmonyPatch(nameof(DauSys.AddMood))]
            [HarmonyPrefix]
            static void AddMoodPrefix(ref int num)
            {
                if (settings.MoodUnlimited && num < 0) num = 0;
            }

            /// <summary>
            /// 感悟倍率
            /// </summary>
            [HarmonyPatch(nameof(DauSys.AddInspiration))]
            [HarmonyPrefix]
            static void AddInspirationPrefix(ref int num)
            {
                if (num > 0) num = (int) (num * settings.DauInspireMul);
            }

            /// <summary>
            /// 声望倍率
            /// </summary>
            [HarmonyPatch(nameof(DauSys.AddFame))]
            [HarmonyPrefix]
            static void AddFamePrefix(ref int num)
            {
                if (num > 0) num = (int) (num * settings.DauFrameMul);
            }

            /// <summary>
            /// 显示女儿的心事
            /// </summary>
            [HarmonyPatch(nameof(DauSys.ShowDauWorryChoice))]
            [HarmonyPrefix]
            static void ShowDauWorryChoicePrefix(ref string[] choiceCh, Worry ___tempWorry)
            {
                for (var i = 0; i < ___tempWorry.choiceAddFaReview.Count; i++)
                {
                    for (var j = 0; j < ___tempWorry.choiceAddFaReview[i].Length; j++)
                    {
                        var score = ___tempWorry.choiceAddFaReview[i][j];
                        if (score > 0) choiceCh[i] += $" ({FaWishScoreCn[j]}+{score})";
                    }
                }
            }

            /// <summary>
            /// 剧院打工-台词随便选
            /// </summary>
            [HarmonyPatch(nameof(DauSys.ClickLineChoice))]
            [HarmonyPrefix]
            static void ClickLineChoicePrefix(int index, ref List<ValueTuple<int, string>>[] ___linesChoice, int ___lineIndex, int ___lineType)
            {
                var lc = ___linesChoice[___lineIndex][index];
                lc.Item1 = ___lineType;
            }
        }

        [HarmonyPatch(typeof(DataSys))]
        static class DataSysPatch
        {
            /// <summary>
            /// 行动不减
            /// </summary>
            /// <param name="num"></param>
            [HarmonyPatch(nameof(DataSys.AddEnergy))]
            [HarmonyPrefix]
            static void AddEnergyPrefix(ref int num)
            {
                if (settings.ActionUnlimited && num < 0) num = 0;
            }
        }

        [HarmonyPatch(typeof(ChatSys))]
        static class ChatSysPatch
        {
            [HarmonyPatch(nameof(ChatSys.ChatMenu), new Type[] {typeof(string[]), typeof(string[]), typeof(ChatSys.MenuState), typeof(bool[])})]
            [HarmonyPrefix]
            static void ChatMenuPrefix(ref string[] selectName, ChatSys.MenuState state)
            {
                switch (state)
                {
                    case ChatSys.MenuState.dauWishPer: //父亲以身作则
                        var tempWishPerChat = WingAccessTools.GetFieldValue<Chat>(TaskSys.Instan, "tempWishPerChat");
                        for (var i = 0; i < selectName.Length; i++)
                        {
                            LogF($"{i} tempWishPerChat.icon={tempWishPerChat.icon}");
                            var ints = Func.IntLists(tempWishPerChat.icon)[i];
                            LogF($"ints={String.Join(",", ints)}");
                            for (var j = 0; j < Constant.dauWishNatureCh.Length; j++)
                            {
                                if (ints[j] > 0) selectName[i] += $" {Constant.dauWishNatureCh[j]}+{ints[j]} ";
                                if (ints[j] < 0) selectName[i] += $" {Constant.dauWishNatureCh[j]}-{ints[j]} ";
                            }
                        }

                        break;
                }
            }
        }


        [HarmonyPatch(typeof(FeastSys))]
        static class FestSysPatch
        {
            /// <summary>
            /// 数学题全对
            /// </summary>
            [HarmonyPatch("CheckQuesAnswer")]
            [HarmonyPrefix]
            static void CheckQuesAnswerPrefix(ref int ___quesIndex, int ___tempQueAns)
            {
                ___quesIndex = ___tempQueAns;
            }
        }

        /// <summary>
        /// 节日-转盘
        /// </summary>
        [HarmonyPatch]
        static class FeastSys_BirdFeastEnu
        {
            public static MethodBase TargetMethod()
            {
                return AccessTools.FirstMethod(
                    AccessTools.FirstInner(typeof(FeastSys), t => t.Name.StartsWith("<BirdFeastEnu>d"))
                    , t => t.Name.Contains("MoveNext"));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                ILCursor c = new ILCursor(instructions);
                // 转盘定格第二个
                if (c.TryGotoNext(inst => inst.Instruction.MatchStfld("FeastSys::spinGameTime"))
                    && c.TryGotoNext(inst => inst.Instruction.MatchCallByName("UnityEngine.Random::Range"))
                   )
                {
                    c.Index += 1;
                    c.Emit(OpCodes.Pop);
                    c.Emit(OpCodes.Ldc_I4, 250);
                }

                return c.Context.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(NpcSys))]
        static class NpcSysPatch
        {
            /// <summary>
            /// 约会显示选项
            /// </summary>
            [HarmonyPatch("RandomDateSelect")]
            [HarmonyPostfix]
            static void RandomDateSelectPostfix(NpcSys __instance, List<DateSelect> ___dateSelects)
            {
                string[] chName = new string[Constant.dateBtnCount];
                for (int index = 0; index < chName.Length; ++index)
                {
                    var favNum = ___dateSelects[index].addFavor[NpcSys.tempNpc.enName];
                    chName[index] = $"{___dateSelects[index].chName} ({favNum})";
                }

                WingAccessTools.InvokeMethod<object>(__instance, "SetDateBtnUi", new[] {chName, null});
            }

            /// <summary>
            /// 最大化氛围
            /// </summary>
            [HarmonyPatch(nameof(NpcSys.AddMood))]
            [HarmonyPrefix]
            static void AddMoodPostfix(ref int num, int ___mood)
            {
                if (num > 0)
                    num = (int) (num * settings.NpcMoodMul)
                        ;
            }

            /// <summary>
            /// 请教+20好感
            /// </summary>
            [HarmonyPatch(nameof(NpcSys.EndConsult))]
            [HarmonyPrefix]
            static void EndConsultPrefix()
            {
                NpcSys.tempNpc.AddFavor(20);
            }
        }

        [HarmonyPatch(typeof(MapSys))]
        static class MapSysPatch
        {
            /// <summary>
            /// 钓鱼满分
            /// </summary>、
            [HarmonyPatch("FishingResult")]
            [HarmonyPrefix]
            static void FishingResultPrefix(ref int ___hitCount, int ___fishNum, ref int ___fishingScore, int ___rivalFishingScore)
            {
                ___hitCount = ___fishNum;
                ___fishingScore = ___rivalFishingScore;
            }
        }

        [HarmonyPatch]
        static class OtherPatch
        {
            /// <summary>
            /// 骰子游戏必胜
            /// </summary>
            [HarmonyPatch(typeof(DiceSys), nameof(DiceSys.StartDiceBtn))]
            [HarmonyPostfix]
            static void DiceSys_StartDiceBtn_Postfix(IList ___dicePers)
            {
                WingAccessTools.SetFieldValue(___dicePers[1], "hp", 1);
            }

            /// <summary>
            /// 冒险一击必杀
            /// </summary>
            [HarmonyPatch(typeof(FightSys), "GeneEnemy")]
            [HarmonyPostfix]
            static void FightSys_GeneEnemy_Postfix(List<FightEnemy> ___tempEnemies)
            {
                if (settings.FightOneHit) ___tempEnemies.ForEach(e => e.hp = 1);
            }
        }

        #endregion
    }
}