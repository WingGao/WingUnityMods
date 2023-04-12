using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using iFActionGame2;
using WingUtil.Harmony;

namespace WingMod
{
    [HarmonyPatch(typeof(ICompiler))]
    public class ICompiler_Patch
    {
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

            return cursor.Context.AsEnumerable();
        }

        private static IEnumerable<Assembly> GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(x => !String.IsNullOrEmpty(x.Location));
        }
    }
}