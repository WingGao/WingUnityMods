using System;
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

        [HarmonyPatch(typeof(DauSys))]
        static class DauSysPatch
        {
            /// <summary>
            /// 心情不减
            /// </summary>
            /// <param name="num"></param>
            [HarmonyPatch(nameof(DauSys.AddMood))]
            [HarmonyPrefix]
            static void AddMoodPrefix(ref int num)
            {
                if (settings.MoodUnlimited && num < 0) num = 0;
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

        #endregion
    }
}