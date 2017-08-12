using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace NetInject.Cecil
{
    public static class CecilHelper
    {
        public static IEnumerable<TypeDefinition> GetAllTypes(this AssemblyDefinition ass)
            => ass.Modules.SelectMany(m => m.GetAllTypes());

        public static IEnumerable<TypeDefinition> GetAllTypes(this ModuleDefinition mod)
            => mod.Types.SelectMany(t => t.GetAllTypes());

        public static IEnumerable<TypeDefinition> GetAllTypes(this TypeDefinition type)
            => (new[] {type}).Concat(type.NestedTypes.SelectMany(t => t.GetAllTypes()));

        public static IEnumerable<TypeReference> GetAllTypeRefs(this AssemblyDefinition ass)
            => ass.Modules.SelectMany(m => m.GetTypeReferences());

        public static IEnumerable<MemberReference> GetAllMemberRefs(this AssemblyDefinition ass)
            => ass.Modules.SelectMany(m => m.GetMemberReferences());
    }
}