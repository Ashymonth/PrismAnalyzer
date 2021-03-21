using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                .OfType<ClassDeclarationSyntax>().First();

            var constructorDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                .OfType<ConstructorDeclarationSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: ct =>
                        MakeUppercaseAsync(context.Document, declaration, constructorDeclaration, ct),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Document> MakeUppercaseAsync(Document document, ClassDeclarationSyntax typeDeclaration,
            ConstructorDeclarationSyntax constructorDeclaration, CancellationToken ct)
        {
            var oldRoot = await document.GetSyntaxRootAsync(ct);
            if (oldRoot == null)
            {
                return document;
            }

            var semanticModel = await document.GetSemanticModelAsync(ct);

            var parameters = constructorDeclaration.ParameterList.Parameters;

            var paramSymbol = semanticModel.GetDeclaredSymbol(parameters[0]);

            if (paramSymbol == null)
            {
                return document;
            }

            var properties = Helper.GetPropertiesWithTypes(paramSymbol.Type, typeDeclaration, semanticModel);

            typeDeclaration = typeDeclaration.WithMembers(new SyntaxList<MemberDeclarationSyntax>(properties.Select((tuple, i) => Test(tuple.Name, tuple.Item2.Name))));

            var root = oldRoot.InsertNodesAfter(constructorDeclaration, typeDeclaration.Members);

            return document.WithSyntaxRoot(root);
        }

        private static PropertyDeclarationSyntax Test(string propName, string propType)
        {
            return PropertyDeclaration(
                    IdentifierName(
                        Identifier(
                            TriviaList(),
                            propType,
                            TriviaList(
                                Space
                            )
                        )
                    ),
                    Identifier(
                        TriviaList(),
                        propName,
                        TriviaList(
                            CarriageReturnLineFeed
                        )
                    )
                )
                .WithModifiers(
                    TokenList(
                        Token(
                            TriviaList(
                                new[]
                                {
                                    CarriageReturnLineFeed,
                                    Whitespace("        ")
                                }
                            ),
                            SyntaxKind.PublicKeyword,
                            TriviaList(
                                Space
                            )
                        )
                    )
                )
                .WithAccessorList(
                    AccessorList(
                            List<AccessorDeclarationSyntax>(
                                new AccessorDeclarationSyntax[]
                                {
                                    AccessorDeclaration(
                                            SyntaxKind.GetAccessorDeclaration
                                        )
                                        .WithKeyword(
                                            Token(
                                                TriviaList(
                                                    Whitespace("            ")
                                                ),
                                                SyntaxKind.GetKeyword,
                                                TriviaList(
                                                    Space
                                                )
                                            )
                                        )
                                        .WithExpressionBody(
                                            ArrowExpressionClause(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("Entity"),
                                                        IdentifierName(propName)
                                                    )
                                                )
                                                .WithArrowToken(
                                                    Token(
                                                        TriviaList(),
                                                        SyntaxKind.EqualsGreaterThanToken,
                                                        TriviaList(
                                                            Space
                                                        )
                                                    )
                                                )
                                        )
                                        .WithSemicolonToken(
                                            Token(
                                                TriviaList(),
                                                SyntaxKind.SemicolonToken,
                                                TriviaList(
                                                    CarriageReturnLineFeed
                                                )
                                            )
                                        ),
                                    AccessorDeclaration(
                                            SyntaxKind.SetAccessorDeclaration
                                        )
                                        .WithKeyword(
                                            Token(
                                                TriviaList(
                                                    Whitespace("            ")
                                                ),
                                                SyntaxKind.SetKeyword,
                                                TriviaList(
                                                    CarriageReturnLineFeed
                                                )
                                            )
                                        )
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
                                                                                    Whitespace("                ")
                                                                                ),
                                                                                "Entity",
                                                                                TriviaList()
                                                                            )
                                                                        ),
                                                                        IdentifierName(
                                                                            Identifier(
                                                                                TriviaList(),
                                                                                propName,
                                                                                TriviaList(
                                                                                    Space
                                                                                )
                                                                            )
                                                                        )
                                                                    ),
                                                                    IdentifierName("value")
                                                                )
                                                                .WithOperatorToken(
                                                                    Token(
                                                                        TriviaList(),
                                                                        SyntaxKind.EqualsToken,
                                                                        TriviaList(
                                                                            Space
                                                                        )
                                                                    )
                                                                )
                                                        )
                                                        .WithSemicolonToken(
                                                            Token(
                                                                TriviaList(),
                                                                SyntaxKind.SemicolonToken,
                                                                TriviaList(
                                                                    CarriageReturnLineFeed
                                                                )
                                                            )
                                                        ),
                                                    ExpressionStatement(
                                                            InvocationExpression(
                                                                IdentifierName(
                                                                    Identifier(
                                                                        TriviaList(
                                                                            Whitespace("                ")
                                                                        ),
                                                                        "RaisePropertyChanged",
                                                                        TriviaList()
                                                                    )
                                                                )
                                                            )
                                                        )
                                                        .WithSemicolonToken(
                                                            Token(
                                                                TriviaList(),
                                                                SyntaxKind.SemicolonToken,
                                                                TriviaList(
                                                                    CarriageReturnLineFeed
                                                                )
                                                            )
                                                        )
                                                )
                                                .WithOpenBraceToken(
                                                    Token(
                                                        TriviaList(
                                                            Whitespace("            ")
                                                        ),
                                                        SyntaxKind.OpenBraceToken,
                                                        TriviaList(
                                                            CarriageReturnLineFeed
                                                        )
                                                    )
                                                )
                                                .WithCloseBraceToken(
                                                    Token(
                                                        TriviaList(
                                                            Whitespace("            ")
                                                        ),
                                                        SyntaxKind.CloseBraceToken,
                                                        TriviaList(
                                                            CarriageReturnLineFeed
                                                        )
                                                    )
                                                )
                                        )
                                }
                            )
                        )
                        .WithOpenBraceToken(
                            Token(
                                TriviaList(
                                    Whitespace("        ")
                                ),
                                SyntaxKind.OpenBraceToken,
                                TriviaList(
                                    CarriageReturnLineFeed
                                )
                            )
                        )
                        .WithCloseBraceToken(
                            Token(
                                TriviaList(
                                    Whitespace("        ")
                                ),
                                SyntaxKind.CloseBraceToken,
                                TriviaList()
                            )
                        )
                );
        }
    }
}