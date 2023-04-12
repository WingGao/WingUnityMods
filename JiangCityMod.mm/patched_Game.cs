using OpenTK.Windowing.Desktop;

#pragma warning disable CS0626
namespace iFActionGame2
{
    public class patched_Game : Game
    {
        public patched_Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        extern void orig_loadScript();

        void loadScript()
        {
            
        }
    }
}