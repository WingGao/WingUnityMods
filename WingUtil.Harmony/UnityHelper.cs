﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace WingUtil
{
    public static class UnityHelper
    {
        public static String Version = "2022年1月25日22:53:42";

        /// <summary>
        /// 获取MonoBehaviour对象包含的所有Components
        /// </summary>
        /// <param name="mbe"></param>
        /// <returns></returns>
        public static String FmtAllComponents(MonoBehaviour mbe)
        {
            return String.Join(";", mbe.GetComponents(typeof(Component))
                .Select(v => v.GetType().FullName).Where(v => !v.StartsWith("UnityEngine.")));
        }


        public class ToggleGroupItem
        {
            public readonly String name;
            public readonly System.Object value;

            public ToggleGroupItem(String name, System.Object value)
            {
                this.name = name;
                this.value = value;
            }

            public override string ToString()
            {
                return $"ToggleGroupItem(name={name},value={value})";
            }
        }

        /// <summary>
        /// 构建UMM的PopupToggleGroup选项
        /// </summary>
        public static bool DrawPopupToggleGroup(ref System.Object selected, String fieldName, List<ToggleGroupItem> values)
        {
            var changed = false;
            GUILayout.BeginHorizontal();
            GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
            GUILayout.Space(5);
            var selectedVal = selected;
            //TODO 性能优化
            var valIdx = values.FindIndex(v => v.value.Equals(selectedVal));
            if (valIdx < 0) valIdx = 0;
            // UnityModManager.Logger.Log($"valIdx={valIdx}, selectedVal={selectedVal} unique={values.GetHashCode()}");
            var options = new List<GUILayoutOption>();
            options.Add(GUILayout.ExpandWidth(false));
            if (UnityModManager.UI.PopupToggleGroup(ref valIdx, values.Select(v => v.name).ToArray(), fieldName, values.GetHashCode(),
                    null, options.ToArray()))
            {
                selected = values[valIdx].value;
                changed = true;
                // UnityModManager.Logger.Log($"PopupToggleGroup change valIdx={valIdx}, selected={selected} ");
            }

            GUILayout.EndHorizontal();
            return changed;
        }

        public static void DrawText(String label, System.Object value = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            var sk = new GUIStyle(GUI.skin.label);
            sk.alignment = TextAnchor.UpperRight; //右对齐
            if (value != null)
            {
                var t = "";
                if (value is float)
                {
                    t = $"{value:F2}";
                }
                else
                {
                    t = value.ToString();
                }


                GUILayout.Label(t, sk);
            }

            GUILayout.EndHorizontal();
        }
    }
}