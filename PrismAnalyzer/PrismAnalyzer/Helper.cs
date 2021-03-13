using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace PrismAnalyzer
{
    public static class Helper
    {
        public static IEnumerable<string> GetProperties(ITypeSymbol typeSymbol, ClassDeclarationSyntax classDeclarationSyntax, SemanticModel semanticModel)
        {
            var entityProperties = typeSymbol.GetMembers().Where(symbol => symbol.Kind == SymbolKind.Property)
                .Select(symbol => symbol.Name);

            var modelProperties = classDeclarationSyntax.Members.OfType<PropertyDeclarationSyntax>()
                .Select(syntax => semanticModel.GetDeclaredSymbol(syntax)?.Name);

            return entityProperties.Except(modelProperties);
        }

        public static IEnumerable<(string Name, IPropertySymbol)> GetPropertiesWithTypes(ITypeSymbol typeSymbol, ClassDeclarationSyntax classDeclarationSyntax, SemanticModel semanticModel)
        {
            var entityProperties = typeSymbol.GetMembers().Where(symbol => symbol.Kind == SymbolKind.Property);

            var modelProperties = classDeclarationSyntax.Members.OfType<PropertyDeclarationSyntax>()
                .Select(syntax => semanticModel.GetDeclaredSymbol(syntax)?.Name);

            return entityProperties.Select(symbol => (symbol.Name, (IPropertySymbol)symbol))
                .Where(arg => !modelProperties.Contains(arg.Name));
        }
    }
}