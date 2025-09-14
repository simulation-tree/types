using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Types.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class MetadataBankGenerator : IIncrementalGenerator
    {
        public const string TypeNameFormat = "{0}MetadataBank";
        public const string FieldBufferVariableName = "fields";
        public const string InterfaceBufferVariableName = "interfaces";

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ITypeSymbol?> types = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);
            context.RegisterSourceOutput(types.Collect(), Generate);
        }

        private void Generate(SourceProductionContext context, ImmutableArray<ITypeSymbol?> typesArray)
        {
            List<ITypeSymbol> types = new();
            foreach (ITypeSymbol? type in typesArray)
            {
                if (type is not null)
                {
                    types.Add(type);
                }
            }

            if (types.Count > 0)
            {
                string source = Generate(types, out string typeName);
                context.AddSource($"{typeName}.generated.cs", source);
            }
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

        public static string Generate(IReadOnlyList<ITypeSymbol> types, out string typeName)
        {
            string? assemblyName = types[0].ContainingAssembly?.Name;
            if (assemblyName is not null && assemblyName.EndsWith(".Core"))
            {
                assemblyName = assemblyName.Substring(0, assemblyName.Length - 5);
            }

            SourceBuilder source = new();
            source.Append("using ");
            source.Append(Constants.Namespace);
            source.Append(';');
            source.AppendLine();

            source.Append("using ");
            source.Append(Constants.Namespace);
            source.Append('.');
            source.Append("Functions");
            source.Append(';');
            source.AppendLine();

            source.AppendLine("using System;");
            source.AppendLine();

            if (assemblyName is not null)
            {
                source.Append("namespace ");
                source.AppendLine(assemblyName);
                source.BeginGroup();
            }

            source.AppendLine("/// <summary>");
            source.AppendLine("/// Contains all types declared by this project.");
            source.AppendLine("/// </summary>");

            typeName = TypeNameFormat.Replace("{0}", assemblyName ?? "");
            typeName = typeName.Replace(".", "");
            source.Append("public readonly struct ");
            source.Append(typeName);
            source.Append(" : ");
            source.Append(Constants.MetadataBankTypeName);
            source.AppendLine();

            source.BeginGroup();
            {
                source.Append("readonly void ");
                source.Append(Constants.MetadataBankTypeName);
                source.Append(".Load(");
                source.Append(Constants.RegisterFunctionTypeName);
                source.Append(" register)");
                source.AppendLine();

                source.BeginGroup();
                {
                    int fieldCount = 0;
                    int interfaceCount = 0;
                    int index = source.Length;

                    //register all interfaces first
                    HashSet<ITypeSymbol> interfaceTypes = [];
                    foreach (ITypeSymbol type in types)
                    {
                        foreach (INamedTypeSymbol interfaceType in type.AllInterfaces)
                        {
                            interfaceTypes.Add(interfaceType);
                        }
                    }

                    foreach (ITypeSymbol interfaceType in interfaceTypes)
                    {
                        AppendRegisterInterface(source, interfaceType);
                    }

                    foreach (ITypeSymbol type in types)
                    {
                        (int fieldCount, int interfaceCount) x = AppendRegisterType(source, type);
                        fieldCount += x.fieldCount;
                        interfaceCount += x.interfaceCount;
                    }

                    if (fieldCount > 0)
                    {
                        source.InsertAt(index, "\r\n");
                        source.InsertAt(index, " = new();");
                        source.InsertAt(index, FieldBufferVariableName);
                        source.InsertAt(index, ' ');
                        source.InsertAt(index, Constants.FieldBufferTypeName);
                        source.InsertIndentationAt(index);
                    }

                    if (interfaceCount > 0)
                    {
                        source.InsertAt(index, "\r\n");
                        source.InsertAt(index, " = new();");
                        source.InsertAt(index, InterfaceBufferVariableName);
                        source.InsertAt(index, ' ');
                        source.InsertAt(index, Constants.InterfaceBufferTypeName);
                        source.InsertIndentationAt(index);
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

        private static void AppendRegisterInterface(SourceBuilder source, ITypeSymbol interfaceType)
        {
            bool hasStaticAbstractMembers = false;
            foreach (ISymbol method in interfaceType.GetMembers())
            {
                if (method.IsStatic)
                {
                    hasStaticAbstractMembers = true;
                    break;
                }
            }

            string fullName = interfaceType.GetFullTypeName();
            source.Append("if (!");
            source.Append(Constants.RegistryTypeName);
            source.Append(".IsInterfaceRegistered");
            if (hasStaticAbstractMembers)
            {
                source.Append('(');
                source.Append('"');
                source.Append(fullName);
                source.Append('"');
                source.Append(')');
            }
            else
            {
                source.Append('<');
                source.Append(fullName);
                source.Append(">()");
            }

            source.Append(')');
            source.AppendLine();
            source.BeginGroup();
            {
                source.Append("register.RegisterInterface");

                if (hasStaticAbstractMembers)
                {
                    source.Append('(');
                    source.Append('"');
                    source.Append(fullName);
                    source.Append('"');
                    source.Append(',');
                    source.Append(' ');
                    source.Append("RuntimeTypeTable.GetHandle(typeof(");
                    source.Append(fullName);
                    source.Append("))");
                    source.Append(')');
                }
                else
                {
                    source.Append('<');
                    source.Append(fullName);
                    source.Append('>');
                    source.Append('(');
                    source.Append(')');
                }

                source.Append(';');
                source.AppendLine();
            }
            source.EndGroup();
        }

        private static (int fieldCount, int interfaceCount) AppendRegisterType(SourceBuilder source, ITypeSymbol type)
        {
            string fullName = type.GetFullTypeName();
            if (fullName.EndsWith("e__FixedBuffer"))
            {
                return default;
            }

            source.Append("if (!");
            source.Append(Constants.RegistryTypeName);
            source.Append(".IsTypeRegistered<");
            source.Append(fullName);
            source.Append(">())");
            source.AppendLine();

            int fieldCount = 0;
            int interfaceCount = 0;
            source.BeginGroup();
            {
                HashSet<string> fieldNames = new();
                foreach (IFieldSymbol field in type.GetFields())
                {
                    if (fieldNames.Add(field.Name))
                    {
                        AppendField(source, field, ref fieldCount);
                    }
                }

                foreach (INamedTypeSymbol interfaceType in type.AllInterfaces)
                {
                    AppendInterface(source, interfaceType, ref interfaceCount);
                }

                source.Append("register.RegisterType<");
                source.Append(fullName);
                source.Append(">(");
                if (fieldCount > 0 || interfaceCount > 0)
                {
                    if (fieldCount > 0)
                    {
                        source.Append(FieldBufferVariableName);
                        source.Append(',');
                        source.Append(' ');
                        source.Append(fieldCount);
                    }
                    else
                    {
                        source.Append("default, 0");
                    }

                    source.Append(',');
                    source.Append(' ');

                    if (interfaceCount > 0)
                    {
                        source.Append(InterfaceBufferVariableName);
                        source.Append(',');
                        source.Append(' ');
                        source.Append(interfaceCount);
                    }
                    else
                    {
                        source.Append("default, 0");
                    }
                }

                source.Append(");");
                source.AppendLine();
            }
            source.EndGroup();
            return (fieldCount, interfaceCount);

            static void AppendField(SourceBuilder source, IFieldSymbol field, ref int count)
            {
                source.Append(FieldBufferVariableName);
                source.Append('[');
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

            static void AppendInterface(SourceBuilder source, INamedTypeSymbol interfaceType, ref int count)
            {
                source.Append(InterfaceBufferVariableName);
                source.Append('[');
                source.Append(count);
                source.Append("] = new(\"");
                source.Append(interfaceType.GetFullTypeName());
                source.Append("\");");
                source.AppendLine();
                count++;
            }
        }
    }
}