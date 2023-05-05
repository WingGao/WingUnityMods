using System;
using UnityEngine;

namespace WingUtil.UnityModManager
{
    public static class WingUnityDraw
    {
        public static void DrawButtonGroup(string labelText, string[] buttonLabels, Action<int> buttonAction)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(labelText);
            for (int i = 0; i < buttonLabels.Length; i++)
            {
                if (GUILayout.Button(buttonLabels[i]))
                {
                    buttonAction(i);
                }
            }

            GUILayout.EndHorizontal();
        }
    }
}