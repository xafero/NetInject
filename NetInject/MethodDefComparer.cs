using System;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;

namespace NetInject
{
    class MethodDefComparer : IEqualityComparer<MethodDefinition>
    {
        public bool Equals(MethodDefinition x, MethodDefinition y)
            => GetId(x) == GetId(y);

        public int GetHashCode(MethodDefinition obj)
            => GetId(obj).GetHashCode();

        static string GetId(MethodDefinition obj)
            => obj.FullName.Split(new[] { "::" }, StringSplitOptions.None).Last();
    }
}