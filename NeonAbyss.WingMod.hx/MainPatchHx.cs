using System;
using HarmonyLib;
using NEON.UI;

namespace NeonAbyss.WingMod.hx
{
    public class MainPatchHx
    {
        public static void Start()
        {
            Harmony.CreateAndPatchAll(typeof(MainPatchHx));
        }

        [HarmonyPatch(typeof(MapManager), "ShowFullMap")]
        class Patch
        {
            static bool Prefix()
            {
                MapManager.Instance.RefreshRoomOnlyIcon();
                MapManager.Instance.RefreshRoomOnlyIcon(true);
                return true;
            }
        }
    }
}