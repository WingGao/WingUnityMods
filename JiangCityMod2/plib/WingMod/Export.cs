using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using iFActionGame2;
using System.IO;
using System.Linq;
using HarmonyLib;

// ！！必须使用该注释区分 using与正文！！
// WingModScript
namespace iFActionScript
{
    // 一个简单的示例mod
    [HarmonyPatch]
    public static class MyExportMod
    {
        // Mod的入口
        [HarmonyPatch(typeof(WingSourceHarmPatcher), nameof(WingSourceHarmPatcher.OnPatch))]
        static void OnPatch()
        {
        }

        //存档加载完成
        [HarmonyPatch(typeof(SMain), nameof(SMain.init))]
        [HarmonyPostfix]
        static void OnGameLoad()
        {
            RV.Tips.show("MyExampleMod1", 100);
            try
            {
                Export();
            }
            catch (Exception e)
            {
                FileLog.Log(e.ToString());
            }
        }

        private static string CATE_COMPONENTS = "components";
        private static string CATE_BUILDINGS = "buildings";
        private static HashSet<int> HAND_BUILD = new HashSet<int>() {100, 101, 105};

        static void Export()
        {
            var data = new ExportData();
            data.version["jiangcity"] = "wing";
            var iconPrefix = "data/jiangcity/";
            var iconItemPos = "center;background-size:150%";
            var iconBuildPos = "center;background-size:cover";
            data.categories.Add(new ExportBase() {id = CATE_COMPONENTS, name = "加工件"});
            data.categories.Add(new ExportBase() {id = CATE_BUILDINGS, name = "建筑"});
            data.icons.Add(new ExportIcon() {id = CATE_COMPONENTS, file = $"{iconPrefix}Graphics/Icon/item_1.png", position = iconItemPos});
            data.icons.Add(new ExportIcon() {id = CATE_BUILDINGS, file = $"{iconPrefix}Graphics/Icon/build/100.png", position = iconBuildPos});
            var hash = new ExportHash();
            //物品导出
            var exportItems = new Dictionary<int, GDItem>(); //key=itemId
            foreach (var keyValuePair in RV.SelfSet.setFormula)
            {
                var formula = keyValuePair.Value;
                var rec = new ExportRecipe() {id = formula.name, name = formula.name, category = CATE_COMPONENTS, time = formula._time / 60};
                // FileLog.Log($"{formula.name} {formula}");
                // 排除厨房
                formula.buildId.DoIf(bid => !HAND_BUILD.Contains(bid), bid => { rec.producers.Add(RV.SelfSet.setBuild[bid].name); });
                if (rec.producers.Count == 0) continue;
                hash.recipes.Add(formula.name);
                formula.input.Do(x =>
                {
                    exportItems[x.id] = x.getData();
                    rec.inItems[x.getData().name] = x.num;
                    hash.items.Add(x.getData().name);
                });
                // FileLog.Log($"output={formula.output.Length}");
                formula.output.Do(x =>
                {
                    exportItems[x.id] = x.getData();
                    rec.outItems[x.getData().name] = x.num;
                    hash.items.Add(x.getData().name);
                });
                data.recipes.Add(rec);
            }

            //建筑导出
            var exportBuild = new Dictionary<int, GDBuild>();
            foreach (var gdBuild in RV.SelfSet.setBuild)
            {
                var build = gdBuild.Value;
                exportBuild[gdBuild.Key] = build;
                var ei = new ExportItem() {id = build.name, name = build.name, category = "buildings"};
                ExportIcon icon = null;
                if (build.name.EndsWith("传送带"))
                {
                    ei.id = "be_" + ei.id;
                    ei.belt = new ExportItemBelt() {speed = 1};
                    hash.belts.Add(ei.id);
                    ei.row = 1;
                    if (build.name.Contains("地下"))
                    {
                        icon = new ExportIcon() {id = ei.id, file = $"{iconPrefix}Graphics/Factory/Build/conveyor_belt{build.id-30}.png"};
                        ei.belt.speed = (int) Math.Pow(2, build.id - 30);
                    }
                    else
                    {
                        icon = new ExportIcon() {id = ei.id, file = $"{iconPrefix}Graphics/Factory/Build/conveyor_belt{build.id-20}.png"};
                        ei.belt.speed = (int) Math.Pow(2, build.id - 20);
                    }
                }
                else if (build.pow < 0)
                {
                    ei.factory = new ExportItemFactory() {usage = -build.pow, speed = 1};
                    ei.row = 2;
                    hash.factories.Add(ei.id);
                }

                hash.items.Add(ei.id);
                data.items.Add(ei);
                data.icons.Add(icon != null ? icon : new ExportIcon() {id = ei.id, file = $"{iconPrefix}Graphics/Icon/build/{build.id}.png", position = iconBuildPos});
            }


            exportItems.Values.Do(x =>
            {
                var ei = new ExportItem() {id = x.name, name = x.name, category = "components", row = x.itemType};
                if (x.price == 0)
                {
                    ei.row = 1;
                }
                else
                {
                    ei.row = x.level + 2;
                }

                data.icons.Add(new ExportIcon() {id = x.name, file = $"{iconPrefix}Graphics/Icon/{x.icon}", position = iconItemPos});
                data.items.Add(ei);
            });
            File.WriteAllText(IVal.BasePath + "data.json", JsonConvert.SerializeObject(data));
            File.WriteAllText(IVal.BasePath + "hash.json", JsonConvert.SerializeObject(hash));
        }


        class ExportData
        {
            public Dictionary<string, string> version = new Dictionary<string, string>();
            public List<ExportBase> categories = new();
            public List<ExportIcon> icons = new();
            public List<ExportItem> items = new();
            public List<ExportRecipe> recipes = new();
        }

        class ExportBase
        {
            public string id;
            public string name;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string? category;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? row;

            public int stack = 1;
        }

        class ExportItem : ExportBase
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public ExportItemFactory factory;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public ExportItemBelt belt;
        }

        class ExportItemFactory : ExportItemBelt
        {
            public string type = "electric";
            public int usage;
        }

        class ExportItemBelt
        {
            public int speed;
        }

        class ExportIcon
        {
            public string id;
            public string file;
            public string position;
        }

        class ExportRecipe : ExportBase
        {
            public int time;
            [JsonProperty("in")] public Dictionary<string, int> inItems = new();
            [JsonProperty("out")] public Dictionary<string, int> outItems = new();
            public List<string> producers = new();
        }

        class ExportHash
        {
            public HashSet<string> items = new();
            public HashSet<string> belts = new();
            public HashSet<string> factories = new();
            public HashSet<string> recipes = new();
            public HashSet<string> beacons = new();
            public HashSet<string> fuels = new();
            public HashSet<string> wagons = new();
            public HashSet<string> modules = new();
        }
    }
}