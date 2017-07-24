using System.Collections.Generic;
using Mono.Cecil;

namespace NetInject
{
    class AssemblyNameComparer : IEqualityComparer<AssemblyNameReference>
    {
        public bool Equals(AssemblyNameReference x, AssemblyNameReference y)
            => x.Name == y.Name && x.Version == y.Version;

        public int GetHashCode(AssemblyNameReference obj)
            => $"{obj.Name}:{obj.Version}".GetHashCode();
    }
}