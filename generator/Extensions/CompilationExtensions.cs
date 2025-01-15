using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Types.Generator
{
    public static class CompilationExtensions
    {
        /// <summary>
        /// Iterates through all value struct types found in all syntax trees.
        /// </summary>
        public static IEnumerable<ITypeSymbol> GetAllTypes(this Compilation compilation)
        {
            HashSet<ITypeSymbol> types = new();
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(tree);
                TypeDeclarationsWalker walker = new(semanticModel);
                walker.Visit(tree.GetRoot());
                foreach (ITypeSymbol type in walker.types)
                {
                    TryAddType(type);
                }
            }

            foreach (MetadataReference assemblyReference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(assemblyReference) is IAssemblySymbol assemblySymbol)
                {
                    Stack<ISymbol> stack = new();
                    stack.Push(assemblySymbol.GlobalNamespace);
                    while (stack.Count > 0)
                    {
                        ISymbol current = stack.Pop();
                        if (current is INamespaceSymbol namespaceSymbol)
                        {
                            foreach (ISymbol member in namespaceSymbol.GetNamespaceMembers())
                            {
                                stack.Push(member);
                            }

                            foreach (ISymbol member in namespaceSymbol.GetTypeMembers())
                            {
                                stack.Push(member);
                            }
                        }
                        else if (current is ITypeSymbol type)
                        {
                            if (type.DeclaredAccessibility != Accessibility.Internal)
                            {
                                TryAddType(type);

                                foreach (ISymbol member in type.GetMembers())
                                {
                                    stack.Push(member);
                                }
                            }
                        }
                    }
                }
            }

            void TryAddType(ITypeSymbol type)
            {
                if (type.IsRefLikeType)
                {
                    return;
                }

                if (type.DeclaredAccessibility == Accessibility.Private || type.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
                {
                    return;
                }

                if (type is INamedTypeSymbol namedType)
                {
                    if (namedType.IsGenericType)
                    {
                        return;
                    }
                }

                if (type.IsUnmanaged())
                {
                    types.Add(type);
                }
            }

            return types;
        }
    }
}