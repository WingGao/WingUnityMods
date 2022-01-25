using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TestGameMain;
using WingUtil.Harmony;

namespace TestGameMainPatch
{
    public static class PatchUMM
    {
        public static void Patch()
        {
            Harmony.DEBUG = true;
            var harmony = new Harmony("TestGameMainPatch.PatchUMM");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Console.WriteLine("PatchUMM Patch");
        }

        [HarmonyPatch(typeof(ClassA))]
        public static class ClassAPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch("GetNamePrivate")]
            static void GetNamePrivatePost(ref String __result)
            {
                __result = "From UMM GetNamePrivatePost";
            }

            [HarmonyTranspiler]
            [HarmonyPatch("Print")]
            static IEnumerable<CodeInstruction> PrintTranspiler(IEnumerable<CodeInstruction> instructions,
                ILGenerator gen)
            {
                ILCursor c = new ILCursor(instructions);
                if (c.TryGotoNext(
                        inst => inst.Instruction.MatchLdfld("TestGameMain.ClassA::FieldClass1")
                    ))
                {
                    // 替换opcode
                    // c.Next.Next.Next.Instruction.opcode = OpCodes.Ldc_I4_0;
                    c.Index += 9; // call         instance string TestGameMain.ClassA::Method1()
                    // 替换方法调用
                    c.RemoveRange(1);
                    c.Emit(OpCodes.Call, AccessTools.Method(typeof(ClassA), "Method2"));
                    Console.WriteLine($"Path FieldClass1.FieldInt1 == 1 Line_{c.Index:X}");
                    //插入调用静态方法
                    c.TryGotoNext(MoveType.After, inst => inst.Instruction.MatchCallByName("TestGameMain.ClassA::MethodStatic1"));
                    // 有些情况下使用 callvirt
                    c.Emit(OpCodes.Call, AccessTools.Method(typeof(ClassAPatch), "MyFunction"));
                    c.Emit(OpCodes.Nop);
                }

                return c.Context.AsEnumerable();
            }

            public static void MyFunction()
            {
                Console.WriteLine("MyFunction Run");
            }
        }
    }
}