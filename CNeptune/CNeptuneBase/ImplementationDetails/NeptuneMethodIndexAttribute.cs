using System;
using System.ComponentModel;
using System.Diagnostics;

namespace CNeptuneBase.ImplementationDetails
{
    /// <summary>
    /// Applied by CNeptune to generic methods and methods in generic types
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class NeptuneMethodIndexAttribute : Attribute
    {
        public int NeptuneMethodIndex { get; }

        public NeptuneMethodIndexAttribute(int neptuneMethodIndex)
        {
            NeptuneMethodIndex = neptuneMethodIndex;
        }
    }
}