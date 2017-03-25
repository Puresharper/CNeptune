using System;
using Mono;
using Mono.Cecil;

namespace Mono.Cecil.Cil
{
    internal partial class Branch
    {
        private class Scope : IDisposable
        {
            private Branch m_Branch;

            public Scope(Branch branch)
            {
                this.m_Branch = branch;
            }

            public void Dispose()
            {
                Branch.m_Dictionary.Add(this.m_Branch.Body, this.m_Branch);
            }
        }
    }
}
