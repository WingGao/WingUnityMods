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
    }
}