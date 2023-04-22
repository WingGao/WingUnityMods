using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using iFActionGame2;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using WingUtil.Harmony;

namespace WingMod
{
    [HarmonyPatch]
    public class CSharpPatch
    {
        private static string mainScript;
        private static bool IsGameScriptEmit = false;

        [HarmonyPatch(typeof(CSharpSyntaxTree))]
        [HarmonyPatch(nameof(CSharpSyntaxTree.ParseText),
            new Type[]
            {
                typeof(SourceText), typeof(CSharpParseOptions), typeof(string), typeof(ImmutableDictionary<string, ReportDiagnostic>),
                typeof(bool), typeof(CancellationToken)
            })]
        [HarmonyPrefix]
        static void CSharpSyntaxTree_ParseText(ref SourceText text)
        {
            if (String.IsNullOrEmpty(mainScript))
            {
                FileLog.Log($"CSharpSyntaxTree_ParseText Stack=> {Environment.StackTrace}");
                var stack = new StackTrace().GetFrames().First(x => x.GetMethod().FullDescription().Contains("iFActionGame2"));
                // foreach (var stackFrame in stack)
                // {
                //     FileLog.Log($"CSharpSyntaxTree_ParseText=> {stackFrame}");
                // }
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) FileLog.Log($"Assembly=> {assembly.FullName}");
                mainScript = text.ToString();
                File.WriteAllText(IVal.BasePath + "src.cs", mainScript);
                // 注入启动函数
                var startIdx = mainScript.IndexOf("public static void GameRun()");
                var nextBlock = mainScript.IndexOf("{", startIdx);
                mainScript = mainScript.Insert(nextBlock + 1, "WingSourceHarmPatcher.Patch();");
                ICompiler_Patch.LoadScripts(ref mainScript);
                // mainScript = $"using System; {mainScript}";
                text = SourceText.From(mainScript);
                // WingAccessTools.SetFieldValue(text, "_source", mainScript);
                // FileLog.Log($"CSharpSyntaxTree_ParseText:\n{mainScript}");
            }

            // FileLog.Log(text.ToString());
        }


        // MetadataReference.CreateFromFile
        [HarmonyPatch(typeof(MetadataReference))]
        [HarmonyPatch(nameof(MetadataReference.CreateFromFile),
            new[] {typeof(string), typeof(MetadataReferenceProperties), typeof(DocumentationProvider)})]
        [HarmonyPrefix]
        static void MetadataReference_CreateFromFile(ref string path)
        {
            // FileLog.Log($"MetadataReference_CreateFromFile path = {path}");
            // 空文件重定向
            if (String.IsNullOrEmpty(path)) path = IVal.BasePath + "OpenTK.Mathematics.dll";
        }

        [HarmonyPatch]
        class EmitPatch
        {
            static MethodBase TargetMethod()
            {
                return AccessTools.FirstMethod(typeof(Compilation), m => m.Name == "Emit" && m.GetParameters().Length == 12);
            }

            static void Postfix(ref EmitResult __result)
            {
                if (IsGameScriptEmit) return;
                IsGameScriptEmit = true;
                if (!__result.Success)
                {
                    foreach (var resultDiagnostic in __result.Diagnostics)
                    {
                        if (resultDiagnostic.WarningLevel == (int) DiagnosticSeverity.Error) Console.WriteLine(resultDiagnostic);
                    }
                }
            }
        }
    }

    // [HarmonyPatch(typeof(ICompiler))]
    public class ICompiler_Patch
    {
        // [HarmonyPatch(nameof(ICompiler.Compile))]
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

        public static void LoadScripts(ref string text)
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

            var usingTxt = usingMap.Keys.Select(u => "using " + u + ";").Join(null, "");
            var modTxt = scripts.Join(null, "\n");
            FileLog.Log($"usingTxt => {usingTxt}");
            FileLog.Log($"modTxt => \n{modTxt}");
            // modTxt = "";
            text = $"{usingTxt}\n{text}\n{modTxt}";
        }

        // [HarmonyPatch(nameof(ICompiler.Compile))]
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