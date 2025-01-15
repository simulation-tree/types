using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Types.Generator
{
    public static class SharedFunctions
    {
        public const string Namespace = "Types";
        public const string TypeLayout = "TypeLayout";
        public const string TypeAttribute = "Types.TypeAttribute";
        public const string InheritInterfacePrefix = "Types.IInherit<";

        public static void CollectTypeSymbols(this Compilation compilation, HashSet<ITypeSymbol> types)
        {
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

                if (IsUnmanaged(type) && HasTypeAttribute(type))
                {
                    types.Add(type);
                }
            }
        }

        public static bool HasTypeAttribute(this ITypeSymbol type)
        {
            Stack<ITypeSymbol> stack = new();

            ImmutableArray<AttributeData> attributes = type.GetAttributes();
            foreach (AttributeData attribute in attributes)
            {
                if (attribute.AttributeClass is INamedTypeSymbol attributeType)
                {
                    stack.Push(attributeType);
                }
            }

            while (stack.Count > 0)
            {
                ITypeSymbol current = stack.Pop();
                string attributeTypeName = current.ToDisplayString();
                if (attributeTypeName == TypeAttribute)
                {
                    return true;
                }
                else
                {
                    if (current.BaseType is INamedTypeSymbol attributeBaseType)
                    {
                        stack.Push(attributeBaseType);
                    }
                }
            }

            return false;
        }

        public static bool IsUnmanaged(this ITypeSymbol type)
        {
            //check if the entire type is a true value type and doesnt contain references
            Stack<ITypeSymbol> stack = new();
            stack.Push(type);

            while (stack.Count > 0)
            {
                ITypeSymbol current = stack.Pop();
                if (current.IsReferenceType)
                {
                    return false;
                }

                foreach (IFieldSymbol field in GetFields(current))
                {
                    stack.Push(field.Type);
                }
            }

            return true;
        }

        public static IEnumerable<IFieldSymbol> GetFields(this ITypeSymbol type)
        {
            foreach (ISymbol typeMember in type.GetMembers())
            {
                if (typeMember is IFieldSymbol field)
                {
                    if (field.HasConstantValue || field.IsStatic)
                    {
                        continue;
                    }

                    yield return field;
                }
            }
        }

        public static IEnumerable<IMethodSymbol> GetMethods(this ITypeSymbol type)
        {
            foreach (ISymbol typeMember in type.GetMembers())
            {
                if (typeMember is IMethodSymbol method)
                {
                    if (method.IsStatic)
                    {
                        continue;
                    }

                    if (method.MethodKind == MethodKind.Constructor)
                    {
                        continue;
                    }

                    yield return method;
                }
            }
        }

        public static string GetFullTypeName(this ITypeSymbol symbol)
        {
            SpecialType special = symbol.SpecialType;
            if (special == SpecialType.System_Boolean)
            {
                return "System.Boolean";
            }
            else if (special == SpecialType.System_Byte)
            {
                return "System.Byte";
            }
            else if (special == SpecialType.System_SByte)
            {
                return "System.SByte";
            }
            else if (special == SpecialType.System_Int16)
            {
                return "System.Int16";
            }
            else if (special == SpecialType.System_UInt16)
            {
                return "System.UInt16";
            }
            else if (special == SpecialType.System_Int32)
            {
                return "System.Int32";
            }
            else if (special == SpecialType.System_UInt32)
            {
                return "System.UInt32";
            }
            else if (special == SpecialType.System_Int64)
            {
                return "System.Int64";
            }
            else if (special == SpecialType.System_UInt64)
            {
                return "System.UInt64";
            }
            else if (special == SpecialType.System_Single)
            {
                return "System.Single";
            }
            else if (special == SpecialType.System_Double)
            {
                return "System.Double";
            }
            else if (special == SpecialType.System_Decimal)
            {
                return "System.Decimal";
            }
            else if (special == SpecialType.System_Char)
            {
                return "System.Char";
            }
            else
            {
                return symbol.ToDisplayString();
            }
        }
    }
}