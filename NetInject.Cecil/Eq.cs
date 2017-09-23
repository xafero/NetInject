using System.Collections.Generic;
using Mono.Cecil;

namespace NetInject.Cecil
{
    public static class Eq
    {
        public static IEqualityComparer<GenericInstanceType> Type => new GenericTypeComparer();
        public static IEqualityComparer<GenericInstanceMethod> Meth => new GenericMethodComparer();

        private class GenericTypeComparer : IEqualityComparer<GenericInstanceType>
        {
            public bool Equals(GenericInstanceType x, GenericInstanceType y) 
                => x?.FullName.Equals(y?.FullName) ?? false;

            public int GetHashCode(GenericInstanceType obj) => obj.FullName.GetHashCode();
        }

        private class GenericMethodComparer : IEqualityComparer<GenericInstanceMethod>
        {
            public bool Equals(GenericInstanceMethod x, GenericInstanceMethod y) 
                => x?.FullName.Equals(y?.FullName) ?? false;

            public int GetHashCode(GenericInstanceMethod obj) => obj.FullName.GetHashCode();
        }
    }
}