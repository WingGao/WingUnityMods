using System.Collections.Generic;
using FairyGUI;
using HarmonyLib;
using ModLoaderLite;
using ModLoaderLite.Config;
using XiaWorld;

namespace AmzCuiTiAll
{

    public static class AmzCuiTiAll
    {
        public static bool CuiTiAllEnabled = true;

        public static void OnInit()
        {
            // KLog.Dbg("[AmzCuiTiAll] OnInit x");
        }
        
        public static void OnLoad()
        {
            KLog.Dbg("[AmzCuiTiAll] OnLoad x");
            Configuration.AddCheckBox(nameof (AmzCuiTiAll), "CuiTiAllEnabled", "淬体显示全部词条", CuiTiAllEnabled);
            Configuration.Subscribe(new EventCallback0(AmzCuiTiAll.HandleConfig));
        }

        public static void OnSave()
        {
            // Dictionary<int, int> dictionary = new Dictionary<int, int>();
            // foreach (KeyValuePair<int, int> jianzhenCore in SuperJianZhen.SuperJianZhen.JianzhenCores)
            // {
            //     if (ThingMgr.Instance.FindThingByID(jianzhenCore.Key) is BuildingThing)
            //         dictionary.Add(jianzhenCore.Key, jianzhenCore.Value);
            // }
            // MLLMain.AddOrOverWriteSave("jnjly.SuperJianZhen.jianzhenCores", (object) dictionary);
            // SuperJianZhen.SuperJianZhen.JianzhenCores = dictionary;
        }

        private static void HandleConfig()
        {
            AmzCuiTiAll.CuiTiAllEnabled = Configuration.GetCheckBox(nameof (AmzCuiTiAll), "CuiTiAllEnabled");
        }
    }
}