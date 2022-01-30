using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using WingUtil;
using WingUtil.Harmony;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace WingMod
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Header("修改")] [Draw("掉落神兵/圣物/技能最高等级")]
        public bool DropWeaponLevel3 = true;

        [Draw("见闻掉落无限制")] public bool DropStoryUnlimited = true;
        [Draw("必出书怪")] public bool ShuguaiEnable = false;
        [Draw("毒宗碎片增加")] public bool DuPopEnable = true;
        [Draw("剑返自动触发")] public bool FlySwardAutoBack = true;
        [Draw("剑返无冷却")] public bool FlySwardBackNoCd = true;
        [Draw("飞剑按住自动")] public bool FlySwardKeepPress = true;

        // [Draw("飞剑冷却百分比", Max = 100, Min = 0, Precision = 0)]
        // public float FlySwardBackCoolPer = 50;

        [Draw("减伤百分比", Max = 100, Min = 0, Precision = 0)]
        public float EnemyDamagePer = 99;

        [Draw("不会死亡")] public bool DeathDisable = true;


        [Draw("伤害倍率", Min = 1, Precision = 0)] public float DamageMultiply = 1f;

        [Draw("魂不减")] public bool SoulNotDecrease = true;

        [Draw("神兵-下次掉落")] public MagicSwordName MagicSwordNextDrop;
        [Draw("打印Error")] public bool LogError = false;

        public void OnInit()
        {
            MagicSwordNextDrop = MagicSwordName.None;
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
        /// 加载
        /// https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
        /// </summary>
        /// <param name="modEntry"></param>
        static void Load(UnityModManager.ModEntry modEntry)
        {
            settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            settings.OnInit();

            Harmony.DEBUG = true;
            FileLog.Reset();
            WingLog.Reset();

            mod = modEntry;
            var harmony = new Harmony(mod.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnShowGUI = OnShowGUI;

            EnemyControlPatch.Init();

            LogF("WingMod load");
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Draw(modEntry);
            // GUILayout.BeginVertical();
            // var magIdx = 0;
            // var magCol = 5;
            // foreach (var magicSwordName in Enum.GetValues(typeof(MagicSwordName)).Cast<MagicSwordName>())
            // {
            //     if (magIdx == 0) GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            //     GUILayout.Label(magicSwordName.ToString(), GUILayout.ExpandWidth(true));
            //     magIdx++;
            //     if (magIdx <= magCol)
            //     {
            //         GUILayout.EndHorizontal();
            //         magIdx = 0;
            //     }
            // }
            //
            // if (magIdx > 0) GUILayout.EndHorizontal();
            //
            // GUILayout.EndVertical();
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

        [HarmonyPatch(typeof(Debug))]
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
                public void LogFormat(LogType logType, Object context, string format, params object[] args)
                {
                    // StackTrace st = new StackTrace(3, true);
                    /**
                     *  at UnityEngine.Logger.Log (UnityEngine.LogType logType, System.Object message) [0x00000] in <3d993dea89b649118f5e3c1a995c56fc>:0  skip2
  at UnityEngine.Debug.Log (System.Object message) [0x00000] in <3d993dea89b649118f5e3c1a995c56fc>:0 skip3
  at PlayerPrefControl.StartFromBegining () [0x00000] in <a311cad17a3041fba8282ec312c03a2e>:0 
  at StageControl.StageControl.Start_Patch1 (StageControl ) [0x00000] in <a311cad17a3041fba8282ec312c03a2e>:0  InitObjs!8.762533
                     */
                    StackFrame callStack = new StackFrame(3, true);
                    UnityModManager.Logger.Log(String.Format(format, args),
                        $"[{logType}] [{WingLog.GetStackFrame(4)}] ");
                }

                public void LogException(Exception exception, Object context)
                {
                    if (mod.Active && settings.LogError) UnityModManager.Logger.LogException(" ", exception, "[Error]");
                }
            }
        }

        #endregion

        #region 该游戏的修改

        // 书怪地图
        private static List<int> BookOfAbyssMapConfigIds = new List<int>() { 81, 83, 96, 232, 268, 273, 279, 304, 305 };

        public static void OnShowGUI(UnityModManager.ModEntry modEntry)
        {
            LogF("OnShowGUI");
            LogMagicSwardUse();
        }

        // 打印神兵使用情况
        static void LogMagicSwardUse()
        {
            if (GlobalParameter.instance == null) return;
            StringBuilder sb = new StringBuilder();
            sb.Append("神兵使用： ");
            foreach (var magicSwordName in Enum.GetValues(typeof(MagicSwordName)).Cast<MagicSwordName>())
            {
                if (magicSwordName == MagicSwordName.None) continue;
                var mg = new MagicSword();
                mg.magicSwordName = magicSwordName;
                sb.Append($"{TextControl.instance.MagicSwordInfo(mg)[0]}={GlobalParameter.instance.MagicSwordUsed[(int)(magicSwordName - 1)]}; ");
            }

            LogF(sb.ToString());
        }

        //圣物掉落
        [HarmonyPatch(typeof(PotionDropPool))]
        public static class PotionDropPool_Pop
        {
            [HarmonyPrefix]
            [HarmonyPatch("Pop", new[] { typeof(PN), typeof(int), typeof(Vector3) })]
            public static void Prefix1(ref int level)
            {
                if (mod.Active && settings.DropWeaponLevel3) level = 2; //金色
            }

            [HarmonyPrefix]
            [HarmonyPatch("Pop", new[] { typeof(int), typeof(int), typeof(Vector3) })]
            static void Prefix2(ref int level)
            {
                Prefix1(ref level);
            }
        }

        //技能掉落
        [HarmonyPatch(typeof(SkillDropPool), "Pop")]
        public static class SkillDropPool_Pop
        {
            static void Prefix(ref bool isGolden)
            {
                if (mod.Active && settings.DropWeaponLevel3) isGolden = true;
            }
        }

        //神兵掉落
        [HarmonyPatch(typeof(MagicSwordPool))]
        public static class MagicSwordPoolPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("Pop")]
            static void Prefix(ref int level)
            {
                if (mod.Active && settings.DropWeaponLevel3) level = 3; //绝世
            }

            // 修改掉落ID
            [HarmonyPostfix]
            [HarmonyPatch("ID")]
            static void IDPost(ref int __result)
            {
                if (mod.Active && settings.MagicSwordNextDrop != MagicSwordName.None)
                {
                    __result = (int)settings.MagicSwordNextDrop;
                    settings.MagicSwordNextDrop = MagicSwordName.None; //一次性指令
                }
            }
        }

        //神兵词条
        [HarmonyPatch(typeof(MagicSwordControl), "RandomEntrys")]
        public static class MagicSwordControl_RandomEntrys
        {
            static void Prefix(ref int level)
            {
                if (mod.Active && settings.DropWeaponLevel3) level = 3; //绝世
            }
        }

        // 减伤
        [HarmonyPatch(typeof(PlayerAnimControl), "DealDamage")]
        public static class PlayerAnimControl_DealDamage
        {
            static void Prefix(ref float damage)
            {
                if (mod.Active) damage *= (100 - settings.EnemyDamagePer) / 100;
            }
        }

        //死亡控制
        [HarmonyPatch(typeof(Parameter))]
        public static class PlayerParameterPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch("HP", MethodType.Setter)]
            static void HpPost(Parameter __instance,ref float ___hp)
            {
                //血量最小为1
                if (mod.Active && settings.DeathDisable && __instance == PlayerAnimControl.instance.playerParameter) ___hp = Math.Max(1f, ___hp);
            }
        }

        // 角色控制
        [HarmonyPatch(typeof(PlayerAnimControl))]
        public static class PlayerAnimControlPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("Souls", MethodType.Setter)]
            static bool SoulSetterPrefix(int __0, int ___souls)
            {
                //魂不减
                if (mod.Active && settings.SoulNotDecrease)
                {
                    // LogF($"SoulSetterPrefix {___souls} ==> {__0}");
                    if (__0 > 0 && __0 < ___souls) return false;
                }

                return true;
            }

            [HarmonyTranspiler]
            [HarmonyPatch("Update")]
            static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions,
                ILGenerator gen)
            {
                ILCursor c = new ILCursor(instructions);
                /**
                 * 2022年1月26日19:39:26
                 * else if (this.CheckPlayerInputDown(SpecialAction.FlyingSword) && this.camControl.CanManipulate && !this.camControl.CanOnlyMove && !this.BERSERK_SKILL_ShadowStrike && !this.FROZENMASTER_SKILL_HoarFrost)
		{
			if (this.THUNDERGOD_SKILL_ThunderDash)
			{
				this.shootCoolDownTimer = 0f;
				this.startDash = true;
				return;
			}
			if (this.shootCoolDownTimer >= this.shootCoolDownTime)
			{
				this.shootCoolDownTimer = 0f;
				this.NormalShoot(this.mousePoint, false, 0f, false);
				return;
			}
			this.shootCoolDownTimer += Time.deltaTime;
		}
                 */
                if (c.TryGotoNext(
                        inst => inst.Instruction.MatchCallByName("PlayerAnimControl::CheckPlayerInputDown") &&
                                inst.Previous.Instruction.opcode == OpCodes.Ldc_I4_1,
                        inst => true, //any
                        inst => true, //any
                        inst => inst.Instruction.MatchLdfld("PlayerAnimControl::camControl")
                    ))
                {
                    LogF($"UpdateTranspiler Line_{c.Index}");
                    c.Index += 1;
                    // c.Emit(OpCodes.Callvirt, AccessTools.Method(typeof(PlayerAnimControlPatch), "FlySwardKeepPatch"));
                    c.Emit(OpCodes.Call, AccessTools.Method(typeof(PlayerAnimControlPatch), "FlySwardKeepPatch"));
                    // c.Emit(OpCodes.Nop);
                }

                // c.LogTo(LogF, "PlayerAnimControl.Update_Patched");

                return c.Context.AsEnumerable();
            }

            //飞剑按住自动发射
            public static bool FlySwardKeepPatch(bool a)
            {
                if (mod.Active && settings.FlySwardKeepPress)
                    return (bool)AccessTools.Method(typeof(PlayerAnimControl), "CheckPlayerInputKeep").Invoke(
                        PlayerAnimControl.instance,
                        new object[] { SpecialAction.FlyingSword });
                return a;
            }
        }

        // 飞剑冷却完成
        static bool IsFlySwardBackCdOk()
        {
            return PlayerAnimControl.instance.DrawCoolDownTimer >= PlayerAnimControl.instance.drawCoolDown *
                (1.0 - (double)PlayerAnimControl.instance.playerParameter.DRAW_SWORD_CD_REDUCE);
        }

        // 怪物受伤
        [HarmonyPatch(typeof(EnemyControl))]
        public static class EnemyControlPatch
        {
            private static HashSet<int> StoryIds = new HashSet<int>();

            public static void Init()
            {
                for (var i = 0; i <= 34; i++)
                {
                    StoryIds.Add(i);
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch("DealDamage")]
            static void DealDamagePrefix(EnemyControl __instance, ref float damage)
            {
                //自动剑返
                if (mod.Active && settings.FlySwardAutoBack &&
                    PlayerAnimControl.instance.playerParameter.SWORDS_COUNT == 0 &&
                    IsFlySwardBackCdOk())
                {
                    PlayerAnimControl.instance.ShouldDrawSword = true;
                    // 冷却时间
                    PlayerAnimControl.instance.DrawCoolDownTimer = 0;
                }

                // 剑返无cd
                if (mod.Active && settings.FlySwardBackNoCd)
                    PlayerAnimControl.instance.DrawCoolDownTimer = PlayerAnimControl.instance.drawCoolDown;
                // 伤害倍率
                if (mod.Active) damage *= settings.DamageMultiply;
            }

            // 修改过的
            private static HashSet<EnemyControl> patchedMonster = new HashSet<EnemyControl>();

            // 掉落
            [HarmonyPrefix]
            [HarmonyPatch("Drop")]
            static void DropPrefix(EnemyControl __instance)
            {
                if (mod.Active && settings.DropStoryUnlimited && __instance.storyChipDropProb != null)
                {
                    if (!patchedMonster.Contains(__instance))
                    {
                        patchedMonster.Add(__instance);
                        var dropSet = new HashSet<int>(StoryIds);
                        __instance.storyChipDrop.ForEach(t => dropSet.Remove(t));
                        foreach (var i in dropSet) //添加剩余的掉落
                        {
                            __instance.storyChipDrop.Add(i);
                            __instance.storyChipDropProb.Add(0.5f);
                        }
                    }
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch("OnDisable")]
            static void OnDisablePrefix(EnemyControl __instance)
            {
                patchedMonster.Remove(__instance); //移除patch标记
            }
        }

        [HarmonyPatch(typeof(TheBookOfAbyssControl), "Start")]
        public static class TheBookOfAbyssControlPatch
        {
            // static void Prefix(TheBookOfAbyssControl __instance)
            static void Postfix(TheBookOfAbyssControl __instance, EnemyControl ___enemyControl)
            {
                if (mod.Active && settings.DuPopEnable)
                {
                    // __instance.isLil = true;
                    LogF(
                        $"发现 TheBookOfAbyssControl MonsterID={___enemyControl.MonsterID} {UnityHelper.FmtAllComponents(__instance)}");
                }
            }
        }

        // 地图控制
        [HarmonyPatch(typeof(StageControl), "Start")]
        public static class StageControlPatch
        {
            static void Postfix(StageControl __instance, int ___mapConfigSceneID)
            {
                var mapConfig = XML_Loader.instance.mapConfigs[___mapConfigSceneID];
                LogF($"当前地图 {mapConfig.MapNameCHS} mapConfigSceneID={___mapConfigSceneID} stageMapId={__instance.stageMapId} " +
                     $"sceneLevel={__instance.sceneLevel} LevelLoad.Count={LevelLoad.instances.Count}");
                LevelLoad.instances.Select((s, i) =>
                {
                    LogF($"LevelLoad_{i} sceneIndex={JSONWriter.ToJson(s.sceneIndex)}");
                    return i;
                }).ToList().ToString();
            }
        }

        [HarmonyPatch(typeof(LevelLoad), "Load")]
        public static class LevelLoadPatch
        {
            static void Prefix(LevelLoad __instance, ref int ___index)
            {
                if (mod.Active && settings.ShuguaiEnable)
                {
                    // 判断是否存在书怪地图
                    var sIdList = __instance.sceneIndex.ToList();
                    var sId = sIdList.FirstOrDefault(v => BookOfAbyssMapConfigIds.Contains(GetMapConfigId(__instance.sceneLevel, v)));
                    if (sId > 0)
                    {
                        var mapConfig = XML_Loader.instance.mapConfigs[GetMapConfigId(__instance.sceneLevel, sId)];
                        LogF($"找到书怪地图 mapId={sId} {mapConfig.MapNameCHS}");
                        ___index = sIdList.IndexOf(sId);
                    }
                }
            }
        }

        static int GetSceneBuildIndex(SceneLevel leve, int mapId)
        {
            var sceneBuildIndex = mapId;
            switch (leve)
            {
                case SceneLevel.stage1:
                    sceneBuildIndex += 8;
                    break;
                case SceneLevel.stage2:
                    sceneBuildIndex += 93;
                    break;
                case SceneLevel.stage3:
                    sceneBuildIndex += 249;
                    break;
                case SceneLevel.stage4:
                    sceneBuildIndex += 269;
                    break;
                case SceneLevel.stage5:
                    sceneBuildIndex += 293;
                    break;
                case SceneLevel.stage6:
                    sceneBuildIndex += 314;
                    break;
            }

            return sceneBuildIndex;
        }

        // StageControl.Awake
        static int GetMapConfigId(SceneLevel level, int mapId)
        {
            var mapConfigSceneID = mapId;
            switch (level)
            {
                case SceneLevel.stage1:
                    mapConfigSceneID = mapConfigSceneID;
                    break;
                case SceneLevel.stage2:
                    mapConfigSceneID += 85;
                    break;
                case SceneLevel.stage3:
                    mapConfigSceneID += 241;
                    break;
                case SceneLevel.stage4:
                    mapConfigSceneID += 261;
                    break;
                case SceneLevel.stage5:
                    mapConfigSceneID += 285;
                    break;
                case SceneLevel.stage6:
                    mapConfigSceneID += 306;
                    break;
            }

            return mapConfigSceneID;
        }

        #endregion
    }
}