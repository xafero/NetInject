using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace NetInject.Cecil
{
    public class MethodDefComparer : IEqualityComparer<MethodDefinition>
    {
        public bool Equals(MethodDefinition x, MethodDefinition y)
            => GetId(x) == GetId(y);

        public int GetHashCode(MethodDefinition obj)
            => GetId(obj).GetHashCode();

        private static string GetId(MemberReference obj)
            => obj.FullName.Split(new[] { "::" }, StringSplitOptions.None).Last();
    }
}