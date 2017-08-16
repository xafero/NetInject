using System.Linq;

namespace NetInject.Inspect
{
    public static class ApiExts
    {
        public static bool IsPInvoke(this IMethod meth)
            => meth.Aliases.Any();
        
        public static int BeNonNegative(this int value, int defaultVal)
            => value >= 0 ? value : defaultVal;
    }
}