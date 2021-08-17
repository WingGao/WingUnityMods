using System;
using System.Reflection;
using HarmonyLib;
using MonoMod.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WingUtil.Harmony
{
     public static partial class Extensions
     {
          // public static bool MatchCallvirt(this CodeInstruction instr, string typeFullName, string name)
          //      => instr.MatchCallvirt(out var v) && v.Is(typeFullName, name);
          // public static bool MatchCallvirt(this CodeInstruction instr, out MethodReference value)
          //      => instr.MatchCallvirt(out var v) && v.Is(typeFullName, name);
     }
}