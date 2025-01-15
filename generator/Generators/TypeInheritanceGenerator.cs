using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Threading;

namespace Types.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class TypeInheritanceGenerator : IIncrementalGenerator
    {
        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<InputType?> structTypes = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);
            context.RegisterSourceOutput(structTypes, Generate);
        }

        private static void Generate(SourceProductionContext context, InputType? input)
        {
            if (input is not InheritingType inheritingType)
            {
                return;
            }

            SourceBuilder builder = new();
            builder.AppendLine("using Types;");
            builder.AppendLine();
            builder.AppendLine($"namespace {input.containedNamespace}");
            builder.BeginGroup();
            {
                if (inheritingType.isReadOnly)
                {
                    builder.AppendLine($"public readonly partial struct {input.typeName}");
                }
                else
                {
                    builder.AppendLine($"public partial struct {input.typeName}");
                }

                builder.BeginGroup();
                {
                    foreach (ITypeSymbol inheritedType in inheritingType.inheritedTypes)
                    {
                        foreach (IFieldSymbol field in inheritedType.GetFields())
                        {
                            if (field.IsReadOnly)
                            {
                                builder.AppendLine($"public readonly {field.Type.ToDisplayString()} {field.Name};");
                            }
                            else
                            {
                                builder.AppendLine($"public {field.Type.ToDisplayString()} {field.Name};");
                            }
                        }

                        foreach (IMethodSymbol method in inheritedType.GetMethods())
                        {
                            if (method.IsReadOnly)
                            {
                                builder.AppendLine($"public readonly void {method.Name}()");
                            }
                            else
                            {
                                builder.AppendLine($"public void {method.Name}()");
                            }

                            builder.BeginGroup();
                            {
                                builder.AppendLine("throw new System.NotImplementedException();");
                            }
                            builder.EndGroup();
                        }
                    }
                }
                builder.EndGroup();
            }
            builder.EndGroup();
            context.AddSource($"{input.typeName}.generated.cs", builder.ToString());
        }

        private static bool Predicate(SyntaxNode node, CancellationToken token)
        {
            return node.IsKind(SyntaxKind.StructDeclaration);
        }

        private static InputType? Transform(GeneratorSyntaxContext context, CancellationToken token)
        {
            StructDeclarationSyntax node = (StructDeclarationSyntax)context.Node;
            ITypeSymbol? typeSymbol = context.SemanticModel.GetDeclaredSymbol(node);
            if (typeSymbol is null)
            {
                return null;
            }

            if (!node.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                //todo: emit and error if partial and has the interface
                return null;
            }

            if (typeSymbol.AllInterfaces.Length > 0)
            {
                List<ITypeSymbol> inheritedTypes = new();
                foreach (INamedTypeSymbol interfaceType in typeSymbol.AllInterfaces)
                {
                    if (interfaceType.ToDisplayString().StartsWith(SharedFunctions.InheritInterfacePrefix))
                    {
                        //read the generic type
                        ITypeSymbol genericType = interfaceType.TypeArguments[0];
                        inheritedTypes.Add(genericType);
                    }
                }

                if (inheritedTypes.Count > 0)
                {
                    string? containingNamespace = typeSymbol.ContainingNamespace?.ToDisplayString();
                    string fullTypeName = typeSymbol.ToDisplayString();
                    bool isReadOnly = typeSymbol.IsReadOnly;
                    return new InheritingType(typeSymbol.Name, fullTypeName, containingNamespace, isReadOnly, inheritedTypes);
                }
            }

            return null;
        }
    }
}