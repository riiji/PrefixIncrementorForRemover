using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PrefixInLoopRemover
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PrefixInLoopRemoverCodeFixProvider)), Shared]
    public class PrefixInLoopRemoverCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Replace iterator";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(PrefixInLoopRemoverAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ForStatementSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => ReplacePrefixIterator(context.Document, declaration, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> ReplacePrefixIterator(Document document, ForStatementSyntax typeDecl, CancellationToken cancellationToken)
        {
            var iterators = typeDecl.Incrementors;

            var root = await document.GetSyntaxRootAsync(cancellationToken);

            foreach (var el in iterators.Where(el => el.IsKind(SyntaxKind.PreIncrementExpression) || el.IsKind(SyntaxKind.PreDecrementExpression)))
            {
                // find a identifier of node
                if (!(el.ChildNodes().FirstOrDefault(x => x.IsKind(SyntaxKind.IdentifierName)) is IdentifierNameSyntax identifier))
                    continue;

                var newSyntaxKind = (el.IsKind(SyntaxKind.PreIncrementExpression))
                    ? SyntaxKind.PostIncrementExpression
                    : SyntaxKind.PostDecrementExpression;

                var newToken = SyntaxFactory.PostfixUnaryExpression(newSyntaxKind, identifier).NormalizeWhitespace();

                root = root.ReplaceNode(el, newToken);
            }

            var newDocument = document.WithSyntaxRoot(root);

            return newDocument;
        }
    }
}
