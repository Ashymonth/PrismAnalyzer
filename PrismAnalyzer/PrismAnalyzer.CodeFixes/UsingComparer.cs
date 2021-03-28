using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PrismAnalyzer
{
    public class UsingComparer : IEqualityComparer<UsingDirectiveSyntax>
    {
        public bool Equals(UsingDirectiveSyntax x, UsingDirectiveSyntax y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Name.ToString() == y.Name.ToString();
        }

        public int GetHashCode(UsingDirectiveSyntax obj)
        {
            unchecked
            {
                var hashCode = obj.UsingKeyword.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.StaticKeyword.GetHashCode();
                hashCode = (hashCode * 397) ^ (obj.Alias != null ? obj.Alias.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ obj.Name.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.SemicolonToken.GetHashCode();
                return hashCode;
            }
        }
    }
}