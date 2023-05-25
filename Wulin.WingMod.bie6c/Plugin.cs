using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace Wulin.WingMod.bie6c
{
    [BepInPlugin("WingMod", "WingMod", "0.0.1")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            // Plugin startup logic
            Log.LogInfo($"Plugin WingMod is loaded!");
        }
    }
}
