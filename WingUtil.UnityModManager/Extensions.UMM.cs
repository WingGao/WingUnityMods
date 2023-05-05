using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityModManagerNet;

namespace WingUtil.UnityModManager
{
    public static partial class Extensions
    {
        public static String FmtString(this KeyBinding key)
        {
            var modifiersValue = new byte[] {1, 2, 4};
            var modifiersStr = new[] {"Ctrl", "Shift", "Alt"};
            var modifiers = key.modifiers;
            var keyNames = new List<String>();
            for (var i = 0; i < modifiersValue.Length; i++)
                if ((modifiers & modifiersValue[i]) != 0)
                    keyNames.Add(modifiersStr[i]);
            keyNames.Add(Enum.GetName(typeof(KeyCode), key.keyCode));
            return String.Join("+", keyNames);
        }
    }
}