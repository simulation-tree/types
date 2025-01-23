using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Types.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class TypeBankGenerator : IIncrementalGenerator
    {
        private static readonly SourceBuilder source = new();
        public const string TypeName = "TypeBank";

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ITypeSymbol?> types = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);
            IncrementalValueProvider<Compilation> compilation = context.CompilationProvider;
            context.RegisterSourceOutput(types.Collect().Combine(compilation), Generate);
        }

        private void Generate(SourceProductionContext context, (ImmutableArray<ITypeSymbol?> types, Compilation compilation) input)
        {
            string sourceCode = Generate(input.types, input.compilation);
            context.AddSource($"{TypeName}.generated.cs", sourceCode);
        }

        private static bool Predicate(SyntaxNode node, CancellationToken token)
        {
            return node.IsKind(SyntaxKind.StructDeclaration);
        }

        private static ITypeSymbol? Transform(GeneratorSyntaxContext context, CancellationToken token)
        {
            StructDeclarationSyntax node = (StructDeclarationSyntax)context.Node;
            ITypeSymbol? type = context.SemanticModel.GetDeclaredSymbol(node);
            if (type is null)
            {
                return null;
            }

            if (type is INamedTypeSymbol namedType)
            {
                if (namedType.IsGenericType)
                {
                    return null;
                }
            }

            if (type.IsRefLikeType)
            {
                return null;
            }

            if (type.DeclaredAccessibility != Accessibility.Public && type.DeclaredAccessibility != Accessibility.Internal)
            {
                return null;
            }

            if (type.IsUnmanaged())
            {
                return type;
            }

            return null;
        }

        public static string Generate(ImmutableArray<ITypeSymbol?> types, Compilation compilation)
        {
            string? assemblyName = compilation.AssemblyName;
            source.Clear();
            source.AppendLine("using Types;");
            source.AppendLine("using Types.Functions;");
            source.AppendLine("using Unmanaged;");
            source.AppendLine();

            if (assemblyName is not null)
            {
                source.Append("namespace ");
                source.AppendLine(assemblyName);
                source.BeginGroup();
            }

            source.Append("public readonly struct ");
            source.Append(TypeName);
            source.Append(" : ITypeBank");
            source.AppendLine();

            source.BeginGroup();
            {
                source.AppendLine("void ITypeBank.Load(Register register)");
                source.BeginGroup();
                {
                    source.AppendLine("USpan<TypeLayout.Variable> buffer = stackalloc TypeLayout.Variable[(int)TypeLayout.Capacity];");
                    foreach (ITypeSymbol? type in types)
                    {
                        if (type is not null)
                        {
                            AppendRegister(type);
                        }
                    }
                }
                source.EndGroup();
            }
            source.EndGroup();

            if (assemblyName is not null)
            {
                source.EndGroup();
            }

            return source.ToString();
        }

        private static void AppendRegister(ITypeSymbol type)
        {
            string fullName = type.GetFullTypeName();
            if (fullName.EndsWith("e__FixedBuffer"))
            {
                return;
            }

            byte count = 0;
            HashSet<string> fieldNames = new();
            AppendInheritedFields(type, fieldNames, ref count);

            foreach (IFieldSymbol field in type.GetFields())
            {
                if (fieldNames.Add(field.Name))
                {
                    AppendVariable(field, ref count);
                }
            }

            source.Append("register.Invoke<");
            source.Append(fullName);
            source.Append(">(");
            if (count > 0)
            {
                source.Append("buffer.Slice(0, ");
                source.Append(count);
                source.Append(')');
            }

            source.Append(");");
            source.AppendLine();

            static void AppendVariable(IFieldSymbol field, ref byte count)
            {
                source.Append("buffer[");
                source.Append(count);
                source.Append("] = new(\"");
                source.Append(field.Name);
                source.Append("\", \"");
                if (field.IsFixedSizeBuffer)
                {
                    string variableTypeName = field.Type.GetFullTypeName().TrimEnd('*');
                    source.Append(variableTypeName);
                    source.Append('[');
                    source.Append(field.FixedSize);
                    source.Append(']');
                }
                else
                {
                    source.Append(field.Type.GetFullTypeName());
                }

                source.Append("\");");
                source.AppendLine();
                count++;
            }

            static void AppendInheritedFields(ITypeSymbol type, HashSet<string> fieldNames, ref byte count)
            {
                foreach (ITypeSymbol inheritedType in type.GetInheritingTypes())
                {
                    AppendInheritedFields(inheritedType, fieldNames, ref count);

                    foreach (IFieldSymbol field in inheritedType.GetFields())
                    {
                        if (fieldNames.Add(field.Name))
                        {
                            AppendVariable(field, ref count);
                        }
                    }
                }
            }
        }
    }
}