using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using iFActionGame2;
using Microsoft.CodeAnalysis.CSharp;
using WingUtil.Harmony;

namespace WingMod
{
    [HarmonyPatch(typeof(ICompiler))]
    public class ICompiler_Patch
    {
        [HarmonyPatch(nameof(ICompiler.Compile))]
        [HarmonyPrefix]
        static void Compile_Prefix(ref string text, string aName)
        {
            if (aName != "iFActionGameScript") return;
            var mySource = File.ReadAllLines(IVal.BasePath + "WingModSourcePatcher.cs");
            var skipUsing = 0;
            for (var i = 0; i < mySource.Length; i++)
            {
                if (mySource[i].StartsWith("namespace"))
                {
                    skipUsing = i;
                    break;
                }
            }

            text = $"using HarmonyLib;using OpenTK.Windowing.GraphicsLibraryFramework;using WingUtil.Harmony;using System.Reflection.Emit;" +
                   $"\n{text}\n{String.Join("\n", mySource.Skip(skipUsing))}";
            // var patcher = new ScriptSourcePatcher(text);
            // patcher.Patch();
        }

        [HarmonyPatch(nameof(ICompiler.Compile))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Compile_Patch(IEnumerable<CodeInstruction> instructions)
        {
            // 将 AppDomain.CurrentDomain.GetAssemblies() =》 GetAssemblies
            ILCursor cursor = new ILCursor(instructions);
            if (cursor.TryGotoNext(it => it.Instruction.MatchCallByName("System.AppDomain::get_CurrentDomain")))
            {
                // FileLog.Log($"ICompiler.Compile System.AppDomain::GetAssemblies => Line_{cursor.Index} {cursor.Next}");
                cursor.RemoveRange(2);
                cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(ICompiler_Patch), nameof(GetAssemblies)));
            }

            if (cursor.TryGotoNext(it => it.Instruction.MatchCallByName("Microsoft.CodeAnalysis.CSharp.CSharpCompilation::AddReferences")))
            {
                cursor.Index += 2; //跳过pop
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.Emit(OpCodes.Ldloc_S, 4);
                cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(ICompiler_Patch), nameof(PatchCompilation)));
            }

            return cursor.Context.AsEnumerable();
        }

        private static IEnumerable<Assembly> GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(x => !String.IsNullOrEmpty(x.Location));
        }

        private static void PatchCompilation(String name, CSharpCompilation compilation)
        {
            FileLog.Log($"PatchCompilation {name} {compilation.GlobalNamespace}");
        }
    }
}