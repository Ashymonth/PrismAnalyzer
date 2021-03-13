using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PrismAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PrismAnalyzerCodeFixProvider)), Shared]
    public class PrismAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string PropertyTemplate =
            "        public {0} {1} {{ get => Entity.{1}; set {{ Entity.{1} = value; RaisePropertyChanged(); }} }}\n";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(PrismAnalyzerAnalyzer.DiagnosticId);

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
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

            var constructorDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().First();


            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => MakeUppercaseAsync(context.Document, declaration, constructorDeclaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Document> MakeUppercaseAsync(Document document, ClassDeclarationSyntax typeDeclaration, ConstructorDeclarationSyntax constructorDeclaration, CancellationToken ct)
        {
            var oldRoot = await document.GetSyntaxRootAsync(ct);
            if (oldRoot == null)
            {
                return document;
            }

            var semanticModel = await document.GetSemanticModelAsync(ct);

            var parameters = constructorDeclaration.ParameterList.Parameters;

            var paramSymbol = semanticModel.GetDeclaredSymbol(parameters[0]);

            var properties = Helper.GetPropertiesWithTypes(paramSymbol?.Type, typeDeclaration, semanticModel);

            typeDeclaration = typeDeclaration.WithMembers(Construct(properties));

            var root = oldRoot.InsertNodesAfter(constructorDeclaration, typeDeclaration.Members);

            return document.WithSyntaxRoot(root);
        }

        private static SyntaxList<MemberDeclarationSyntax> Construct(IEnumerable<(string, IPropertySymbol)> properties)
        {
            var result = new List<MemberDeclarationSyntax>();

            foreach (var (propName, propType) in properties)
            {

                var type = propType.Type.ToString().Split('.');
                var result1 = SyntaxFactory.ParseMemberDeclaration(string.Format(PropertyTemplate, type.Last(), propName));

                result.Add(result1);
            }

            return new SyntaxList<MemberDeclarationSyntax>(result);
        }
    }
}