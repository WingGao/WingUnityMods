using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using HarmonyLib;
using iFActionGame2;
using WingUtil.Harmony;

// ！！必须使用该注释区分 using与正文！！
// WingModScript
namespace iFActionScript
{
    // 后宫MOD
    // Map455 结婚地图
    [HarmonyPatch]
    public static class WingModHouGong
    {
        class MySettings
        {
            public int DaLaoPoId = -1; //大老婆id

            public Dictionary<int, MarrySetting> MarryMap = new Dictionary<int, MarrySetting>();
            [JsonIgnore] public List<MarrySetting> MarriedList = new List<MarrySetting>(); // 老婆列表
            [JsonIgnore] public MarrySetting NpcDinghun; //当前订婚的
            [JsonIgnore] public HashSet<int> MarrySkills = new HashSet<int>(); //拥有的技能

            // 筛选老婆顺序
            public void SortMarried()
            {
                MarriedList = MarryMap.Values.Where(x => x.step > 0).ToList();
                MarriedList.Sort((a, b) => a.index.CompareTo(b.index));
                FileLog.Log($@"MarriedList= {String.Join(",", MarriedList.Select(x =>
                {
                    MarrySkills.Add(MarryNpcDefines[x.npcId].SkillId);
                    return x.npcId;
                }))}");
                if (MarriedList.Count > 0 && MarriedList.First().step >= STEP_JIEHUN) DaLaoPoId = MarriedList.First().npcId; //大老婆
                //找到订婚者,当前只能有一个订婚者
                var d = MarriedList.LastOrDefault();
                if (d != null && d.step == STEP_DINGHUN) NpcDinghun = d;
                else NpcDinghun = null;
                FileLog.Log($"NpcDinghun= {NpcDinghun?.npcId}");
            }
        }

        class MarrySetting
        {
            public int npcId;
            public int step;
            public long index = -1; //结婚顺序
        }

        static int STEP_DINGHUN = 1; //订婚
        static int STEP_JIEHUN = 3; //结婚了

        private static MySettings _settings = new MySettings();

        /// <summary>
        /// 当前结婚对象
        /// </summary>
        /// RV.GameData.value[165] 婚期
        private static int GameValueMarryId
        {
            get => (int) RV.GameData.value[166];
            set => RV.GameData.value[166] = value;
        }

        class MarryDefine
        {
            public Type Clz;
            public int NpcId;
            public string SkillDef; //伴侣技能描述
            public int SkillId;
            public Dictionary<int, Type> AffectNpcList = new();
        }


        //结婚人物配置
        private static Dictionary<int, MarryDefine> MarryNpcDefines = new Dictionary<int, MarryDefine>()
        {
            {
                //柴霏霏
                7, new MarryDefine() {Clz = typeof(NChaifeifei), NpcId = 7, SkillDef = "[丰裕]进货原材料数量+10%", SkillId = 50}
            },
            {
                //柳依依
                12, new MarryDefine() {Clz = typeof(NLiujianjian), NpcId = 12, SkillDef = "[活力]耐力恢复+1", SkillId = 20}
            },
            {
                //礼嫣
                14, new MarryDefine()
                {
                    Clz = typeof(NLiyan), NpcId = 14, SkillDef = "[顿悟]悟性增加20点", SkillId = 60,
                    AffectNpcList = {{27, typeof(NHongxian)}}
                }
            },
            {
                //陆蝉衣
                16, new MarryDefine() {Clz = typeof(NLuchanyi), NpcId = 16, SkillDef = "[妙手]手工制造数量+1", SkillId = 40}
            },
        };

        //单纯受影响的npc列表，key=npcID,val=结婚的npcid
        static Dictionary<int, int> AffectNpcIdMap = new Dictionary<int, int>();

        // Mod的入口
        [HarmonyPatch(typeof(WingSourceHarmPatcher), nameof(WingSourceHarmPatcher.OnPatch))]
        [HarmonyPostfix]
        static void OnPatch()
        {
            FileLog.Log("WingModHouGong加载");
        }

        //存档保存
        [HarmonyPatch(typeof(WingSourceHarmPatcher), nameof(WingSourceHarmPatcher.OnSaveWrite))]
        [HarmonyPostfix]
        static void OnSaveWrite()
        {
            WingSourceHarmPatcher.ModSaveSettings["WingModHouGong"] = JsonConvert.SerializeObject(_settings);
        }

        //存档加载
        [HarmonyPatch(typeof(WingSourceHarmPatcher), nameof(WingSourceHarmPatcher.OnSaveLoad))]
        [HarmonyPostfix]
        static void OnSaveLoad(GMain gmain)
        {
            if (WingSourceHarmPatcher.ModSaveSettings.ContainsKey("WingModHouGong"))
            {
                _settings = JsonConvert.DeserializeObject<MySettings>(WingSourceHarmPatcher.ModSaveSettings["WingModHouGong"]);
            }
            else _settings = new MySettings();

            if (_settings.MarryMap.Count == 0) //初始化
            {
                // 判断当前是否结婚
                if (gmain.marryID != -1)
                {
                    _settings.DaLaoPoId = gmain.marryID;
                    _settings.MarryMap[_settings.DaLaoPoId] = new MarrySetting() {npcId = gmain.marryID, step = STEP_JIEHUN, index = 0};
                }
            }

            _settings.SortMarried();
            //初始化受影响的常规npc
            AffectNpcIdMap.Clear();
            foreach (var marryNpcDefine in MarryNpcDefines.Values)
            {
                if (marryNpcDefine.AffectNpcList != null)
                {
                    foreach (var key in marryNpcDefine.AffectNpcList.Keys)
                    {
                        AffectNpcIdMap[key] = marryNpcDefine.NpcId;
                    }
                }
            }
        }


        static MarrySetting GetMarrySetting(int npcId)
        {
            if (!MarryNpcDefines.ContainsKey(npcId)) return null;
            if (!_settings.MarryMap.ContainsKey(npcId)) _settings.MarryMap[npcId] = new MarrySetting() {npcId = npcId};
            return _settings.MarryMap[npcId];
        }


        /// <summary>
        /// 统一在操作的时候 将对象设置
        /// </summary>
        class MarryNpcBase
        {
            public static void BasePrefix(int npcId, bool noMarry)
            {
                var marryNpcId = npcId;
                if (AffectNpcIdMap.ContainsKey(npcId)) marryNpcId = AffectNpcIdMap[npcId]; //普通NPC找到结婚的NPC
                // 应用后宫数据
                var ms = GetMarrySetting(marryNpcId);
                if (ms == null) return;

                if (ms.step >= STEP_DINGHUN) //与当前npc结婚了
                {
                    if (ms.step >= STEP_JIEHUN) RV.GameData.marryID = marryNpcId; //结婚了
                    else RV.GameData.marryID = -1;
                    GameValueMarryId = marryNpcId;
                }
                else if (noMarry)
                {
                    RV.GameData.marryID = -1;
                    if (_settings.NpcDinghun != null) GameValueMarryId = _settings.NpcDinghun.npcId;
                    else GameValueMarryId = -1;
                }
            }

            public static void BasePostfix()
            {
                // 还原
                RV.GameData.marryID = _settings.DaLaoPoId;
                GameValueMarryId = _settings.DaLaoPoId;
            }
        }

        /// <summary>
        /// NPC修改送礼
        [HarmonyPatch]
        class Npc_GiftGiving
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                return MarryNpcDefines.Values.Select(t => AccessTools.FirstMethod(t.Clz, m => m.Name == nameof(NPCBase.GiftGiving)));
            }

            static void Prefix(object __instance, int level)
            {
                var npc = __instance as NPCBase;
                MarryNpcBase.BasePrefix(npc.id, true);
                if (level == 1666) //同心锁
                {
                    if (GameValueMarryId < 0) //允许订婚
                    {
                        var ms = GetMarrySetting(npc.id);
                        if (ms.step < STEP_DINGHUN) //允许订婚
                        {
                            ms.index = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            ms.step = STEP_DINGHUN;
                            _settings.SortMarried();
                        }
                    }
                }
            }

            static void Postfix(object __instance, int level)
            {
                var npc = __instance as NPCBase;
                MarryNpcBase.BasePostfix();
            }
        }

        /// <summary>
        /// 结婚行为影响的npc
        /// </summary>
        /// <returns></returns>
        static List<Type> GetMarryAndAffectNpcList()
        {
            var npcList = new List<Type>();
            foreach (var marryDefine in MarryNpcDefines.Values)
            {
                npcList.Add(marryDefine.Clz);
                if (marryDefine.AffectNpcList != null) npcList.AddRange(marryDefine.AffectNpcList.Values);
            }

            return npcList;
        }

        /// <summary>
        /// NPC当日动作
        [HarmonyPatch]
        class Npc_initDay
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                var methods = new List<MethodInfo>();
                foreach (var t in GetMarryAndAffectNpcList())
                {
                    var mInitDay = AccessTools.FirstMethod(t, m => m.Name == nameof(NPCBase.initDay));
                    if (mInitDay != null) methods.Add(mInitDay);
                }

                return methods;
            }

            static void Prefix(object __instance)
            {
                var npc = __instance as NPCBase;
                MarryNpcBase.BasePrefix(npc.id, true);
            }

            static void Postfix(object __instance)
            {
                var npc = __instance as NPCBase;
                var ms = GetMarrySetting(npc.id);
                if (ms != null && ms.step >= STEP_JIEHUN)
                {
                    //并排
                    var idx = _settings.MarriedList.IndexOf(ms);
                    if (RV.GameData.selfHomeLevel == 0)
                    {
                        npc.nowX = 272 + 20 * idx;
                    }
                    else if (RV.GameData.selfHomeLevel == 1)
                    {
                        npc.nowX = 352 + 20 * idx;
                    }
                }

                MarryNpcBase.BasePostfix();
            }
        }

        /// <summary>
        /// NPC其他动作
        [HarmonyPatch]
        class Npc_makeAction
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                var methods = new List<MethodInfo>();
                foreach (var t in GetMarryAndAffectNpcList())
                {
                    var m1 = AccessTools.FirstMethod(t, m => m.Name == "makeAction");
                    if (m1 != null) methods.Add(m1);
                }

                return methods;
            }

            static void Prefix(object __instance)
            {
                var npc = __instance as NPCBase;
                MarryNpcBase.BasePrefix(npc.id, true);
            }

            static void Postfix(object __instance)
            {
                var npc = __instance as NPCBase;
                MarryNpcBase.BasePostfix();
            }
        }

        /// <summary>
        /// 通用婚后工坊行程
        /// </summary>
        [HarmonyPatch]
        class Npc_hunhou
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                var methods = new List<MethodInfo>()
                {
                    AccessTools.Method(typeof(NLiyan), nameof(NLiyan.standToFront)),
                    AccessTools.Method(typeof(NLiyan), nameof(NLiyan.frontToStand)),
                    AccessTools.Method(typeof(NLiyan), nameof(NLiyan.exitToStand)),
                    AccessTools.Method(typeof(NLiyan), nameof(NLiyan.wellToStand)),
                    AccessTools.Method(typeof(NLiyan), nameof(NLiyan.houseToWell)),
                };

                return methods;
            }

            static void Postfix(NPCBase npc)
            {
                var ms = GetMarrySetting(npc.id);
                if (ms != null && ms.step >= STEP_JIEHUN)
                {
                    var lastAct = npc.actionList.LastOrDefault();
                    if (lastAct != null)
                    {
                        //并排
                        var idx = _settings.MarriedList.IndexOf(ms);
                        lastAct.startX += 20 * idx;
                    }
                }
            }
        }

        /// <summary>
        /// 处理婚期
        /// </summary>
        [HarmonyPatch(typeof(GLDayLogic), nameof(GLDayLogic.dayDo))]
        [HarmonyPrefix]
        static void GLDayLogic_dayDo()
        {
            if ((int) RV.GameData.value[165] == 3 && _settings.NpcDinghun != null)
            {
                FileLog.Log($"GLDayLogic_dayDo 结婚了 {_settings.NpcDinghun.npcId}");
                //结婚当天
                GameValueMarryId = _settings.NpcDinghun.npcId; //设置订婚者
                RV.GameData.marryID = -1; //游戏内需要
                _settings.MarryMap[_settings.NpcDinghun.npcId].step = STEP_JIEHUN;
                _settings.NpcDinghun = null;
            }
        }

        /// <summary>
        /// 显示多个伴侣技能
        /// </summary>
        [HarmonyPatch(typeof(WMenuSocial))]
        class WMenuSocialPatch
        {
            [HarmonyPatch(MethodType.Constructor)]
            [HarmonyPostfix]
            static void WMenuSocial(WMenuSocial __instance, IViewport ___view, List<CCheckBof> ___checks, List<ISprite> ___npcDraws, List<LRegion> ___regions)
            {
                //伴侣技能

                int npcListIdx = 0;
                int marryNum = 0;
                for (int i = 0; i < RV.NPCList.Count; i++)
                {
                    NPCBase npc = RV.NPCList[i];
                    if (npc.isSecondary) continue;
                    var check = ___checks[npcListIdx];
                    if (npc.canMarry && npc.isMarry && MarryNpcDefines.ContainsKey(npc.id))
                    {
                        // FileLog.Log($"WMenuSocial {npc.name}");
                        if (marryNum > 0) //跳过第一个
                        {
                            var marrySkill = new ISprite(RF.LoadBitmap("System/Menu/marrySkill.png"), ___view);
                            marrySkill.z = 25;
                            marrySkill.x = 430;
                            marrySkill.y = check.y + 25;
                            ___npcDraws.Add(marrySkill);

                            LRegion region = new LRegion(marrySkill, ___view);
                            region.tag = $"伴侣技能 - \\n{MarryNpcDefines[npc.id].SkillDef}";
                            region.onEnterRegion += __instance.enterRegion;
                            region.onLeaveRegion += __instance.leaveRegion;
                            ___regions.Add(region);
                        }

                        marryNum++;
                    }

                    npcListIdx++;
                }
            }
        }

        /// <summary>
        /// 结婚技能的影响修改
        /// </summary>
        [HarmonyPatch]
        class MarrySkillPatch
        {
            static void Patch(int checkSkillId)
            {
                if (_settings.MarrySkills.Contains(checkSkillId)) RV.GameData.value[49] = checkSkillId;
            }

            [HarmonyPatch(typeof(GDGongFu), nameof(GDGongFu.getMax))]
            [HarmonyPrefix]
            static void GDGongFu_getMax()
            {
                Patch(30);
            }

            [HarmonyPatch(typeof(GOre), nameof(GOre.getCL))]
            [HarmonyPrefix]
            static void apply_method1()
            {
                Patch(50);
            }

            [HarmonyPatch(typeof(LActorEx), nameof(LActorEx.updateCtrl))]
            [HarmonyPrefix]
            static void apply_method3()
            {
                Patch(20);
            }

            [HarmonyPatch(typeof(SInventory), nameof(SInventory.drawAllItems))]
            [HarmonyPrefix]
            static void apply_method4()
            {
                Patch(10);
            }

            [HarmonyPatch(typeof(SInventory), nameof(SInventory.update))]
            [HarmonyPrefix]
            static void apply_method6()
            {
                Patch(10);
            }

            [HarmonyPatch(typeof(SInventory), "reLoad")]
            [HarmonyPrefix]
            static void apply_method5()
            {
                Patch(10);
            }

            [HarmonyPatch(typeof(WBuildMakeTable), "makeItem")]
            [HarmonyPrefix]
            static void apply_method7()
            {
                Patch(40);
            }

            [HarmonyPatch(typeof(GActor), nameof(GActor.talent), MethodType.Getter)]
            [HarmonyPrefix]
            static void apply_method8()
            {
                Patch(60);
            }
        }
    }
}