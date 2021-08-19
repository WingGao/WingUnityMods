﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MonoMod.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WingUtil.Harmony
{
    public static partial class Extensions
    {
        public static string FormatArgument(this CodeInstruction instr, string extra = null)
        {
            var argument = instr.operand;
            if (argument == null)
                return "NULL";
            Type type = argument.GetType();
            if (argument is MethodBase member)
                return member.FullDescription() + (extra != null ? " " + extra : "");
            if (argument is FieldInfo fieldInfo)
                return fieldInfo.FieldType.FullDescription() + " " + fieldInfo.DeclaringType.FullDescription() + "::" + fieldInfo.Name;
            if (type == typeof(Label))
                return string.Format("Label{0}", (object)((Label)argument).GetHashCode());
            if (type == typeof(Label[]))
                return "Labels" + string.Join(",",
                    ((IEnumerable<Label>)(Label[])argument)
                    .Select<Label, string>((Func<Label, string>)(l => l.GetHashCode().ToString())).ToArray<string>());
            if (type == typeof(LocalBuilder))
                return string.Format("{0} ({1})", (object)((LocalVariableInfo)argument).LocalIndex,
                    (object)((LocalVariableInfo)argument).LocalType);
            return type == typeof(string) ? argument.ToString().ToLiteral() : argument.ToString().Trim();
        }


        public static bool MatchCall(this CodeInstruction instr, out MethodBase value)
        {
            if (instr.opcode == OpCodes.Call || instr.opcode == OpCodes.Callvirt)
            {
                value = instr.operand as MethodBase;
                return true;
            }

            value = default;
            return false;
        }

        /// 只匹配名字
        /// callvirt System.Void Framework.UICLabel::set_text(System.String value) ==> Framework.UICLabel::set_text
        /// </summary>
        /// <param name="instr"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool MatchCallByName(this CodeInstruction instr, string name)
        {
            if (instr.MatchCall(out var member))
            {
                var mName = (member.DeclaringType != null ? member.DeclaringType.FullDescription() + "::" : "")
                            + member.Name;
                return mName == name;
            }

            return false;
        }

        public static bool MatchCallvirt(this CodeInstruction instr, out MethodBase value)
        {
            if (instr.opcode == OpCodes.Callvirt)
            {
                value = instr.operand as MethodBase;
                return true;
            }

            value = default;
            return false;
        }
    }
}