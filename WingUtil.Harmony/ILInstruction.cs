using System.Reflection.Emit;
using HarmonyLib;

namespace WingUtil.Harmony
{
    public class ILInstruction
    {
        public CodeInstruction Instruction;
        public ILInstruction Next;
        public ILInstruction Previous;

        public override string ToString()
        {
            return "ILInstruction => " + Instruction.ToString();
        }

        public void Nop()
        {
            Instruction.opcode = OpCodes.Nop;
            Instruction.operand = null;
        }
    }
}