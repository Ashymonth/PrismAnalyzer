using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace PrismAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PrismAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        private const string BaseModelType = "Model";
        private const string BaseEntityType = "Entity";

        public const string DiagnosticId = "PrismAnalyzer";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            if (classDeclaration.BaseList == null)
            {
                return;
            }

            var baseType = classDeclaration.BaseList;
            var isModel = baseType.Types.FirstOrDefault()?.GetFirstToken();
            if (!isModel.HasValue || !isModel.Value.Text.Contains(BaseModelType))
            {
                return;
            }

            var semanticModel = context.SemanticModel;

            var constructor = classDeclaration.Members.FirstOrDefault(syntax => syntax.Kind() == SyntaxKind.ConstructorDeclaration);

            var constructorDeclaration = (ConstructorDeclarationSyntax)constructor;
            if (constructorDeclaration == null)
            {
                return;
            }

            var parameters = constructorDeclaration.ParameterList.Parameters;

            if (!parameters.Any())
            {
                return;
            }

            var injectedClass = semanticModel.GetDeclaredSymbol(parameters[0]);

            var injectedClassType = injectedClass?.Type;
            if (injectedClassType?.BaseType == null)
            {
                return;
            }

            if (!injectedClassType.BaseType.Name.Contains(BaseEntityType))
            {
                return;
            }

            var result = Helper.GetProperties(injectedClassType, classDeclaration, semanticModel);

            if (result.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, constructorDeclaration.Identifier.GetLocation()));
            }
        }
    }
}