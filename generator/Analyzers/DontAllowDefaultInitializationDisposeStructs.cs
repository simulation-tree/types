using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Types
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DontAllowDefaultInitializationDisposeStructs : DiagnosticAnalyzer
    {
        private const string ID = "T0001";
        private const string Title = "Disposable struct must be initialized";
        private const string Format = "Disposable struct type `{0}` cannot be initialized with a default value";
        private const string Category = "Types";
        private const DiagnosticSeverity Severity = DiagnosticSeverity.Error;

        private static readonly DiagnosticDescriptor rule;

        static DontAllowDefaultInitializationDisposeStructs()
        {
            rule = new(ID, Title, Format, Category, Severity, true);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [rule];

        public override void Initialize(AnalysisContext context)
        {
            SyntaxKind syntaxKinds = SyntaxKind.VariableDeclaration;
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, syntaxKinds);
        }

        private void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is VariableDeclarationSyntax variableDeclaration)
            {
                //check if the type being declared is disposable
                TypeSyntax type = variableDeclaration.Type;
                ITypeSymbol? typeSymbol = context.SemanticModel.GetTypeInfo(type).Type;
                if (typeSymbol is null)
                {
                    return;
                }

                if (!typeSymbol.IsValueType)
                {
                    return;
                }

                if (!typeSymbol.HasInterface("System.IDisposable"))
                {
                    return;
                }

                string? name = null;
                bool assignedToDefault = false;
                foreach (SyntaxNode child in variableDeclaration.ChildNodes())
                {
                    if (child is IdentifierNameSyntax identifierName)
                    {
                        name = identifierName.Identifier.Text;
                    }

                    if (name is not null)
                    {
                        if (child is VariableDeclaratorSyntax variableDeclarator)
                        {
                            foreach (SyntaxNode grandChild in child.ChildNodes())
                            {
                                if (grandChild is EqualsValueClauseSyntax equalsValueClause)
                                {
                                    foreach (SyntaxNode greatGrandChild in grandChild.ChildNodes())
                                    {
                                        if (greatGrandChild is LiteralExpressionSyntax literalExpression)
                                        {
                                            if (literalExpression.Token.Text == "default")
                                            {
                                                assignedToDefault = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (assignedToDefault)
                {
                    Location location = context.Node.GetLocation();
                    string typeName = type.ToString();
                    Diagnostic diagnostic = Diagnostic.Create(rule, location, typeName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}