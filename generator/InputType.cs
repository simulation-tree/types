using Microsoft.CodeAnalysis;

namespace Types.Generator
{
    public class InputType
    {
        public readonly SyntaxTree syntaxTree;
        public readonly string typeName;
        public readonly string fullTypeName;
        public readonly string? containedNamespace;
        public string? comment;

        public InputType(SyntaxTree syntaxTree, string name, string fullTypeName, string? containedNamespace)
        {
            this.syntaxTree = syntaxTree;
            this.typeName = name;
            this.fullTypeName = fullTypeName;
            this.containedNamespace = containedNamespace;
        }
    }
}