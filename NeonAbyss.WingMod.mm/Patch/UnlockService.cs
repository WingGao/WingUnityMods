namespace NEON.UI.UnlockSystem.Service
{
    public class patch_UnlockService : UnlockService
    {
        public extern int orig_IncBossCoins(int coins);

        public int IncBossCoins(int coins)
        {
            return orig_IncBossCoins(coins);
        }

        public extern int orig_IncAbyssCoins(int coins);

        public int IncAbyssCoins(int coins)
        {
            if (coins > 0 && coins < 5) coins *= 5; //5倍霓虹
            return orig_IncAbyssCoins(coins);
        }
    }
}