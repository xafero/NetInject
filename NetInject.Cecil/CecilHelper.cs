using System.Collections.Generic;
using System.Diagnostics;
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

        public static bool IsStandardLib(string key)
            => key == "mscorlib" || key == "System" ||
               key == "System.Core" || key == "Microsoft.CSharp";

        public static bool IsDelegate(this TypeDefinition type)
            => type?.BaseType?.FullName == typeof(System.MulticastDelegate).FullName
               || type?.BaseType?.FullName == typeof(System.Delegate).FullName;

        public static string GetParamStr(IMetadataTokenProvider meth)
            => meth.ToString().Split(new[] {'('}, 2).Last().TrimEnd(')');

        public static TypeKind GetTypeKind(this TypeDefinition typeDef)
        {
            if (typeDef.IsEnum)
                return TypeKind.Enum;
            if (typeDef.IsValueType)
                return TypeKind.Struct;
            if (typeDef.IsDelegate())
                return TypeKind.Delegate;
            if (typeDef.IsInterface)
                return TypeKind.Interface;
            if (typeDef.IsClass)
                return TypeKind.Class;
            return default(TypeKind);
        }
    }
}