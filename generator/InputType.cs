namespace Types.Generator
{
    public class InputType
    {
        public readonly string typeName;
        public readonly string fullTypeName;
        public readonly string? containedNamespace;
        public string? comment;

        public InputType(string name, string fullTypeName, string? containedNamespace)
        {
            this.typeName = name;
            this.fullTypeName = fullTypeName;
            this.containedNamespace = containedNamespace;
        }
    }
}