#pragma warning disable CS0626
using System.Collections;
using MonoMod;
using NEON.Game;

namespace NEON.UI
{
    public class patch_MapManager : MapManager
    {
        // public extern IEnumerator orig_Start();
        // protected IEnumerator Start()
        // {
        //     yield return orig_Start();
        // }
        [MonoModIgnore]
        private extern void orig_ShowFullMap(bool teleport);

        [MonoModPublic]
        public void ShowFullMap(bool teleport)
        {
            // 全开
            orig_ShowFullMap(teleport);
            RefreshRoomOnlyIcon();
            RefreshRoomOnlyIcon(true);
        }
        [MonoModIgnore]
        public extern void orig_ConstructMinimap(Level lvl);

        public void ConstructMinimap(Level lvl)
        {
            orig_ConstructMinimap(lvl);
            RefreshRoomOnlyBg();
            RefreshRoomOnlyIcon();
            RefreshRoomOnlyIcon(true);
        }
    }
}