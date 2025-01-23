using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Types.Generator
{
    public class InputType
    {
        public readonly SyntaxTree syntaxTree;
        public readonly string typeName;
        public readonly string fullTypeName;
        public readonly string? containedNamespace;
        public readonly bool isReadOnly;
        public readonly IReadOnlyList<ITypeSymbol> inheritedTypes;

        public InputType(SyntaxTree syntaxTree, string name, string fullTypeName, string? containedNamespace, bool isReadOnly, IReadOnlyList<ITypeSymbol> inheritedTypes)
        {
            this.isReadOnly = isReadOnly;
            this.inheritedTypes = inheritedTypes;
            this.syntaxTree = syntaxTree;
            this.typeName = name;
            this.fullTypeName = fullTypeName;
            this.containedNamespace = containedNamespace;
        }
    }
}