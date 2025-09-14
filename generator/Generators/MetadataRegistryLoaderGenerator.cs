using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace Types.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class MetadataRegistryLoaderGenerator : IIncrementalGenerator
    {
        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(context.CompilationProvider, Generate);
        }

        private void Generate(SourceProductionContext context, Compilation compilation)
        {
            if (compilation.GetEntryPoint(context.CancellationToken) is not null)
            {
                context.AddSource($"{Constants.RegistryLoaderTypeName}.generated.cs", Generate(compilation));
            }
        }

        private static string Generate(Compilation compilation)
        {
            string? assemblyName = compilation.AssemblyName;
            SourceBuilder builder = new();
            builder.Append("using ");
            builder.Append(Constants.Namespace);
            builder.Append(';');
            builder.AppendLine();

            builder.AppendLine();

            if (assemblyName is not null)
            {
                builder.Append("namespace ");
                builder.Append(assemblyName);
                builder.AppendLine();
                builder.BeginGroup();
            }

            builder.Append("public static class ");
            builder.Append(Constants.RegistryLoaderTypeName);
            builder.AppendLine();
            builder.BeginGroup();
            {
                builder.AppendLine("/// <summary>");
                builder.AppendLine("/// Registers all types declared by this and other");
                builder.AppendLine("/// referenced projects.");
                builder.AppendLine("/// </summary>");

                builder.AppendLine("public static void Load()");
                builder.BeginGroup();
                {
                    // collect compatible types
                    List<ITypeSymbol> types = new();
                    foreach (ITypeSymbol type in compilation.GetAllTypes())
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

                        types.Add(type);
                    }

                    // check if there are any structs that the metadata bank generator would work with,
                    // in order to find out if this assembly has a metadata bank itself
                    bool hasMetadataBank = false;
                    foreach (ITypeSymbol type in types)
                    {
                        if (type.TypeKind == TypeKind.Struct && type.IsUnmanaged())
                        {
                            if (type.ContainingAssembly?.Name != assemblyName)
                            {
                                continue;
                            }

                            hasMetadataBank = true;
                            break;
                        }
                    }

                    if (hasMetadataBank)
                    {
                        string? metadataBankAssemblyName = types[0].ContainingAssembly?.Name;
                        if (metadataBankAssemblyName is not null && metadataBankAssemblyName.EndsWith(".Core"))
                        {
                            metadataBankAssemblyName = metadataBankAssemblyName.Substring(0, metadataBankAssemblyName.Length - 5);
                        }

                        builder.Append(Constants.RegistryTypeName);
                        builder.Append(".Load<");
                        if (metadataBankAssemblyName is not null)
                        {
                            builder.Append(metadataBankAssemblyName);
                            builder.Append('.');
                        }

                        string typeName = MetadataBankGenerator.TypeNameFormat.Replace("{0}", assemblyName ?? "");
                        typeName = typeName.Replace(".", "");
                        builder.Append(typeName);
                        builder.Append(">();");
                        builder.AppendLine();
                    }

                    foreach (ITypeSymbol type in types)
                    {
                        if (type.HasInterface(Constants.Namespace + '.' + Constants.MetadataBankTypeName))
                        {
                            builder.Append(Constants.RegistryTypeName);
                            builder.Append(".Load<");
                            builder.Append(type.GetFullTypeName());
                            builder.Append(">();");
                            builder.AppendLine();
                        }
                    }
                }
                builder.EndGroup();
            }
            builder.EndGroup();

            if (assemblyName is not null)
            {
                builder.EndGroup();
            }

            return builder.ToString();
        }
    }
}