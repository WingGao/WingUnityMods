namespace NEON.Game.PowerUps
{
    //BOSS币
    public class patch_FaithTokenPowerup : FaithTokenPowerup
    {
        public extern void orig_PickItUp(Pickup pickup, Actor actor, bool desImmediate);

        public override void PickItUp(Pickup pickup, Actor actor, bool desImmediate = true)
        {
            if (count > 0 && count < 5) this.count *= 5; //5倍
            orig_PickItUp(pickup, actor, desImmediate);
        }
    }
}