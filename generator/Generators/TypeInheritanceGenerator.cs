using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Threading;

namespace Types
{
    [Generator(LanguageNames.CSharp)]
    public class TypeInheritanceGenerator : IIncrementalGenerator
    {
        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<InputType?> structTypes = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);
            IncrementalValuesProvider<(InputType? Left, Compilation Right)> input = structTypes.Combine(context.CompilationProvider);
            context.RegisterSourceOutput(input, Generate);
        }

        private static void Generate(SourceProductionContext context, (InputType? input, Compilation compilation) data)
        {
            InputType? type = data.input;
            if (type is null)
            {
                return;
            }

            SourceBuilder builder = new();
            builder.AppendLine("using Types;");
            builder.AppendLine();

            if (type.containedNamespace is not null)
            {
                builder.Append("namespace ");
                builder.Append(type.containedNamespace);
                builder.AppendLine();

                builder.BeginGroup();
            }

            //write the type declaration
            builder.Append("public ");
            if (type.isReadOnly)
            {
                builder.Append("readonly ");
            }

            builder.Append("partial struct ");
            builder.Append(type.typeName);

            //decorate with interfaces
            List<ITypeSymbol> implementingTypes = new();
            foreach (ITypeSymbol inheritedType in type.inheritedTypes)
            {
                foreach (ITypeSymbol implementingType in inheritedType.AllInterfaces)
                {
                    if (!implementingType.GetFullTypeName().StartsWith("Types.IInherits<"))
                    {
                        implementingTypes.Add(implementingType);
                    }
                }
            }

            if (implementingTypes.Count > 0)
            {
                builder.Append(" : ");
                for (int i = 0; i < implementingTypes.Count; i++)
                {
                    ITypeSymbol implementingType = implementingTypes[i];
                    builder.Append(implementingType.GetFullTypeName());
                    if (i < implementingTypes.Count - 1)
                    {
                        builder.Append(", ");
                    }
                }
            }

            builder.AppendLine();

            //implement everything that the inheriting types do
            builder.BeginGroup();
            {
                if (type.inheritedTypes.Count > 0)
                {
                    foreach (ITypeSymbol inheritedType in type.inheritedTypes)
                    {
                        AppendInheritedType(data, builder, inheritedType);
                    }

                    //implicit casts up towards the inherited types
                    builder.AppendLine();
                    for (int i = 0; i < type.inheritedTypes.Count; i++)
                    {
                        ITypeSymbol inheritedType = type.inheritedTypes[i];
                        builder.Append("public static implicit operator ");
                        builder.Append(inheritedType.GetFullTypeName());
                        builder.Append('(');
                        builder.Append(type.typeName);
                        builder.Append(" input)");
                        builder.AppendLine();

                        builder.BeginGroup();
                        {
                            builder.Append(inheritedType.GetFullTypeName());
                            builder.Append(" result = default;");
                            foreach (IFieldSymbol field in inheritedType.GetFields())
                            {
                                builder.AppendLine();
                                builder.Append("result.");
                                builder.Append(field.Name);
                                builder.Append(" = input.");
                                builder.Append(field.Name);
                                builder.Append(';');
                            }

                            builder.AppendLine();
                            builder.Append("return result;");
                            builder.AppendLine();
                        }
                        builder.EndGroup();

                        if (i < type.inheritedTypes.Count - 1)
                        {
                            builder.AppendLine();
                        }
                    }
                }
            }
            builder.EndGroup();

            if (type.containedNamespace is not null)
            {
                builder.EndGroup();
            }

            context.AddSource($"{type.typeName}.generated.cs", builder.ToString());
        }

        private static void AppendInheritedType((InputType? input, Compilation compilation) data, SourceBuilder builder, ITypeSymbol inheritedType)
        {
            //write fields
            foreach (IFieldSymbol field in inheritedType.GetFields())
            {
                builder.Append("public ");
                if (field.IsReadOnly)
                {
                    builder.Append("readonly ");
                }

                builder.Append(field.Type.GetFullTypeName());
                builder.Append(' ');
                builder.Append(field.Name);
                builder.Append(';');
                builder.AppendLine();
            }

            //write properties and methods
            HashSet<SyntaxNode> propertyNodes = new();
            foreach (IMethodSymbol method in inheritedType.GetMethods())
            {
                SyntaxReference syntaxReference = method.DeclaringSyntaxReferences[0];
                SyntaxNode syntaxNode = syntaxReference.GetSyntax();
                if (method.MethodKind == MethodKind.PropertyGet || method.MethodKind == MethodKind.PropertySet)
                {
                    SyntaxNode? current = syntaxNode;
                    while (current is not null)
                    {
                        if (current.Parent is PropertyDeclarationSyntax propertyNode)
                        {
                            if (propertyNodes.Add(propertyNode))
                            {
                                builder.AppendLine();

                                //copy entire property but replace types with full names
                                if (method.DeclaredAccessibility == Accessibility.Public)
                                {
                                    builder.Append("public ");
                                }

                                if (method.IsReadOnly)
                                {
                                    //only write the readonly keyword if there is no body
                                    foreach (SyntaxNode childNode in propertyNode.ChildNodes())
                                    {
                                        if (childNode is ArrowExpressionClauseSyntax arrowExpression)
                                        {
                                            builder.Append("readonly ");
                                            break;
                                        }
                                    }
                                }

                                //type name
                                SemanticModel semanticModel = data.compilation.GetSemanticModel(propertyNode.SyntaxTree);
                                ITypeSymbol? propertyType = semanticModel.GetSymbolInfo(propertyNode.Type).Symbol as ITypeSymbol;
                                if (propertyType is not null)
                                {
                                    builder.Append(propertyType.GetFullTypeName());
                                }
                                else
                                {
                                    builder.Append(propertyNode.Type.ToFullString());
                                }

                                builder.Append(' ');

                                //type of explicit interface
                                if (method.ExplicitInterfaceImplementations.Length > 0)
                                {
                                    IMethodSymbol first = method.ExplicitInterfaceImplementations[0];
                                    builder.Append(first.ContainingType.GetFullTypeName());
                                    builder.Append('.');
                                }

                                //name
                                builder.Append(propertyNode.Identifier);

                                //body
                                foreach (SyntaxNode childNode in propertyNode.ChildNodes())
                                {
                                    if (childNode is ArrowExpressionClauseSyntax arrowExpression)
                                    {
                                        builder.Append(' ');
                                        builder.Append(arrowExpression.ToString());
                                        builder.Append(';');
                                        builder.AppendLine();
                                    }
                                    else if (childNode is AccessorListSyntax multipleBodies)
                                    {
                                        builder.AppendLine();
                                        builder.AppendLine(multipleBodies.ToString());
                                    }
                                }
                            }

                            break;
                        }

                        current = current.Parent;
                    }
                }
                else
                {
                    //copy entire method
                    builder.AppendLine();
                    builder.AppendLine(syntaxNode.ToString());
                }
            }
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
                List<ITypeSymbol> inheritedTypes = [.. typeSymbol.GetInheritingTypes()];
                if (inheritedTypes.Count > 0)
                {
                    string? containingNamespace = typeSymbol.ContainingNamespace?.ToDisplayString();
                    string fullTypeName = typeSymbol.GetFullTypeName();
                    bool isReadOnly = typeSymbol.IsReadOnly;
                    SyntaxTree syntaxTree = node.SyntaxTree;
                    return new(syntaxTree, typeSymbol.Name, fullTypeName, containingNamespace, isReadOnly, inheritedTypes);
                }
            }

            return null;
        }
    }
}