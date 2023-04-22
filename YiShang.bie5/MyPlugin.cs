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
        private ConfigEntry<int> CnfOreDropMul; //矿石掉落倍率
        private ConfigEntry<float> CnfMoveSpeedMul; //移动速度
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
            CnfOreDropMul = Config.Bind("Global", "CnfOreDropMul", 1, "Rocks drop multiplier");
            CnfMoveSpeedMul = Config.Bind("Global", "CnfMoveSpeedMul", 1f, "Movement speed multiplier");
            //手动patch
            // var randomArray_RandomItem = AccessTools.Method(typeof(RandomArray), nameof(RandomArray.RandomItem), new Type[] {typeof(int).MakeByRefType()});
            // harmony.Patch(randomArray_RandomItem, null, new HarmonyMethod(AccessTools.Method(typeof(MyPatcher), nameof(MyPatcher.RandomArray_Patch))));

            // UniverseLib.Universe.Init(() => MyUI.Init());
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
                }

                cursor.LogTo(Log, "getButtonFunc_Patch");
                return cursor.Context.AsEnumerable();
            }

            /// <summary>
            /// 讨价还价-简单
            /// </summary>
            [HarmonyPatch(typeof(BargainWindowClass), nameof(BargainWindowClass.clickPauseButton))]
            [HarmonyPrefix]
            static void clickPauseButton_Patch(BargainWindowClass __instance, float ___width3)
            {
                var p = __instance.buoy.transform.localPosition; // = new Vector3((float) (___width3 / 2.0 - 1), __instance, 0);
                p.x = (float) (___width3 / 2.0 - 1);
                __instance.buoy.transform.localPosition = p;
            }
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
                Log($"任务商品 {renwuList.First().goodList.First()}");
                //城市名映射
                cityDataCopy.ForEach(d =>
                {
                    var cityKey = d.getString("cityName");
                    var cnName = Localization.LocalizationString(cityKey);
                    CityNameCnMap[cnName] = cityKey;
                    Log($"cnName={cnName} -> {cityKey}");
                });
                // foreach (var cityObj in Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name.StartsWith("cityGoodInfoLine")))
                // {
                //     var cityNameCn = cityObj.transform.Find("name").gameObject.getText();
                //     if (CityNameCnMap.ContainsKey(cityNameCn))
                //     {
                //         Dictionary<string, object> cityDataStatic = dataToolClass.findCityDataStatic(CityNameCnMap[cityNameCn]);
                //         var cityData = ConvertTo<YCity>(cityDataStatic);
                //         Log($"当前城市 {JsonConvert.SerializeObject(cityDataStatic)}");
                //         for (int i = 0; i < 4; ++i) //获取商品数据
                //         {
                //             if (needGoods.Contains(cityData.goodsData[i].goodName)) //任务商品
                //             {
                //                 Transform transform = cityObj.transform.Find($"good{i}");
                //                 transform.Find("Text").gameObject.setTextColor(Color.green);
                //             }
                //         }
                //     }
                // }
            }
        }
    }
}