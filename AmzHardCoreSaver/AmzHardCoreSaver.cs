using FairyGUI;
using HarmonyLib;
using XiaWorld;

namespace AmzHardCoreSaver
{
    [HarmonyPatch(typeof(Wnd_GameMain), "ShowMainMenu")]
    internal static class AmzHardCoreSaver
    {
        private static int oldLength = 0;

        static void Prefix(Wnd_GameMain __instance, ref PopupMenu ___MainMenu)
        {
            KLog.Dbg("[AmzHardCoreSaver] Prefix");
            if (___MainMenu != null && World.Instance != null)
            {
                if (oldLength == 0) oldLength = ___MainMenu._list.numItems;
                if (___MainMenu._list.numItems <= oldLength)
                {
                    if (World.Instance.GameMode == g_emGameMode.HardCore)
                    {
                        KLog.Dbg("[AmzHardCoreSaver] AddMenu");
                        // 替换为存档
                        ___MainMenu.AddItem(TFMgr.Get("存档"), (EventCallback0) (() => Wnd_Save.Instance.ShowSaveWnd(0)));

                        // 替换为读档
                        ___MainMenu.AddItem(TFMgr.Get("读档"), (EventCallback0) (() => Wnd_Save.Instance.ShowSaveWnd(1)));
                    }
                }
            }
        }

        static void replaceMenu(PopupMenu ___MainMenu)
        {
            KLog.Dbg("[AmzHardCoreSaver] add buttons v1");
            var i = 0;
            var t1 = -1;
            var t2 = -1;
            foreach (var gObject in ___MainMenu._list.GetChildren())
            {
                if (gObject.asButton.title == TFMgr.Get("存档并返回"))
                {
                    t1 = i;
                }
                else if (gObject.asButton.title == TFMgr.Get("存档并退出"))
                {
                    t2 = i;
                }

                i++;
            }

            if (t1 >= 0)
            {
                var name = ___MainMenu.GetItemName(t1);
                KLog.Dbg("[AmzHardCoreSaver] remove {0} {1}", t1, name);
                if (___MainMenu.RemoveItem(name))
                {
                    ___MainMenu.AddItem(TFMgr.Get("存档"),
                        (EventCallback0) (() => Wnd_Save.Instance.ShowSaveWnd(0)));
                }
            }
            else
            {
                // ___MainMenu.AddItem(TFMgr.Get("存档"),
                //     (EventCallback0) (() => Wnd_Save.Instance.ShowSaveWnd(0)));
            }

            if (t2 >= 0)
            {
                if (___MainMenu.RemoveItem(___MainMenu.GetItemName(t2)))
                {
                    ___MainMenu.AddItem(TFMgr.Get("读档"),
                        (EventCallback0) (() => Wnd_Save.Instance.ShowSaveWnd(1)));
                }
            }
            else
            {
                // ___MainMenu.AddItem(TFMgr.Get("读档"),
                //     (EventCallback0) (() => Wnd_Save.Instance.ShowSaveWnd(1)));
            }
        }

        static void Postfix(Wnd_GameMain __instance, ref PopupMenu ___MainMenu)
        {
        }
    }
}