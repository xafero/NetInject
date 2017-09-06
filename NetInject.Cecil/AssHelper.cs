using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NetInject.Cecil
{
    public static class AssHelper
    {
        public static IEnumerable<T> GetAttribute<T>(this ICustomAttributeProvider prov) where T : Attribute
            => GetAttribute<T>(prov.CustomAttributes);

        private static IEnumerable<T> GetAttribute<T>(IEnumerable<CustomAttribute> attributes) where T : Attribute
            => attributes.Where(a => a.AttributeType.FullName == typeof(T).FullName)
                .Select(a => a.ConstructorArguments.Select(c => c.Value).ToArray())
                .Select(a => (T)typeof(T).GetConstructors().First().Invoke(a));
    }
}