using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace WingUtil.Harmony
{
    public class ILContext
    {
        public List<ILInstruction> Instructions;
        public List<ILLabel> Labels = new List<ILLabel>();

        public ILContext(IEnumerable<CodeInstruction> cis)
        {
            ILInstruction prev = null;
            Instructions = new List<ILInstruction>();
            foreach (var codeInstruction in cis)
            {
                var inst = new ILInstruction();
                inst.Instruction = codeInstruction;
                inst.Previous = prev;
                Instructions.Add(inst);
                if (prev != null)
                {
                    prev.Next = inst;
                }

                prev = inst;
            }
        }

        public int IndexOf(ILInstruction instr)
        {
            return Instructions.IndexOf(instr);
        }

        public ILInstruction Insert(int idx, CodeInstruction ci)
        {
            var inst = new ILInstruction();
            inst.Instruction = ci;
            //TODO 边界检查
            var prev = Instructions[idx - 1];
            var next = Instructions[idx];
            if (prev != null)
            {
                prev.Next = inst;
                inst.Previous = prev;
            }

            if (next != null)
            {
                next.Previous = inst;
                inst.Next = next;
            }

            Instructions.Insert(idx, inst);
            return inst;
        }

        public IEnumerable<CodeInstruction> AsEnumerable()
        {
            return Instructions.Select(x => x.Instruction);
        }

        /// <summary>
        /// Obtain all labels pointing at the given instruction.
        /// </summary>
        /// <param name="instr">The instruction to get all labels for.</param>
        /// <returns>All labels targeting the given instruction.</returns>
        public IEnumerable<ILLabel> GetIncomingLabels(ILInstruction instr)
            => Labels.Where(l => l.Target == instr);
    }
}