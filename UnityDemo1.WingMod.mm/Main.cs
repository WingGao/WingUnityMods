using System;
using System.Reflection;
using HarmonyLib;
using WingGao.Mod;

#pragma warning disable CS0626 // orig_ method is marked external and has no attributes on it.
// namespace NEON.UI.MainScreen
// {
//     // The patch_ class is in the same namespace as the original class.
//     // This can be bypassed by placing it anywhere else and using [MonoModPatch("global::Celeste.Player")]
//
//     // Visibility defaults to "internal", which hides your patch from runtime mods.
//     // If you want to "expose" new members to runtime mods, create extension methods in a public static class PlayerExt
//     class patch_MainScreenMenu : MainScreenMenu
//     {
//         // : Player lets us reuse any of its visible members without redefining them.
//         // MonoMod creates a copy of the original method, called orig_Added.
//
//         public extern void orig_Select(MainScreenMenuItem item);
//         public new void Select(MainScreenMenuItem item)
//         {
//             WingPatch.StartPatch();
//             orig_Select(item);
//         }
//         void Awake(){
//             WingPatch.StartPatch();
//         }
//     }
// }
//patch unity
namespace UnityEngine.EventSystems
{
    // The patch_ class is in the same namespace as the original class.
    // This can be bypassed by placing it anywhere else and using [MonoModPatch("global::Celeste.Player")]

    // Visibility defaults to "internal", which hides your patch from runtime mods.
    // If you want to "expose" new members to runtime mods, create extension methods in a public static class PlayerExt
    class patch_EventSystem : EventSystem
    {
        // : Player lets us reuse any of its visible members without redefining them.
        // MonoMod creates a copy of the original method, called orig_Added.

        public extern void orig_Update();
        protected override void Update()
        {
            WingPatch.StartPatch();
            orig_Update();
        }
    }
}