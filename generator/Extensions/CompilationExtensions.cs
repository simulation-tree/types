using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Types.Generator
{
    public static class CompilationExtensions
    {
        public static IEnumerable<SyntaxTree> GetAllTrees(this Compilation compilation)
        {
            HashSet<SyntaxTree> trees = new();
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                trees.Add(tree);
            }

            foreach (MetadataReference assemblyReference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(assemblyReference) is IAssemblySymbol assemblySymbol)
                {
                    foreach (SyntaxReference declarationReference in assemblySymbol.DeclaringSyntaxReferences)
                    {
                        SyntaxTree tree = declarationReference.SyntaxTree;
                        trees.Add(tree);
                    }
                }
            }

            return trees;
        }

        /// <summary>
        /// Iterates through all types found in every syntax trees.
        /// </summary>
        public static IReadOnlyCollection<ITypeSymbol> GetAllTypes(this Compilation compilation)
        {
            Stack<ISymbol> symbolStack = new();
            HashSet<ITypeSymbol> types = [];
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(tree);
                TypeDeclarationsWalker walker = new(semanticModel);
                walker.Visit(tree.GetRoot());
                foreach (ITypeSymbol type in walker.types)
                {
                    types.Add(type);
                }
            }

            foreach (MetadataReference assemblyReference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(assemblyReference) is IAssemblySymbol assemblySymbol)
                {
                    symbolStack.Push(assemblySymbol.GlobalNamespace);
                    while (symbolStack.Count > 0)
                    {
                        ISymbol current = symbolStack.Pop();
                        if (current is INamespaceSymbol namespaceSymbol)
                        {
                            foreach (ISymbol member in namespaceSymbol.GetNamespaceMembers())
                            {
                                symbolStack.Push(member);
                            }

                            foreach (ISymbol member in namespaceSymbol.GetTypeMembers())
                            {
                                symbolStack.Push(member);
                            }
                        }
                        else if (current is ITypeSymbol type)
                        {
                            types.Add(type);
                            foreach (ISymbol member in type.GetMembers())
                            {
                                symbolStack.Push(member);
                            }
                        }
                        else if (current is IFieldSymbol field)
                        {
                            types.Add(field.Type);
                        }
                        else if (current is IMethodSymbol method)
                        {
                            types.Add(method.ReturnType);
                            foreach (IParameterSymbol parameter in method.Parameters)
                            {
                                types.Add(parameter.Type);
                            }
                        }
                        else if (current is IPropertySymbol property)
                        {
                            types.Add(property.Type);
                        }
                    }
                }
            }

            return types;
        }

        /// <summary>
        /// Iterates through all value types found in every syntax trees.
        /// </summary>
        public static IReadOnlyDictionary<ITypeSymbol, TypeMetadata> GetAllReferencedTypes(this Compilation compilation)
        {
            Dictionary<ITypeSymbol, TypeMetadata> valueTypes = [];
            IReadOnlyCollection<ITypeSymbol> allTypes = GetAllTypes(compilation);
            foreach (ITypeSymbol type in allTypes)
            {
                if (type.IsRefLikeType)
                {
                    continue;
                }

                if (type.DeclaredAccessibility == Accessibility.Private || type.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
                {
                    continue;
                }

                if (type is INamedTypeSymbol namedType)
                {
                    if (namedType.IsGenericType)
                    {
                        continue;
                    }
                }

                if (type.IsUnmanaged())
                {
                    valueTypes.Add(type, new(type, 0));
                }
            }

            //now count the references
            HashSet<SyntaxNode> rootNodes = [];
            Dictionary<string, HashSet<ITypeSymbol>> symbolMap = new();
            foreach (ITypeSymbol type in allTypes)
            {
                foreach (SyntaxReference reference in type.DeclaringSyntaxReferences)
                {
                    rootNodes.Add(reference.GetSyntax());
                }

                if (symbolMap.TryGetValue(type.Name, out HashSet<ITypeSymbol> symbols))
                {
                    symbols.Add(type);
                }
                else
                {
                    symbols = [];
                    symbols.Add(type);
                    symbolMap.Add(type.Name, symbols);
                }

                if (symbolMap.TryGetValue(type.GetFullTypeName(), out symbols))
                {
                    symbols.Add(type);
                }
                else
                {
                    symbols = [];
                    symbols.Add(type);
                    symbolMap.Add(type.GetFullTypeName(), symbols);
                }
            }

            foreach (SyntaxNode rootNode in rootNodes)
            {
                foreach (SyntaxNode descendantNode in rootNode.DescendantNodes())
                {
                    if (descendantNode is FieldDeclarationSyntax fieldDeclaration)
                    {
                        TryIncrementReferenceCount(fieldDeclaration.Declaration.Type);
                    }
                    else if (descendantNode is VariableDeclarationSyntax variableDeclaration)
                    {
                        TryIncrementReferenceCount(variableDeclaration.Type);
                    }
                    else if (descendantNode is BasePropertyDeclarationSyntax propertyDeclaration)
                    {
                        TryIncrementReferenceCount(propertyDeclaration.Type);
                    }
                    else if (descendantNode is TypeArgumentListSyntax typeArgumentList)
                    {
                        foreach (TypeSyntax typeSyntax in typeArgumentList.Arguments)
                        {
                            TryIncrementReferenceCount(typeSyntax);
                        }
                    }
                    else if (descendantNode is ObjectCreationExpressionSyntax objectCreation)
                    {
                        TryIncrementReferenceCount(objectCreation.Type);
                    }
                    else if (descendantNode is ArgumentListSyntax argumentList)
                    {
                        foreach (ArgumentSyntax argumentNode in argumentList.Arguments)
                        {
                            foreach (SyntaxNode argumentDescendantNode in argumentNode.DescendantNodes())
                            {
                                if (argumentDescendantNode is IdentifierNameSyntax identifierName)
                                {
                                    TryIncrementReferenceCount(identifierName);
                                }
                            }
                        }
                    }
                    else if (descendantNode is IdentifierNameSyntax identifier)
                    {
                        TryIncrementReferenceCount(identifier);
                    }
                }
            }

            void TryIncrementReferenceCount(TypeSyntax type)
            {
                string typeName;
                if (type is SimpleNameSyntax namedTypeSyntax)
                {
                    typeName = namedTypeSyntax.Identifier.ToString();
                }
                else
                {
                    typeName = type.ToString();
                }

                if (symbolMap.TryGetValue(typeName, out HashSet<ITypeSymbol>? symbols))
                {
                    foreach (ITypeSymbol symbol in symbols)
                    {
                        if (valueTypes.TryGetValue(symbol, out TypeMetadata metadata))
                        {
                            metadata.references++;
                        }
                    }
                }
            }

            return valueTypes;
        }
    }
}