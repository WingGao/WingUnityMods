using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using XiaWorld;

namespace AmzCuiTiAll
{
    [HarmonyPatch(typeof(PracticeMgr))]
    public static class PracticeMgrPatch
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
        
        [HarmonyPostfix]
        [HarmonyPatch( "GetRandomQuenchingLabelList")]
        static void GetRandomQuenchingLabelListPost(PracticeMgr __instance, Npc npc,string part, ref List<BPLabelCacheDef.LabelDataTemp> __result)
        {
            if (!AmzCuiTiAll.Enabled || __result.Count==0) return;

            BodyPartDef def = npc.PropertyMgr.BodyData.GetPart(part).def;
            string cachename = "BaseCache";
            if (!string.IsNullOrEmpty(def.BPQLabelBaseCache))
                cachename = def.BPQLabelBaseCache;
            BPLabelCacheDef bpLabelCacheDef = __instance.GetBPLabelCacheDef(cachename);
            __result.Clear();
            var list = __result;
            bpLabelCacheDef.Labels.ForEach(x =>
            {
                list.Add(new BPLabelCacheDef.LabelDataTemp()
                {
                    Label = x.Label,
                    Lv = x.Lv
                });
            });
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