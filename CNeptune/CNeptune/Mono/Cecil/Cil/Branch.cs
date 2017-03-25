using System;
using System.Collections;
using System.Collections.Generic;
using Mono;
using Mono.Cecil;

namespace Mono.Cecil.Cil
{
    internal partial class Branch
    {
        static private Dictionary<MethodBody, Branch> m_Dictionary = new Dictionary<MethodBody, Branch>();

        static public Branch Query(MethodBody body)
        {
            Branch _branch;
            if (Branch.m_Dictionary.TryGetValue(body, out _branch)) { Branch.m_Dictionary.Remove(body); }
            return _branch;
        }

        public readonly MethodBody Body;
        public readonly Instruction Instruction;

        public Branch(MethodBody body, OpCode branch)
        {
            this.Body = body;
            this.Instruction = Instruction.Create(branch, Instruction.Create(OpCodes.Nop));
        }

        public IDisposable Begin()
        {
            this.Body.Add(this.Instruction);
            return new Branch.Scope(this);
        }

        public void Finialize(Instruction instruction)
        {
            this.Instruction.Operand = instruction;
        }
    }
}
