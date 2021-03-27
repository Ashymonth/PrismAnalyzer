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
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace PrismAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PrismAnalyzerCodeFixProvider)), Shared]
    public class PrismAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(PrismAnalyzerAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var usingDeclaration = root!.DescendantNodesAndSelf().OfType<UsingDirectiveSyntax>().ToList();

            var classDeclaration = root!.FindToken(diagnosticSpan.Start).Parent!.AncestorsAndSelf()
                .OfType<ClassDeclarationSyntax>().First();

            var constructorDeclaration = root.FindToken(diagnosticSpan.Start).Parent!.AncestorsAndSelf()
                .OfType<ConstructorDeclarationSyntax>().First();

            root.TrackNodes(classDeclaration);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: ct =>
                        CreateProperties(context.Document, usingDeclaration, classDeclaration, constructorDeclaration, root, ct),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private static async Task<Document> CreateProperties(Document document, List<UsingDirectiveSyntax> usingDeclaration, ClassDeclarationSyntax classDeclaration,
            ConstructorDeclarationSyntax constructorDeclaration, SyntaxNode root, CancellationToken ct)
        {
            var usingToAdd = new List<UsingDirectiveSyntax>();
            var semanticModel = await document.GetSemanticModelAsync(ct);

            var parameters = constructorDeclaration.ParameterList.Parameters;

            var paramSymbol = semanticModel.GetDeclaredSymbol(parameters[0]);

            var properties = Helper.GetPropertiesWithTypes(paramSymbol!.Type, classDeclaration, semanticModel);

            classDeclaration = classDeclaration.WithMembers(new SyntaxList<MemberDeclarationSyntax>(properties.Select(tuple =>
                 CreateProperty(tuple.Name, tuple.Type, usingDeclaration, usingToAdd))));

            root = root.InsertNodesAfter(constructorDeclaration, classDeclaration.Members);
            root = (root as CompilationUnitSyntax)!.AddUsings(usingToAdd.ToArray());

            return document.WithSyntaxRoot(root);
        }

        private static string SimplifyNameSpaces(ITypeSymbol type, List<UsingDirectiveSyntax> existedUsing, List<UsingDirectiveSyntax> usingToAdd)
        {
            var typeName = type.ToString();
            var nameSpace = type.ContainingNamespace;
            if (nameSpace == null)
            {
                return typeName;
            }

            var nameSpaceName = nameSpace.ToString();

            var usingStatement = existedUsing.FirstOrDefault(syntax => syntax.Name.ToString().Equals(nameSpaceName));
            if (usingStatement == null)
            {
                usingToAdd.Add(UsingDirective(ParseName(nameSpaceName)));
            }

            return typeName.Replace(nameSpaceName, string.Empty).Trim('.');
        }

        private static PropertyDeclarationSyntax CreateProperty(string propName, ITypeSymbol propType, List<UsingDirectiveSyntax> usingDirectiveSyntax, List<UsingDirectiveSyntax> usingToAdd)
        {
            return PropertyDeclaration(
                    IdentifierName(
                        Identifier(
                            TriviaList(),
                            SimplifyNameSpaces(propType, usingDirectiveSyntax, usingToAdd),
                            TriviaList(
                                Space))),
                    Identifier(
                        TriviaList(),
                        propName,
                        TriviaList(
                            CarriageReturnLineFeed)))
                .WithModifiers(
                    TokenList(
                        Token(
                            TriviaList(CarriageReturnLineFeed, Whitespace("        ")),
                            SyntaxKind.PublicKeyword,
                            TriviaList(
                                Space))))
                .WithAccessorList(
                    AccessorList(
                            List(
                                new[]
                                {
                                    AccessorDeclaration(
                                            SyntaxKind.GetAccessorDeclaration)
                                        .WithKeyword(
                                            Token(
                                                TriviaList(
                                                    Whitespace("            ")),
                                                SyntaxKind.GetKeyword,
                                                TriviaList(
                                                    Space)))
                                        .WithExpressionBody(
                                            ArrowExpressionClause(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("Entity"),
                                                        IdentifierName(propName)))
                                                .WithArrowToken(
                                                    Token(
                                                        TriviaList(),
                                                        SyntaxKind.EqualsGreaterThanToken,
                                                        TriviaList(
                                                            Space))))
                                        .WithSemicolonToken(
                                            Token(
                                                TriviaList(),
                                                SyntaxKind.SemicolonToken,
                                                TriviaList(
                                                    CarriageReturnLineFeed))),
                                    AccessorDeclaration(
                                            SyntaxKind.SetAccessorDeclaration)
                                        .WithKeyword(
                                            Token(
                                                TriviaList(
                                                    Whitespace("            ")),
                                                SyntaxKind.SetKeyword,
                                                TriviaList(
                                                    CarriageReturnLineFeed)))
                                        .WithBody(
                                            Block(
                                                    ExpressionStatement(
                                                            AssignmentExpression(
                                                                    SyntaxKind.SimpleAssignmentExpression,
                                                                    MemberAccessExpression(
                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                        IdentifierName(
                                                                            Identifier(
                                                                                TriviaList(
                                                                                    Whitespace("                ")),
                                                                                "Entity",
                                                                                TriviaList())),
                                                                        IdentifierName(
                                                                            Identifier(
                                                                                TriviaList(),
                                                                                propName,
                                                                                TriviaList(
                                                                                    Space)))),
                                                                    IdentifierName("value"))
                                                                .WithOperatorToken(
                                                                    Token(
                                                                        TriviaList(),
                                                                        SyntaxKind.EqualsToken,
                                                                        TriviaList(
                                                                            Space))))
                                                        .WithSemicolonToken(
                                                            Token(
                                                                TriviaList(),
                                                                SyntaxKind.SemicolonToken,
                                                                TriviaList(
                                                                    CarriageReturnLineFeed))),
                                                    ExpressionStatement(
                                                            InvocationExpression(
                                                                IdentifierName(
                                                                    Identifier(
                                                                        TriviaList(
                                                                            Whitespace("                ")),
                                                                        "RaisePropertyChanged",
                                                                        TriviaList()))))
                                                        .WithSemicolonToken(
                                                            Token(
                                                                TriviaList(),
                                                                SyntaxKind.SemicolonToken,
                                                                TriviaList(
                                                                    CarriageReturnLineFeed))))
                                                .WithOpenBraceToken(
                                                    Token(
                                                        TriviaList(
                                                            Whitespace("            ")),
                                                        SyntaxKind.OpenBraceToken,
                                                        TriviaList(
                                                            CarriageReturnLineFeed)))
                                                .WithCloseBraceToken(
                                                    Token(
                                                        TriviaList(
                                                            Whitespace("            ")),
                                                        SyntaxKind.CloseBraceToken,
                                                        TriviaList(
                                                            CarriageReturnLineFeed))))
                                }))
                        .WithOpenBraceToken(
                            Token(
                                TriviaList(
                                    Whitespace("        ")),
                                SyntaxKind.OpenBraceToken,
                                TriviaList(
                                    CarriageReturnLineFeed)))
                        .WithCloseBraceToken(
                            Token(
                                TriviaList(
                                    Whitespace("        ")),
                                SyntaxKind.CloseBraceToken,
                                TriviaList(
                                    CarriageReturnLineFeed))));
        }
    }
}