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
            LoadScripts(ref text);
            // var mySource = File.ReadAllLines(IVal.BasePath + "WingModSourcePatcher.cs");
            // var skipUsing = 0;
            // for (var i = 0; i < mySource.Length; i++)
            // {
            //     if (mySource[i].StartsWith("namespace"))
            //     {
            //         skipUsing = i;
            //         break;
            //     }
            // }
            //
            // text = $"using HarmonyLib;using OpenTK.Windowing.GraphicsLibraryFramework;using WingUtil.Harmony;using System.Reflection.Emit;" +
            //        $"\n{text}\n{String.Join("\n", mySource.Skip(skipUsing))}";
            
            // var patcher = new ScriptSourcePatcher(text);
            // patcher.Patch();
        }

        static void LoadScripts(ref string text)
        {
            var usingMap = new Dictionary<String, int>();
            var scripts = new List<String>();
            //获取目录下的所有cs
            foreach (var file in Directory.GetFiles(IVal.BasePath + "WingMod", "*.cs"))
            {
                var scriptLines = File.ReadAllLines(file);
                var skipUsing = 0;
                for (var i = 0; i < scriptLines.Length; i++)
                {
                    if (scriptLines[i].StartsWith("using")) //优化using
                    {
                        var usingName = scriptLines[i].Split(";")[0].Split(" ").Last();
                        usingMap.TryAdd(usingName, 1);
                    }
                    else if (scriptLines[i].StartsWith("// WingModScript"))
                    {
                        scripts.Add(scriptLines.Skip(i).Join(null, "\n"));
                        break;
                    }
                }
            }

            var usingTxt = usingMap.Keys.Select(u => "using " + u + ";").Join(null,"");
            var modTxt = scripts.Join(null, "\n");
            FileLog.Log($"usingTxt => {usingTxt}");
            FileLog.Log($"modTxt => \n{modTxt}");
            text = $"{usingTxt}\n{text}\n{modTxt}";
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