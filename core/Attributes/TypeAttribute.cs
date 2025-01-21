using System;

namespace Types
{
    /// <summary>
    /// Declares that the type will have a generated <see cref="TypeLayout"/> available.
    /// <para>
    /// This attribute will be removed in the future.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class TypeAttribute : Attribute
    {
    }
}