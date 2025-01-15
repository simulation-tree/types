using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Types.Generator
{
    public class InheritingType : InputType
    {
        public readonly bool isReadOnly;
        public readonly IReadOnlyList<ITypeSymbol> inheritedTypes;

        public InheritingType(string name, string fullTypeName, string? containedNamespace, bool isReadOnly, IReadOnlyList<ITypeSymbol> inheritedTypes) :
            base(name, fullTypeName, containedNamespace)
        {
            this.isReadOnly = isReadOnly;
            this.inheritedTypes = inheritedTypes;
        }
    }
}