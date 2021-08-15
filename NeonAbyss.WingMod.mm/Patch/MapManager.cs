#pragma warning disable CS0626
using System.Collections;
using System.IO;
using MonoMod;
using NEON.Framework;
using NEON.Game;
using NEON.Game.GameModes;
using NEON.Game.LootSystem;
using NEON.Game.Managers;
using UnityEngine;
using UnityEngine.Events;

namespace NEON.UI
{
    public class patch_MapManager : MapManager
    {
        // public extern IEnumerator orig_Start();
        // protected IEnumerator Start()
        // {
        //     yield return orig_Start();
        // }
        private bool patched = false;

        [MonoModIgnore]
        private extern void orig_ShowFullMap(bool teleport);

        [MonoModPublic]
        public void ShowFullMap(bool teleport)
        {
            // 随时可以传送
            orig_ShowFullMap(true);
            // 全开
            RefreshRoomOnlyIcon();
            RefreshRoomOnlyIcon(true);

            
            // Services.ExhibitionDataService.Data.
        }

        [MonoModIgnore]
        public extern void orig_ConstructMinimap(Level lvl);

        public new void ConstructMinimap(Level lvl)
        {
            // 全局入口
            // WingPatch.StartPatch();

            orig_ConstructMinimap(lvl);
            RefreshRoomOnlyBg();
            RefreshRoomOnlyIcon();
            RefreshRoomOnlyIcon(true);

            // 显示物品详情
            var ps = (NEONPlayerState) Global.PlayerState;
            ps.GetShowItemDescription().Set(1);
            ps.GetExplosionProtectProbability().Set(1);
            ps.ExplosionProtect = true; //炸弹保护
            ps.ShopRefeshItem = true; //商店刷新
            ps.canRestockOnPurchase = true;
            ps.ShopHalfPrice = true;
            // WingLog.Log("WingLog patched");
            // LocalPatch();
            // File.AppendAllText("D:\\wing_mod.log", "WingLog patched");
        }

        public void LocalPatch()
        {
            if (patched) return;
            patched = true;
            Global.GetService<EventHub>().OnMonsterKilled +=
                new UnityAction<MonsterActor, Component>(this.OnMonsterKilled);
        }

        public void OnMonsterKilled(MonsterActor m, Component source)
        {
            OnMonsterKilled2();
        }

        public void OnMonsterKilled2()
        {
            var player = Global.GetGameMode<ControlPlayerMode>().Player;
            Global.GetService<LootManager>().SpawnSkillLoot((Vector2) player.transform.position);
        }
    }
}