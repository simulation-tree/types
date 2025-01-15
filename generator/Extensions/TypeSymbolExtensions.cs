using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Types.Generator
{
    public static class TypeSymbolExtensions
    {
        /// <summary>
        /// Checks if the type is a true value type and doesnt contain any references.
        /// </summary>
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
                string attributeTypeName = current.GetFullTypeName();
                if (attributeTypeName == "Types.TypeAttribute")
                {
                    return true;
                }
                else
                {
                    if (current.BaseType is INamedTypeSymbol currentBaseType)
                    {
                        stack.Push(currentBaseType);
                    }
                }
            }

            return false;
        }

        public static IEnumerable<ITypeSymbol> GetInheritingTypes(this ITypeSymbol type)
        {
            Stack<INamedTypeSymbol> stack = new();
            foreach (INamedTypeSymbol interfaceType in type.AllInterfaces)
            {
                stack.Push(interfaceType);
                while (stack.Count > 0)
                {
                    INamedTypeSymbol current = stack.Pop();
                    if (current.GetFullTypeName().StartsWith("Types.IInherits<"))
                    {
                        ITypeSymbol genericType = current.TypeArguments[0];
                        foreach (INamedTypeSymbol interfaceTypeOfGeneric in genericType.AllInterfaces)
                        {
                            stack.Push(interfaceTypeOfGeneric);
                        }

                        yield return genericType;
                    }
                    else if (current.BaseType is INamedTypeSymbol currentBaseType)
                    {
                        stack.Push(currentBaseType);
                    }
                }
            }
        }
    }
}