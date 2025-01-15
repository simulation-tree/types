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

        public static string Generate(Compilation compilation)
        {
            HashSet<ITypeSymbol> types = [];
            Stack<ITypeSymbol> stack = new();
            foreach (ITypeSymbol type in compilation.GetAllTypes())
            {
                if (type.HasTypeAttribute())
                {
                    stack.Push(type);
                    types.Add(type);
                }
            }

            while (stack.Count > 0)
            {
                ITypeSymbol current = stack.Pop();
                foreach (IFieldSymbol field in current.GetFields())
                {
                    if (types.Add(field.Type))
                    {
                        stack.Push(field.Type);
                    }
                }
            }

            source.Clear();
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
                        source.AppendLine("USpan<TypeLayout.Variable> buffer = stackalloc TypeLayout.Variable[32];");

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
                        foreach (ITypeSymbol type in types)
                        {
                            if (type.DeclaredAccessibility != Accessibility.Public)
                            {
                                continue;
                            }

                            //skip anything from system assemblies
                            if (type.ContainingAssembly is IAssemblySymbol assembly)
                            {
                                if (assembly.Name.StartsWith("System") || assembly.Name.StartsWith("Microsoft") || assembly.Name.StartsWith("NUnit") || assembly.Name.StartsWith("Newtonsoft"))
                                {
                                    continue;
                                }
                            }

                            if (type.ContainingNamespace is INamespaceSymbol containingNamespace)
                            {
                                if (containingNamespace.Name.StartsWith("System") || containingNamespace.Name.StartsWith("Microsoft") || containingNamespace.Name.StartsWith("NUnit") || containingNamespace.Name.StartsWith("Newtonsoft"))
                                {
                                    continue;
                                }
                            }

                            //skip pointer types
                            string typeFullName = type.GetFullTypeName();
                            if (typeFullName.Contains("*"))
                            {
                                continue;
                            }

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

                            AppendLayoutRegistration(type, type.GetFullTypeName());
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