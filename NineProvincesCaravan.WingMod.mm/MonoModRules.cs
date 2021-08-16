using Mono.Cecil;
using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.InlineRT;
using MonoMod.Utils;

namespace MonoMod
{
    // 测试IL，修改版本显示
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchDlgSettings))]
    class PatchDlgSettingsAttribute : Attribute
    {
    }

    static class MonoModRules
    {
        public static void PatchDlgSettings(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);

            // FieldReference f_Entity_Collidable = MonoModRule.Modder.Module.GetType("Monocle.Entity").FindField("Collidable");
            MethodReference m_Concat = MonoModRule.Modder.Module.GetType("System.String").FindMethod("System.String Concat(System.String,System.String)");

            // MethodDefinition m_ModCardTexture = context.Method.DeclaringType.FindMethod("string DlgSettings/VersionData::get_Ver()");
            cursor.GotoNext(MoveType.AfterLabel,
                instr => instr.MatchCallvirt("Framework.UICLabel","set_text"));

            Instruction target = cursor.Next;
            // 去除hot的其他判定
            // add if (!Collidable) { DisableStaticMovers(); } before UpdateVisualState()
            cursor.Emit(OpCodes.Ldstr, "wing");
            cursor.Emit(OpCodes.Call, m_Concat);
        }
    }
}