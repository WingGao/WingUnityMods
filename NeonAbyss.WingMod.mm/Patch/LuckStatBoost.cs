using MonoMod;

namespace NEON.Game.PowerUps
{
    public class patch_LuckStatBoost:LuckStatBoost
    {
        // 永远幸运
        [MonoModReplace]
        public virtual bool IsLuck(float change)
        {
            return true;
        }
    }
}