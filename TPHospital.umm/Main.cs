using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using TH20;
using UnityEngine;
using UnityModManagerNet;

namespace WingMod
{
    public enum JobQualifiEnum
    {
        Off,
        None,
        User,
        Log,
    }

    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Header("修改")] [Draw("培训加成")] public float LearnMul = 1;
        [Draw("招聘技能")] public JobQualifiEnum JobQualificationOp = JobQualifiEnum.Off;
        [Draw("招聘最大等级")] public bool JobMaxRank = true;
        [Draw("招聘等待加成")] public float JobWaitMul = 0.5f;
        [Draw("房间治疗加成")] public float RoomTreatmentMul = 2f;
        [Draw("房间诊断加成")] public float RoomDiagnosisMul = 2f;

        [Header("员工")] [Draw("休息倍率")] public float StaffBreakMul = 0.5f;
        [Draw("工资倍率")] public float StaffSalaryMul = 1f;

        [Header("Debug")] [Draw("ShowDebugInfo")]
        public bool ShowDebugInfo = false;


        public void OnInit()
        {
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
            DebugVars.ShowDebugInfo.Value = ShowDebugInfo;
        }

        public void OnChange()
        {
        }
    }

    public class Main
    {
        public static UnityModManager.ModEntry mod;

        // 配置
        public static Settings settings;

        static void LogF(string str)
        {
            UnityModManager.Logger.Log(str, "[WingMod] ");
        }

        /// <summary>
        /// 加载
        /// https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
        /// </summary>
        /// <param name="modEntry"></param>
        static void Load(UnityModManager.ModEntry modEntry)
        {
            settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            settings.OnInit();

            // Harmony.DEBUG = true;
            // FileLog.Reset();
            // WingLog.Reset();

            mod = modEntry;
            var harmony = new Harmony(mod.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            // modEntry.OnShowGUI = OnShowGUI;
            // modEntry.OnHideGUI = OnHideGUI;
            //
            // EnemyControlPatch.Init();

            LogF("WingMod load");
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Draw(modEntry);
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        //全局
        [HarmonyPatch(typeof(GameAlgorithms))]
        static class GameAlgorithmsPatch
        {
            /// <summary>
            /// 修改培训加成
            /// </summary>
            [HarmonyPatch("CalculateTrainingPointLearnRate")]
            [HarmonyPostfix]
            static void Postfix(ref float __result)
            {
                __result *= settings.LearnMul;
            }
        }

        /// <summary>
        /// 房间诊断加成
        /// </summary>
        [HarmonyPatch(typeof(RoomModifierDiagnosis))]
        static class RoomModifierDiagnosisPatch
        {
            [HarmonyPatch("Apply")]
            [HarmonyPrefix]
            static void ApplyPatch(ref float ____percentage)
            {
                if (____percentage >= 0) ____percentage = settings.RoomDiagnosisMul;
            }

            [HarmonyPatch("Percentage", MethodType.Getter)]
            [HarmonyPrefix]
            static void PercentagePatch(ref float ____percentage)
            {
                if (____percentage >= 0) ____percentage = settings.RoomDiagnosisMul;
            }
        }

        /// <summary>
        /// 房间治疗加成
        /// </summary>
        [HarmonyPatch(typeof(RoomModifierTreatment))]
        static class RoomModifierTreatmentPatch
        {
            [HarmonyPatch("Apply")]
            [HarmonyPrefix]
            static void ApplyPatch(ref float ____percentage)
            {
                if (____percentage >= 0) ____percentage = settings.RoomTreatmentMul;
            }

            [HarmonyPatch("Percentage", MethodType.Getter)]
            [HarmonyPrefix]
            static void PercentagePatch(ref float ____percentage)
            {
                if (____percentage >= 0) ____percentage = settings.RoomTreatmentMul;
            }
        }

        //员工补丁
        [HarmonyPatch(typeof(Staff))]
        static class StaffPatch
        {
            /// <summary>
            /// 修改员工休息时间
            /// </summary>
            [HarmonyPatch("GetBreakLength")]
            [HarmonyPostfix]
            static void GetBreakLengthPostfix(ref float __result)
            {
                __result *= settings.StaffBreakMul;
            }

            /// <summary>
            /// 修改员工工资
            /// </summary>
            [HarmonyPatch("GetSalary")]
            [HarmonyPrefix]
            static void GetSalaryPatch(ref int ____salary)
            {
                ____salary = Mathf.RoundToInt(____salary * settings.StaffSalaryMul);
            }
        }

        private static Dictionary<String, String> CnTermsDict = new Dictionary<string, string>
        {
            {"诊断学", "Qualification/Doctor_Diagnosis_1_Name"},
            {"诊断学II", "Qualification/Doctor_Diagnosis_2_Name"},
            {"诊断学III", "Qualification/Doctor_Diagnosis_3_Name"},
            {"诊断学IV", "Qualification/Doctor_Diagnosis_4_Name"},
            {"诊断学V", "Qualification/Doctor_Diagnosis_5_Name"},
            {"研究", "Qualification/Doctor_Research_1_Name"},
            {"研究II", "Qualification/Doctor_Research_2_Name"},
            {"研究III", "Qualification/Doctor_Research_3_Name"},
            {"研究IV", "Qualification/Doctor_Research_4_Name"},
            {"研究V", "Qualification/Doctor_Research_5_Name"},
            {"精神病学", "Qualification/Doctor_Psychiatry_1_Name"},
            {"精神病学II", "Qualification/Doctor_Psychiatry_2_Name"},
            {"精神病学III", "Qualification/Doctor_Psychiatry_3_Name"},
            {"精神病学IV", "Qualification/Doctor_Psychiatry_4_Name"},
            {"精神病学V", "Qualification/Doctor_Psychiatry_5_Name"},
            {"全科诊疗", "Qualification/Doctor_GeneralPractice_1_Name"},
            {"全科诊疗II", "Qualification/Doctor_GeneralPractice_2_Name"},
            {"全科诊疗III", "Qualification/Doctor_GeneralPractice_3_Name"},
            {"全科诊疗IV", "Qualification/Doctor_GeneralPractice_4_Name"},
            {"全科诊疗V", "Qualification/Doctor_GeneralPractice_5_Name"},
            {"治疗", "Qualification/Doctor_Treatment_1_Name"},
            {"治疗II", "Qualification/Doctor_Treatment_2_Name"},
            {"治疗III", "Qualification/Doctor_Treatment_3_Name"},
            {"治疗IV", "Qualification/Doctor_Treatment_4_Name"},
            {"治疗V", "Qualification/Doctor_Treatment_5_Name"},
            {"外科学", "Qualification/Doctor_Surgery_1_Name"},
            {"外科学II", "Qualification/Doctor_Surgery_2_Name"},
            {"外科学III", "Qualification/Doctor_Surgery_3_Name"},
            {"外科学IV", "Qualification/Doctor_Surgery_4_Name"},
            {"外科学V", "Qualification/Doctor_Surgery_5_Name"},
            {"放射学", "Qualification/Doctor_Radiology_1_Name"},
            {"遗传病学", "Qualification/Doctor_Genetics_1_Name"},
            //护士
            {"病房管理", "Qualification/Nurse_WardManagement_1_Name"},
            {"病房管理II", "Qualification/Nurse_WardManagement_2_Name"},
            {"病房管理III", "Qualification/Nurse_WardManagement_3_Name"},
            {"病房管理IV", "Qualification/Nurse_WardManagement_4_Name"},
            {"病房管理V", "Qualification/Nurse_WardManagement_5_Name"},
            {"注射", "Qualification/Nurse_Injections_1_Name"},
            {"药房管理", "Qualification/Nurse_Pharmacy_1_Name"},
            //助理
            {"营销学", "Qualification/Assistant_Marketing_1_Name"},
            {"营销学II", "Qualification/Assistant_Marketing_2_Name"},
            {"营销学III", "Qualification/Assistant_Marketing_3_Name"},
            {"营销学IV", "Qualification/Assistant_Marketing_4_Name"},
            {"营销学V", "Qualification/Assistant_Marketing_5_Name"},
            {"客户服务", "Qualification/Assistant_Service_1_Name"},
            {"客户服务II", "Qualification/Assistant_Service_2_Name"},
            {"客户服务III", "Qualification/Assistant_Service_3_Name"},
            {"客户服务IV", "Qualification/Assistant_Service_4_Name"},
            {"客户服务V", "Qualification/Assistant_Service_5_Name"},
            //勤杂
            {"维护", "Qualification/Janitor_Maintenance_1_Name"},
            {"维护II", "Qualification/Janitor_Maintenance_2_Name"},
            {"维护III", "Qualification/Janitor_Maintenance_3_Name"},
            {"维护IV", "Qualification/Janitor_Maintenance_4_Name"},
            {"维护V", "Qualification/Janitor_Maintenance_5_Name"},
            {"机械学", "Qualification/Janitor_Mechanics_1_Name"},
            {"机械学II", "Qualification/Janitor_Mechanics_2_Name"},
            {"机械学III", "Qualification/Janitor_Mechanics_3_Name"},
            {"机械学IV", "Qualification/Janitor_Mechanics_4_Name"},
            {"机械学V", "Qualification/Janitor_Mechanics_5_Name"},
            {"捉鬼术", "Qualification/Janitor_GhostCapture_1_Name"},
            //通用
            {"工作激情", "Qualification/General_Speed_1_Name"},
        };

        [HarmonyPatch(typeof(JobApplicant))]
        static class JobApplicantPatch
        {
            private static int DoctorIdx = 0;
            private static int NurserIdx = 0;
            private static int AssistantIdx = 0;
            private static int JanitorIdx = 0;

            /// <summary>
            /// 招聘空技能
            /// </summary>
            [HarmonyPatch("AssignRandomQualifications")]
            [HarmonyPostfix]
            static void AssignRandomQualificationsPrefix(ref JobApplicant __instance, Level level)
            {
                if (settings.JobMaxRank)
                {
                    try
                    {
                        AccessTools.Property(typeof(JobApplicant), "Rank").SetValue(__instance, 4);
                    }
                    catch (Exception e)
                    {
                    }
                }

                switch (settings.JobQualificationOp)
                {
                    case JobQualifiEnum.None:
                        __instance.Qualifications.Clear();
                        break;
                    case JobQualifiEnum.User: //根据模板添加
                    {
                        var temps = new List<List<String>>();
                        var tempIdx = 0;
                        switch (__instance.Definition._type)
                        {
                            case StaffDefinition.Type.Doctor:
                                tempIdx = DoctorIdx++;
                                temps = new List<List<String>>
                                {
                                    new List<String> {"全科诊疗", "全科诊疗II", "全科诊疗III", "全科诊疗IV", "全科诊疗V"},
                                    new List<String> {"精神病学", "精神病学II", "精神病学III", "精神病学IV", "精神病学V"},
                                    new List<String> {"治疗", "治疗II", "治疗III", "治疗IV", "治疗V"},
                                    new List<String> {"研究", "研究II", "研究III", "研究IV", "研究V"},
                                    new List<String> {"外科学", "外科学II", "外科学III", "外科学IV", "外科学V"},
                                    new List<String> {"放射学", "诊断学", "诊断学II", "诊断学III", "工作激情"},
                                    new List<String> {"遗传病学", "治疗", "治疗II", "治疗III", "治疗IV"},
                                };
                                break;
                            case StaffDefinition.Type.Nurse:
                                tempIdx = NurserIdx++;
                                temps = new List<List<String>>
                                {
                                    new List<String> {"药房管理", "注射", "治疗", "治疗II", "治疗III"},
                                    new List<String> {"治疗", "治疗II", "治疗III", "治疗IV", "治疗V"},
                                    new List<String> {"诊断学", "诊断学II", "诊断学III", "诊断学IV", "诊断学V"},
                                    new List<String> {"病房管理", "病房管理II", "病房管理III", "病房管理IV", "病房管理V"},
                                };
                                break;
                            case StaffDefinition.Type.Assistant:
                                tempIdx = AssistantIdx++;
                                temps = new List<List<String>>
                                {
                                    new List<String> {"客户服务", "客户服务II", "客户服务III", "客户服务IV", "客户服务V"},
                                    new List<String> {"营销学", "营销学II", "营销学III", "营销学IV", "营销学V"},
                                };
                                break;
                            case StaffDefinition.Type.Janitor:
                                tempIdx = JanitorIdx++;
                                temps = new List<List<String>>
                                {
                                    new List<String> {"维护", "维护II", "维护III", "维护IV", "维护V"},
                                    new List<String> {"机械学", "机械学II", "机械学III", "机械学IV", "机械学V"},
                                    new List<String> {"捉鬼术", "工作激情", "维护", "维护II", "维护III"},
                                };
                                break;
                        }

                        int capacity = __instance.MaxQualifications;
                        var qualDict = level.JobApplicantManager.Qualifications.List.ToDictionary(
                            x => x.Key.NameLocalised.Term,
                            x => x.Key);
                        var tempId = RandomUtils.GlobalRandomInstance.Next(-1, temps.Count); //有概率为空
                        __instance.Qualifications.Clear();
                        if (tempId >= 0)
                        {
                            var tempQuals = temps[tempIdx % temps.Count];
                            for (int index = 0; index < capacity && index < tempQuals.Count; ++index)
                            {
                                var qualCn = tempQuals[index];
                                var qualTerm = CnTermsDict[qualCn];
                                if (qualDict.ContainsKey(qualTerm))
                                {
                                    QualificationDefinition definition = qualDict[qualTerm];
                                    __instance.Qualifications.Add(new QualificationSlot(definition, true));
                                }
                            }
                        }

                        break;
                    }
                    case JobQualifiEnum.Log:
                        var sb = new StringBuilder();
                        sb.Append("生成=> ");
                        __instance.Qualifications.ForEach(s =>
                        {
                            sb.Append(
                                $"{{\"{s.Definition.NameLocalised.Translation}\",\"{s.Definition.NameLocalised.Term}\"}}, ");
                        });
                        LogF(sb.ToString());
                        break;
                }
            }
        }

        [HarmonyPatch(typeof(JobApplicantPool.Config))]
        static class JobApplicantPoolPatch
        {
            /// <summary>
            /// 招聘等待
            /// </summary>
            [HarmonyPatch("GetTimeUntilNextApplicant")]
            [HarmonyPostfix]
            static void GetTimeUntilNextApplicantPatch(ref float __result)
            {
                __result *= settings.JobWaitMul;
            }
        }
    }
}