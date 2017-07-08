using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace NetInject
{
    static class CecilHelper
    {
        public static IEnumerable<TypeDefinition> GetAllTypes(this AssemblyDefinition ass)
            => ass.Modules.SelectMany(m => m.GetAllTypes());

        public static IEnumerable<TypeDefinition> GetAllTypes(this ModuleDefinition mod)
            => mod.Types.SelectMany(t => t.GetAllTypes());

        public static IEnumerable<TypeDefinition> GetAllTypes(this TypeDefinition type)
            => (new[] { type }).Concat(type.NestedTypes.SelectMany(t => t.GetAllTypes()));
    }
}