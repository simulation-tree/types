using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Types.Generator
{
    public class InputType
    {
        public readonly SyntaxTree syntaxTree;
        public readonly ITypeSymbol typeSymbol;
        public readonly string fullTypeName;
        public readonly IReadOnlyList<ITypeSymbol> inheritedTypes;

        public InputType(SyntaxTree syntaxTree, ITypeSymbol symbol, string fullTypeName, IReadOnlyList<ITypeSymbol> inheritedTypes)
        {
            this.inheritedTypes = inheritedTypes;
            this.syntaxTree = syntaxTree;
            this.typeSymbol = symbol;
            this.fullTypeName = fullTypeName;
        }
    }
}