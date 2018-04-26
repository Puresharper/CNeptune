using System;

namespace CNeptune
{
    [AttributeUsage(AttributeTargets.Assembly|AttributeTargets.Class|AttributeTargets.Struct|AttributeTargets.Property|AttributeTargets.Method|AttributeTargets.Constructor, Inherited = false)]
    public class NeptuneAttribute : Attribute
    {
        public bool Managed { get; }

        /// <summary>
        /// Control if members of classes should be manged by Neptune.
        /// NOT inherented from base but from outer scope (like generic parameters).
        /// Usage:
        /// <list type="table">
        /// <item>
        ///   <term>Assembly</term>
        ///   <description>Default on for all classes, can be turned off </description>
        /// </item>
        /// <item>
        ///   <term>class, struct</term>
        ///   <description>Change default setting for members</description>
        /// </item>
        /// <item>
        ///   <term>Property, method</term>
        ///   <description>Change setting for specific member</description>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="managed"></param>
        public NeptuneAttribute(bool managed)
        {
            Managed = managed;
        }
    }
}