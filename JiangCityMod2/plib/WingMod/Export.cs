using System;
using System;
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
    // 一个简单的示例mod
    [HarmonyPatch]
    public static class MyExampleMod1
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
        }

        static void Export()
        {
            var exportItems = new Dictionary<int, GDItem>(); //key=itemId
            foreach (var keyValuePair in RV.SelfSet.setFormula)
            {
                keyValuePair.Value.input.Do(x => exportItems.Add(x.id, x.getData()));
                keyValuePair.Value.output.Do(x => exportItems.Add(x.id, x.getData()));
            }

            var data = new ExportData();
            data.items = exportItems.Values.Select(x => new ExportItem() {id = x.name, name = x.name, category = "components", row = x.itemType}).ToList();
            File.WriteAllText(IVal.BasePath + "export.json", JsonConvert.ToString(data));
        }

        class ExportData
        {
            public List<ExportItem> items;
        }

        class ExportItem
        {
            public string id;
            public string name;
            public string category;
            public int row;
        }
    }
}