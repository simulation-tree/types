using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Types.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class TypeLayoutRegistryGenerator : IIncrementalGenerator
    {
        private static readonly SourceBuilder source = new();
        private const string RegistryName = "TypeLayoutRegistry";
        private const string Namespace = "Types";
        private const string TypeLayoutName = "TypeLayout";

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(context.CompilationProvider, Generate);
        }

        private static void Generate(SourceProductionContext context, Compilation compilation)
        {
            context.AddSource($"{RegistryName}.generated.cs", Generate(compilation));
        }

        private static bool IsPublic(ITypeSymbol type)
        {
            if (type.DeclaredAccessibility == Accessibility.Public)
            {
                //if any generic parameters are non public
                if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
                {
                    foreach (ITypeSymbol typeArgument in namedType.TypeArguments)
                    {
                        if (typeArgument.DeclaredAccessibility != Accessibility.Public)
                        {
                            return false;
                        }
                    }
                }

                //if any field type is non public
                foreach (IFieldSymbol field in type.GetFields())
                {
                    if (field.Type.DeclaredAccessibility != Accessibility.Public)
                    {
                        return false;
                    }

                    if (field.Type.ToDisplayString().EndsWith("?"))
                    {
                        return false;
                    }
                }

                if (type.ToDisplayString().EndsWith("?"))
                {
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IncludeType(ITypeSymbol type)
        {
            //ignore void
            if (type.SpecialType == SpecialType.System_Void)
            {
                return false;
            }

            //skip anything from specific namespaces
            string typeFullName = type.GetFullTypeName();
            if (type.ContainingNamespace is not null)
            {
                if (typeFullName.StartsWith("System."))
                {
                    return false;
                }
                else if (typeFullName.StartsWith("Microsoft.") || typeFullName.StartsWith("NUnit.") || typeFullName.StartsWith("Newtonsoft."))
                {
                    return false;
                }
            }

            //skip pointer types
            if (typeFullName.Contains("*"))
            {
                return false;
            }

            return true;
        }

        public static string Generate(Compilation compilation)
        {
            source.Clear();

            HashSet<ITypeSymbol> allTypes = [];
            Stack<ITypeSymbol> stack = new();
            foreach (ITypeSymbol type in compilation.GetAllTypes())
            {
                stack.Push(type);
                allTypes.Add(type);
            }

            while (stack.Count > 0)
            {
                ITypeSymbol current = stack.Pop();
                foreach (IFieldSymbol field in current.GetFields())
                {
                    if (allTypes.Add(field.Type))
                    {
                        stack.Push(field.Type);
                    }
                }
            }

            //prune incompatible types
            HashSet<ITypeSymbol> types = [];
            foreach (ITypeSymbol type in allTypes)
            {
                if (IsPublic(type) && IncludeType(type))
                {
                    int fieldCount = 0;
                    foreach (IFieldSymbol field in type.GetFields())
                    {
                        fieldCount++;
                    }

                    if (fieldCount < 16)
                    {
                        types.Add(type);
                    }
                }
            }

            //todo: should also prune types that are never referenced/mentioned

            source.AppendLine("using System.Diagnostics;");
            source.AppendLine("using Types;");
            source.AppendLine("using Unmanaged;");
            source.AppendLine();

            source.Append("namespace ");
            source.Append(Namespace);
            source.AppendLine();

            source.BeginGroup();
            {
                source.Append("internal static partial class ");
                source.Append(RegistryName);
                source.AppendLine();

                source.BeginGroup();
                {
                    source.AppendLine("/// <summary>");
                    source.AppendLine("/// Registers all types found in this and referenced assemblies.");
                    source.AppendLine("/// </summary>");
                    source.AppendLine($"public static void RegisterAll()");
                    source.BeginGroup();
                    {
                        source.AppendLine("USpan<TypeLayout.Variable> buffer = stackalloc TypeLayout.Variable[(int)TypeLayout.Capacity];");

                        bool registeredBoolean = false;
                        bool registeredByte = false;
                        bool registeredSByte = false;
                        bool registeredInt16 = false;
                        bool registeredUInt16 = false;
                        bool registeredInt32 = false;
                        bool registeredUInt32 = false;
                        bool registeredInt64 = false;
                        bool registeredUInt64 = false;
                        bool registeredSingle = false;
                        bool registeredDouble = false;
                        bool registeredDecimal = false;
                        bool registeredChar = false;
                        bool registeredNint = false;
                        bool registeredNuint = false;
                        foreach (ITypeSymbol type in types)
                        {
                            string typeFullName = type.GetFullTypeName();
                            if (!registeredBoolean && typeFullName == "System.Boolean")
                            {
                                registeredBoolean = true;
                            }

                            if (!registeredByte && typeFullName == "System.Byte")
                            {
                                registeredByte = true;
                            }

                            if (!registeredSByte && typeFullName == "System.SByte")
                            {
                                registeredSByte = true;
                            }

                            if (!registeredInt16 && typeFullName == "System.Int16")
                            {
                                registeredInt16 = true;
                            }

                            if (!registeredUInt16 && typeFullName == "System.UInt16")
                            {
                                registeredUInt16 = true;
                            }

                            if (!registeredInt32 && typeFullName == "System.Int32")
                            {
                                registeredInt32 = true;
                            }

                            if (!registeredUInt32 && typeFullName == "System.UInt32")
                            {
                                registeredUInt32 = true;
                            }

                            if (!registeredInt64 && typeFullName == "System.Int64")
                            {
                                registeredInt64 = true;
                            }

                            if (!registeredUInt64 && typeFullName == "System.UInt64")
                            {
                                registeredUInt64 = true;
                            }

                            if (!registeredSingle && typeFullName == "System.Single")
                            {
                                registeredSingle = true;
                            }

                            if (!registeredDouble && typeFullName == "System.Double")
                            {
                                registeredDouble = true;
                            }

                            if (!registeredDecimal && typeFullName == "System.Decimal")
                            {
                                registeredDecimal = true;
                            }

                            if (!registeredChar && typeFullName == "System.Char")
                            {
                                registeredChar = true;
                            }

                            if (!registeredNint && typeFullName == "System.IntPtr")
                            {
                                registeredNint = true;
                            }

                            if (!registeredNuint && typeFullName == "System.UIntPtr")
                            {
                                registeredNuint = true;
                            }

                            AppendLayoutRegistration(type, typeFullName);
                        }

                        if (!registeredBoolean)
                        {
                            AppendLayoutRegistration(compilation.GetTypeByMetadataName("System.Boolean") ?? throw new(), "System.Boolean");
                        }

                        if (!registeredByte)
                        {
                            AppendLayoutRegistration(compilation.GetTypeByMetadataName("System.Byte") ?? throw new(), "System.Byte");
                        }

                        if (!registeredSByte)
                        {
                            AppendLayoutRegistration(compilation.GetTypeByMetadataName("System.SByte") ?? throw new(), "System.SByte");
                        }

                        if (!registeredInt16)
                        {
                            AppendLayoutRegistration(compilation.GetTypeByMetadataName("System.Int16") ?? throw new(), "System.Int16");
                        }

                        if (!registeredUInt16)
                        {
                            AppendLayoutRegistration(compilation.GetTypeByMetadataName("System.UInt16") ?? throw new(), "System.UInt16");
                        }

                        if (!registeredInt32)
                        {
                            AppendLayoutRegistration(compilation.GetTypeByMetadataName("System.Int32") ?? throw new(), "System.Int32");
                        }

                        if (!registeredUInt32)
                        {
                            AppendLayoutRegistration(compilation.GetTypeByMetadataName("System.UInt32") ?? throw new(), "System.UInt32");
                        }

                        if (!registeredInt64)
                        {
                            AppendLayoutRegistration(compilation.GetTypeByMetadataName("System.Int64") ?? throw new(), "System.Int64");
                        }

                        if (!registeredUInt64)
                        {
                            AppendLayoutRegistration(compilation.GetTypeByMetadataName("System.UInt64") ?? throw new(), "System.UInt64");
                        }

                        if (!registeredSingle)
                        {
                            AppendLayoutRegistration(compilation.GetTypeByMetadataName("System.Single") ?? throw new(), "System.Single");
                        }

                        if (!registeredDouble)
                        {
                            AppendLayoutRegistration(compilation.GetTypeByMetadataName("System.Double") ?? throw new(), "System.Double");
                        }

                        if (!registeredDecimal)
                        {
                            AppendLayoutRegistration(compilation.GetTypeByMetadataName("System.Decimal") ?? throw new(), "System.Decimal");
                        }

                        if (!registeredChar)
                        {
                            AppendLayoutRegistration(compilation.GetTypeByMetadataName("System.Char") ?? throw new(), "System.Char");
                        }

                        if (!registeredNint)
                        {
                            AppendLayoutRegistration(compilation.GetTypeByMetadataName("System.IntPtr") ?? throw new(), "System.IntPtr");
                        }

                        if (!registeredNuint)
                        {
                            AppendLayoutRegistration(compilation.GetTypeByMetadataName("System.UIntPtr") ?? throw new(), "System.UIntPtr");
                        }
                    }
                    source.EndGroup();
                }
                source.EndGroup();
            }
            source.EndGroup();
            return source.ToString();
        }

        private static void AppendLayoutRegistration(ITypeSymbol type, string fullName)
        {
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

            source.Append(TypeLayoutName);
            source.Append(".Register<");
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