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
        public const string TypeNameFormat = "{0}TypeBank";
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
            source.AppendLine("using Types;");
            source.AppendLine("using Types.Functions;");
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
            source.Append(" : ITypeBank");
            source.AppendLine();

            source.BeginGroup();
            {
                source.Append("readonly void ITypeBank.Load(");
                source.Append(Constants.RegisterFunctionTypeName);
                source.Append(" register)");
                source.AppendLine();

                source.BeginGroup();
                {
                    source.Append(Constants.FieldBufferTypeName);
                    source.Append(' ');
                    source.Append(FieldBufferVariableName);
                    source.Append(" = new();");
                    source.AppendLine();

                    source.Append(Constants.InterfaceBufferTypeName);
                    source.Append(' ');
                    source.Append(InterfaceBufferVariableName);
                    source.Append(" = new();");
                    source.AppendLine();

                    //register all interfaces first
                    HashSet<ITypeSymbol> interfaceTypes = [];
                    foreach (ITypeSymbol type in types)
                    {
                        foreach (INamedTypeSymbol interfaceType in type.AllInterfaces)
                        {
                            if (interfaceType.IsGenericType)
                            {
                                continue;
                            }

                            interfaceTypes.Add(interfaceType);
                        }
                    }

                    foreach (ITypeSymbol interfaceType in interfaceTypes)
                    {
                        AppendRegisterInterface(source, interfaceType);
                    }

                    foreach (ITypeSymbol type in types)
                    {
                        AppendRegisterType(source, type);
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

        private static void AppendRegisterType(SourceBuilder source, ITypeSymbol type)
        {
            string fullName = type.GetFullTypeName();
            if (fullName.EndsWith("e__FixedBuffer"))
            {
                return;
            }

            source.Append("if (!");
            source.Append(Constants.RegistryTypeName);
            source.Append(".IsTypeRegistered<");
            source.Append(fullName);
            source.Append(">())");
            source.AppendLine();

            source.BeginGroup();
            {
                byte variableCount = 0;
                byte interfaceCount = 0;
                HashSet<string> fieldNames = new();
                foreach (IFieldSymbol field in type.GetFields())
                {
                    if (fieldNames.Add(field.Name))
                    {
                        AppendVariable(source, field, ref variableCount);
                    }
                }

                foreach (INamedTypeSymbol interfaceType in type.AllInterfaces)
                {
                    if (interfaceType.IsGenericType)
                    {
                        continue;
                    }

                    AppendInterface(source, interfaceType, ref interfaceCount);
                }

                source.Append("register.RegisterType<");
                source.Append(fullName);
                source.Append(">(");
                if (variableCount > 0 || interfaceCount > 0)
                {
                    source.Append(FieldBufferVariableName);
                    source.Append(',');
                    source.Append(' ');
                    source.Append(variableCount);
                    source.Append(',');
                    source.Append(' ');
                    source.Append(InterfaceBufferVariableName);
                    source.Append(',');
                    source.Append(' ');
                    source.Append(interfaceCount);
                }

                source.Append(");");
                source.AppendLine();
            }
            source.EndGroup();

            static void AppendVariable(SourceBuilder source, IFieldSymbol field, ref byte count)
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

            static void AppendInterface(SourceBuilder source, INamedTypeSymbol interfaceType, ref byte count)
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