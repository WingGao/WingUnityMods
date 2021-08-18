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
    }
}