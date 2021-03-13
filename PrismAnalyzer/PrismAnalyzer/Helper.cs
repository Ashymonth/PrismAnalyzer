using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace PrismAnalyzer
{
    public class Helper
    {
        public static IEnumerable<string> GetProperties(ITypeSymbol paramTypeSymbol)
        {
            return paramTypeSymbol.GetMembers().Where(symbol => symbol.Kind == SymbolKind.Property)
                .Select(symbol => symbol.Name);
        }

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
            var members = typeSymbol.GetMembers().Where(symbol => symbol.Kind == SymbolKind.Property).ToArray();

            var modelProperties = classDeclarationSyntax.Members.OfType<PropertyDeclarationSyntax>()
                .Select(syntax => semanticModel.GetDeclaredSymbol(syntax)?.Name);

            return members.Select(symbol => (symbol.Name, (IPropertySymbol)symbol))
                .Where(arg => !modelProperties.Contains(arg.Name));
        }
    }
}