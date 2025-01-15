﻿using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Types.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class TypeLayoutRegistryGenerator : IIncrementalGenerator
    {
        private static readonly SourceBuilder source = new();
        private const string TypeName = "TypeLayoutRegistry";

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(context.CompilationProvider, Generate);
        }

        private static void Generate(SourceProductionContext context, Compilation compilation)
        {
            context.AddSource($"{TypeName}.generated.cs", Generate(compilation));
        }

        public static string Generate(Compilation compilation)
        {
            HashSet<ITypeSymbol> types = [];
            compilation.CollectTypeSymbols(types);

            source.Clear();
            source.AppendLine("using System.Diagnostics;");
            source.AppendLine("using Types;");
            source.AppendLine("using Unmanaged;");
            source.AppendLine();
            source.AppendLine($"namespace {SharedFunctions.Namespace}");
            source.BeginGroup();
            {
                source.AppendLine($"internal static partial class {TypeName}");
                source.BeginGroup();
                {
                    source.AppendLine("/// <summary>");
                    source.AppendLine("/// Registers all types found in this and referenced assemblies.");
                    source.AppendLine("/// </summary>");
                    source.AppendLine($"public static void RegisterAll()");
                    source.BeginGroup();
                    {
                        source.AppendLine("USpan<TypeLayout.Variable> buffer = stackalloc TypeLayout.Variable[32];");

                        //recursively make sure that types of fields are also registered
                        Stack<ITypeSymbol> stack = new();
                        foreach (ITypeSymbol type in types)
                        {
                            stack.Push(type);
                        }

                        while (stack.Count > 0)
                        {
                            ITypeSymbol type = stack.Pop();
                            foreach (IFieldSymbol field in type.GetFields())
                            {
                                if (types.Add(field.Type))
                                {
                                    stack.Push(field.Type);
                                }
                            }
                        }

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
            AppendInheritedFields(type, ref count);

            foreach (IFieldSymbol field in type.GetFields())
            {
                AppendVariable(field, count);
                count++;
            }

            source.AppendLine($"{SharedFunctions.TypeLayout}.Register<{fullName}>(buffer.Slice(0, {count}));");

            static void AppendVariable(IFieldSymbol field, byte count)
            {
                string variableTypeName = field.Type.GetFullTypeName();
                if (field.IsFixedSizeBuffer)
                {
                    variableTypeName = variableTypeName.TrimEnd('*');
                    variableTypeName += '[';
                    variableTypeName += field.FixedSize;
                    variableTypeName += ']';
                }

                source.AppendLine($"buffer[{count}] = new(\"{field.Name}\", \"{variableTypeName}\");");
                count++;
            }

            static void AppendInheritedFields(ITypeSymbol type, ref byte count)
            {
                foreach (INamedTypeSymbol interfaceType in type.AllInterfaces)
                {
                    if (interfaceType.ToDisplayString().StartsWith(SharedFunctions.InheritInterfacePrefix))
                    {
                        ITypeSymbol genericType = interfaceType.TypeArguments[0];
                        AppendInheritedFields(genericType, ref count);

                        foreach (IFieldSymbol field in genericType.GetFields())
                        {
                            AppendVariable(field, count);
                            count++;
                        }
                    }
                }
            }
        }
    }
}