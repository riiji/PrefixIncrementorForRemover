using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PrefixInLoopRemover
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PrefixInLoopRemoverAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PrefixInLoopRemover";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeForStatement, SyntaxKind.ForStatement);
        }

        private static void AnalyzeForStatement(SyntaxNodeAnalysisContext context)
        {
            var forStatement = (ForStatementSyntax)context.Node;

            if (forStatement == null)
                return;

            var iterators = forStatement.Incrementors;

            foreach (var el in iterators.Where(el => el.IsKind(SyntaxKind.PreIncrementExpression) || el.IsKind(SyntaxKind.PreDecrementExpression)))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, el.GetLocation()));
            }
        }
    }
}
