using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HarmonyLib.Tools;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using WingUtil.Harmony;
using Object = UnityEngine.Object;

namespace YiShang.WingMod.bie5
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class MyPlugin : BaseUnityPlugin
    {
        public const string GUID = "WingMod";
        public const string NAME = "WingMod";
        public const string VERSION = "0.0.2";
        public static Harmony harmony = new Harmony("WingMod");
        public static MyPlugin Instance;
        private ConfigEntry<int> CnfPerCarLoad; //驴车倍率
        private ConfigEntry<float> CnfRenwuMul; //任务倍率
        private static Dictionary<string, string> CityNameCnMap = new Dictionary<string, string>(); //城市 中文名->key

        public static void Log(String s)
        {
            Instance.Logger.LogInfo(s);
        }

        private void Awake()
        {
            Instance = this;
            HarmonyFileLog.Enabled = true;
            // Plugin startup logic
            Logger.LogInfo($"Plugin WingMod is loaded!");
            harmony.PatchAll();
            CnfPerCarLoad = Config.Bind("Global", "CnfPerCarLoad", 4, "每辆驴车装在数");
            CnfPerCarLoad.SettingChanged += setPerCarLoad;
            CnfRenwuMul = Config.Bind("Global", "CnfRenwuMul", 2f, "任务商誉倍数");
            //手动patch
            // var randomArray_RandomItem = AccessTools.Method(typeof(RandomArray), nameof(RandomArray.RandomItem), new Type[] {typeof(int).MakeByRefType()});
            // harmony.Patch(randomArray_RandomItem, null, new HarmonyMethod(AccessTools.Method(typeof(MyPatcher), nameof(MyPatcher.RandomArray_Patch))));

            // UniverseLib.Universe.Init(() => MyUI.Init());
            setPerCarLoad(null, null);
        }


        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
            // Config.Clear();
        }

        static List<T> ConvertToList<T>(List<Dictionary<string, object>> l)
        {
            var j = JsonConvert.SerializeObject(l);
            return JsonConvert.DeserializeObject<List<T>>(j);
        }

        static T ConvertTo<T>(Dictionary<string, object> dic)
        {
            var j = JsonConvert.SerializeObject(dic);
            return JsonConvert.DeserializeObject<T>(j);
        }


        private void Update()
        {
            // if (MyUI.uiBase != null)
            // {
            //     if (InputManager.GetKeyDown(KeyCode.F2))
            //         MyUI.Instance.Toggle();
            // }
        }

        /// <summary>
        /// 计算需要去的时间
        /// </summary>
        /// <param name="toCityName"></param>
        /// <returns></returns>
        static int CalcNeedDay(string toCityName)
        {
            Vector3 position = GameObject.Find("bigMap/cityFatherNode/" + toCityName).transform.position;
            return whereBuyWinClass.calNeedDay(GameObject.Find("bigMap/player").transform.position, position);
        }

        static void setPerCarLoad(object o, EventArgs a)
        {
            staticData.perCarLoad = Instance.CnfPerCarLoad.Value;
            Log($"staticData.perCarLoad={staticData.perCarLoad}");
        }

        [HarmonyPatch]
        static class MyPatcher
        {
            /// <summary>
            /// 任务无上限
            /// </summary>
            /// <param name="instructions"></param>
            /// <returns></returns>
            [HarmonyPatch(typeof(getRenwuWindowClass), nameof(getRenwuWindowClass.getButtonFunc))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> getButtonFunc_Patch(IEnumerable<CodeInstruction> instructions)
            {
                ILCursor cursor = new ILCursor(instructions);
                // cursor.LogTo(Log,"getButtonFunc");
                if (cursor.TryGotoNext(i =>
                        i.Instruction.MatchCallByName("System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>::get_Count")))
                {
                    cursor.Index += 1;
                    cursor.Next.Instruction.opcode = OpCodes.Ldc_I4_S;
                    cursor.Next.Instruction.operand = 9999;
                    cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(MyPatcher), "renwuMul"));
                }

                cursor.LogTo(Log, "getButtonFunc_Patch");
                return cursor.Context.AsEnumerable();
            }

            static void renwuMul()
            {
                var award = getRenwuWindowClass.selectRewnu["award"] as Dictionary<string, int>;
                if (award.ContainsKey("fame")) award["fame"] = (int) (award["fame"] * MyPlugin.Instance.CnfRenwuMul.Value);
            }

            /// <summary>
            /// 讨价还价-简单
            /// </summary>
            [HarmonyPatch(typeof(BargainWindowClass), nameof(BargainWindowClass.clickPauseButton))]
            [HarmonyPrefix]
            static void clickPauseButton_Patch(BargainWindowClass __instance, float ___width3)
            {
                var p = __instance.buoy.transform.localPosition; // = new Vector3((float) (___width3 / 2.0 - 1), __instance, 0);
                p.x = (float) (___width3 / 2.0 - 10);
                __instance.buoy.transform.localPosition = p;
            }

            /// <summary>
            /// 事件显示按钮效果
            /// </summary>
            [HarmonyPatch(typeof(chooseEventWindowClass), "OnEnable")]
            [HarmonyPrefix]
            static void chooseEventWindowClass_Patch()
            {
                if (!GameManager.globalGameData.selectedRandomCityEvent.Contains(string.Format("{0}A", cityRandomEventClass.eventCode)))
                    GameManager.globalGameData.selectedRandomCityEvent.Add(string.Format("{0}A", cityRandomEventClass.eventCode));
                if (!GameManager.globalGameData.selectedRandomCityEvent.Contains(string.Format("{0}B", cityRandomEventClass.eventCode)))
                    GameManager.globalGameData.selectedRandomCityEvent.Add(string.Format("{0}B", cityRandomEventClass.eventCode));
            }

            /// <summary>
            /// 事件都是正面属性
            /// </summary>
            [HarmonyPatch(typeof(RandomEventClass), "childAblityAdd")]
            [HarmonyPrefix]
            static void RandomEventClass_Patch(ref int changeValue)
            {
                if (changeValue < 0) changeValue *= -1;
            }


            /// <summary>
            /// 媒人100%
            /// </summary>
            [HarmonyPatch(typeof(meirenResultWindowClass), "generaMarryPeople")]
            [HarmonyPostfix]
            static void meirenResultWindowClass_Patch(ref List<int> ___rateList)
            {
                for (var i = 0; i < ___rateList.Count; i++)
                {
                    ___rateList[i] = 100;
                }
            }
            
            /// <summary>
            /// 商战必胜
            /// </summary>
            [HarmonyPatch(typeof(ShangZhanWindowClass), "OnEnable")]
            [HarmonyPostfix]
            static void ShangZhanWindowClass_Patch(ShangZhanWindowClass __instance)
            {
                __instance.enymySl.value = 1;
            }
            
            /// <summary>
            /// 学堂100%
            /// </summary>
            [HarmonyPatch(typeof(XueTangWindowClass), "calRate")]
            [HarmonyPostfix]
            static void XueTangWindowClass_Patch(ref int __result)
            {
                __result = 100;
            }

            /// <summary>
            /// 任务详情-显示距离
            /// </summary>
            [HarmonyPatch(typeof(renwuWindowClass), "openRenwuRightWindow")]
            [HarmonyPostfix]
            static void openRenwuRightWindow(renwuWindowClass __instance, Dictionary<string, object> renwu)
            {
                var rw = ConvertTo<YRenwu>(renwu);
                __instance.cityLabel2.setText(__instance.cityLabel2.getText() + " " + CalcNeedDay(rw.targetLocation));
            }

            // [HarmonyPatch(typeof(cityRenwuIndexWindowClass), "clickFinishRenwuButton")]
            // [HarmonyPostfix]
            // static void clickFinishRenwuButton(cityRenwuIndexWindowClass __instance)
            // {
            //     var rw = ConvertTo<YRenwu>(renwu);
            //     __instance.cityLabel2.setText(__instance.cityLabel2.getText() + " " + CalcNeedDay(rw.targetLocation));
            // }
        }

        /// <summary>
        /// 城市列表
        /// </summary>
        [HarmonyPatch(typeof(CityListWindowClass))]
        static class CityListWindowClassPatch
        {
            static HashSet<string> needGoods = new HashSet<string>(); //任务需要的商品
            private static Color32 NeedColor = new Color32(0x2d, 0x8a, 0x82, 0xff); //2D8A82

            public static MethodBase TargetMethod()
            {
                var type = AccessTools.FirstInner(typeof(CityListWindowClass), t => t.Name.StartsWith("<addPrefebPage1>"));
                return AccessTools.FirstMethod(type, method => method.Name.Contains("MoveNext"));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                ILCursor cursor = new ILCursor(instructions);
                if (cursor.TryGotoNext(i =>
                        i.Instruction.MatchCallByName("UnityEngine.Color::get_gray")))
                {
                    cursor.Index += 7; //component.gameObject.SetActive(false);之后
                    //ldloc.s V_18 => GameObject gameObject4 = transform.Find("Text").gameObject; 商品名称
                    cursor.Emit(OpCodes.Ldloc_S, 18);
                    cursor.Emit(OpCodes.Ldloc_S, 21); //商品名称
                    cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(CityListWindowClassPatch), "Patch"));
                }

                return cursor.Context.AsEnumerable();
            }

            static void Patch(GameObject obj4, string goodKey)
            {
                Log($"goodKey={goodKey}");
                if (obj4.GetComponent<Text>().color == Color.gray) //直接显示未解锁商品
                {
                    obj4.setTextTrans(goodKey);
                }

                if (needGoods.Contains(goodKey)) obj4.setTextColor(NeedColor); //任务需要
                // return x + "1";
            }

            /// <summary>
            /// 商品列表显示任务所需物品
            /// </summary>
            // [HarmonyPatch("addPrefebPage1")]
            // [HarmonyTranspiler]
            // static IEnumerable<CodeInstruction> addPrefebPage1_Patch(IEnumerable<CodeInstruction> instructions)
            // {
            //     ILCursor cursor = new ILCursor(instructions);
            //     if (cursor.TryGotoNext(i =>
            //             i.Instruction.MatchCallByName("System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>::get_Count")))
            //     {
            //         cursor.Index += 1;
            //         cursor.Next.Instruction.opcode = OpCodes.Ldc_I4_S;
            //         cursor.Next.Instruction.operand = 9999;
            //     }
            //     return cursor.Context.AsEnumerable();
            // } 
            // [HarmonyPatch("addPrefebPage1")]
            // [HarmonyPostfix]
            static void Prefix(object __instance)
            {
                CityListWindowClass window = WingAccessTools.GetFieldValue<CityListWindowClass>(__instance, "<>4__this");
                var cityDataCopy = WingAccessTools.GetFieldValue<List<Dictionary<string, object>>>(window, "cityDataCopy");
                Log("addPrefebPage1_patch");
                // 获取任务
                needGoods.Clear();
                Log($"当前任务 {JsonConvert.SerializeObject(GameManager.globalGameData.renwuData)}");
                var renwuList = ConvertToList<YRenwu>(GameManager.globalGameData.renwuData);
                renwuList.ForEach(rd => rd.goodList.ForEach(g => needGoods.Add(g)));
                //城市名映射
                cityDataCopy.ForEach(d =>
                {
                    var cityKey = d.getString("cityName");
                    var cnName = Localization.LocalizationString(cityKey);
                    CityNameCnMap[cnName] = cityKey;
                    Log($"cnName={cnName} -> {cityKey}");
                });
            }
        }
    }
}