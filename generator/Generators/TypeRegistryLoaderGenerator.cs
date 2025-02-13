using Microsoft.CodeAnalysis;

namespace Types
{
    [Generator(LanguageNames.CSharp)]
    public class TypeRegistryLoaderGenerator : IIncrementalGenerator
    {
        private const string TypeName = "TypeRegistryLoader";

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(context.CompilationProvider, Generate);
        }

        private void Generate(SourceProductionContext context, Compilation compilation)
        {
            if (compilation.GetEntryPoint(context.CancellationToken) is not null)
            {
                context.AddSource($"{TypeName}.generated.cs", Generate(compilation));
            }
        }

        private static string Generate(Compilation compilation)
        {
            string? assemblyName = compilation.AssemblyName;
            SourceBuilder builder = new();
            builder.AppendLine("using Types;");
            builder.AppendLine();

            if (assemblyName is not null)
            {
                builder.Append("namespace ");
                builder.Append(assemblyName);
                builder.AppendLine();
                builder.BeginGroup();
            }

            builder.Append("public static class ");
            builder.Append(TypeName);
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

                        if (type.HasInterface("Types.ITypeBank"))
                        {
                            builder.Append("TypeRegistry.Load<");
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