using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using XiaWorld;

namespace AmzCuiTiAll
{
    [HarmonyPatch(typeof(PracticeMgr))]
    internal static class PracticeMgrPatch
    {
        // [HarmonyPrefix]
        // [HarmonyPatch( "GetRandomQuenchingLabelList")]
        // static void GetRandomQuenchingLabelListPrefix(ref int count)
        // {
        //     KLog.Dbg("[AmzCuiTiAll] {0} count={1}",AmzCuiTiAll.Enabled,count);
        //     if (!AmzCuiTiAll.Enabled || count<=0) return;
        //
        //     BPLabelCacheDef bpLabelCacheDef = this.GetBPLabelCacheDef(cachename);
        //     count = 30;
        // }

        /// <summary>
        /// 淬体返回所有可用词条，一步到位
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("GetRandomQuenchingLabelList")]
        static void GetRandomQuenchingLabelListPost(PracticeMgr __instance, Npc npc, string part, string item,
            ref List<BPLabelCacheDef.LabelDataTemp> __result)
        {
            if (!AmzCuiTiAll.CuiTiAllEnabled || __result.Count == 0) return;

            NpcBodyData.NpcBodyPart partB = npc.PropertyMgr.BodyData.GetPart(part);
            BodyPartDef def = partB.def;
            // 基础池子
            string cachename = "BaseCache";
            if (!string.IsNullOrEmpty(def.BPQLabelBaseCache))
                cachename = def.BPQLabelBaseCache;

            __result.Clear();
            var list = __result;
            // 排重
            var labelMap = new Dictionary<string, Boolean>();
            var sMapBpLabelCacheDefs = AccessTools.StaticFieldRefAccess<Dictionary<string, BPLabelCacheDef>>(
                typeof(PracticeMgr),
                "s_mapBPLabelCacheDefs");


            foreach (var sMapBpLabelCacheDef in sMapBpLabelCacheDefs)
            {
                // 判断池子
                if (!(sMapBpLabelCacheDef.Key.StartsWith("Cache") || sMapBpLabelCacheDef.Key == cachename))
                {
                    continue;
                }

                KLog.Dbg("[AmzCuiTiAll] 当前池子{0}", sMapBpLabelCacheDef.Key);
                var bpLabelCacheDef = sMapBpLabelCacheDef.Value;

                var labelDataItemsRef =
                    AccessTools.FieldRefAccess<List<string>>(typeof(BPLabelCacheDef.LabelData), "Items");

                bpLabelCacheDef.Labels.ForEach(labelData =>
                {
                    if (!labelMap.ContainsKey(labelData.Label))
                    {
                        labelMap.Add(labelData.Label, true);
                        // 获取fit物品
                        var fitItem = item;
                        if (!string.IsNullOrEmpty(labelData.ItemStr))
                        {
                            var items = labelDataItemsRef.Invoke(labelData);
                            if (items == null) labelData.IsFit(def.Kind.ToString(), item, def.Name); //单纯的初始化
                            fitItem = labelDataItemsRef.Invoke(labelData)[0];
                        }

                        if (labelData.IsFit(def.Kind.ToString(), fitItem, def.Name))
                        {
                            // 计算lv
                            int num = labelData.Lv;
                            if (!string.IsNullOrEmpty(item))
                            {
                                ThingDef tdef = ThingMgr.Instance.GetDef(g_emThingType.Item, item);
                                if (tdef.Rate > 0)
                                {
                                    int a = Mathf.Max(0, tdef.Rate);
                                    num = Mathf.Max(1, num * World.RandomRange(a, 2 * a, GMathUtl.RandomType.emNone));
                                }
                            }

                            list.Add(new BPLabelCacheDef.LabelDataTemp()
                            {
                                Label = labelData.Label,
                                Lv = num
                            });
                        }
                    }
                });
            }
        }

        // [HarmonyPostfix]
        // [HarmonyPatch( "GetRandomBodyQuenchingLabelByCache")]
        // static void GetRandomBodyQuenchingLabelByCachePost(ref int count)
        // {
        //     if (!AmzCuiTiAll.Enabled || count<=0) return;
        //
        //     count = 30;
        // }
    }
}