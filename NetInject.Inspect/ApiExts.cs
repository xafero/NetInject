using System.Linq;

namespace NetInject.Inspect
{
    public static class ApiExts
    {
        public static bool IsPInvoke(this IMethod meth)
            => meth.Aliases.Any();
    }
}