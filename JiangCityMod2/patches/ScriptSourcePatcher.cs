using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WingMod
{
    /// <summary>
    /// 对于游戏源码的patch
    /// </summary>
    public class ScriptSourcePatcher
    {
        private String SourceText;

        public ScriptSourcePatcher(String text)
        {
            SourceText = text;
        }

        public void Patch()
        {
            FileLog.Log("ScriptSourcePatcher Patch");
            var tree = CSharpSyntaxTree.ParseText(SourceText);
            var walker = new ScriptWalker();
            walker.Visit(tree.GetRoot());
            foreach (var classDeclarationSyntax in walker.Classes)
            {
                FileLog.Log($"ScriptSourcePatcher Class: {classDeclarationSyntax.Key}");
            }
        }
    }

    class ScriptWalker : CSharpSyntaxWalker
    {
        public Dictionary<String, ClassDeclarationSyntax> Classes = new Dictionary<string, ClassDeclarationSyntax>();

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            Classes.Add(node.Identifier.Text, node);
            base.VisitClassDeclaration(node);
        }
    }
}