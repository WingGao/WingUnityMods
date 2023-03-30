using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UniverseLib.UI;
using UniverseLib.UI.Panels;
using Wish;
using Logger = BepInEx.Logging.Logger;
using Object = UnityEngine.Object;

namespace SunHaven.WingMod.bie5
{
    public class MyUI : PanelBase
    {
        public static MyUI Instance { get; internal set; }
        internal static UIBase uiBase = null;
        public override string Name => $"{MyPlugin.NAME} v{MyPlugin.VERSION}";
        public override int MinWidth => 500;
        public override int MinHeight => 200;
        public override Vector2 DefaultAnchorMin => new(0.2f, 0.02f);
        public override Vector2 DefaultAnchorMax => new(0.8f, 0.5f);

        public MyUI(UIBase owner) : base(owner)
        {
            Instance = this;
        }

        void Log(String s)
        {
            MyPlugin.Log(s);
        }

        internal static void Init()
        {
            uiBase = UniversalUI.RegisterUI(MyPlugin.GUID, null);
            new MyUI(uiBase);
            // Force refresh of anchors etc
            // Canvas.ForceUpdateCanvases();
        }

        public override void SetActive(bool active)
        {
            UniversalUI.SetUIActive(MyPlugin.GUID, active);
            base.SetActive(active);
        }

        protected override void ConstructPanelContent()
        {
            GameObject mainGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "Main", true, true, true, true, 2, default, new Color(0.08f, 0.08f, 0.08f));
            UIFactory.SetLayoutElement(mainGroup, null, 100);
            GameObject cateGroup = UIFactory.CreateVerticalGroup(mainGroup, "Category", true, false, true, true, 2);
            var teleportPanel = CreateTeleportPanel();
            var btnTeleport = UIFactory.CreateButton(cateGroup, "传送", "传送");
            UIFactory.SetLayoutElement(btnTeleport.GameObject, 5, 30, 1);
            btnTeleport.OnClick += () => teleportPanel.SetActive(!teleportPanel.activeSelf);
        }

        GameObject CreateTeleportPanel()
        {
            var panel = UIFactory.CreateHorizontalGroup(ContentRoot, "TeleportPanel", true, false, true, true, 1);
            UIFactory.SetLayoutElement(panel, 100, 100, 1);
            // SortedDictionary<String, Vector2> places = new SortedDictionary<string, Vector2>()
            // {
            //     {"2playerfarm", new Vector2(366.6284f, 124.0218f)},
            // };
            // places.Add();
            List<String> places = new List<string>() {"2playerfarm", "NelvariFarm", "Quarry", "Tier1Barn0", "Tier1Barn1", "Tier1Barn2", "Tier1Barn3"};
            foreach (var place in places)
            {
                var btn = UIFactory.CreateButton(panel, place, place);
                UIFactory.SetLayoutElement(btn.GameObject, 50, 30, 1);
                btn.OnClick += () =>
                {
                    AccessTools.Method(typeof(QuantumConsoleManager), "teleport")
                        .Invoke(Object.FindObjectOfType<QuantumConsoleManager>(), new object[] {place});
                };
            }

            // panel.SetActive(false);
            return panel;
        }
    }
}