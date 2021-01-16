using System.Collections.Generic;
using System.Linq;

namespace MarineLang.Models.Asts
{
    public static class AstExtensions
    {
        public static IEnumerable<T> LookUp<T>(this IAst ast) where T : class, IAst
        {
            return ast.GetChildrenWithThis()
                .Select(x => x is T t ? t : null)
                .Where(x => x != null);
        }
    }
}
