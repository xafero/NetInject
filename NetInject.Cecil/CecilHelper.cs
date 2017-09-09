using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using System;
using System.Reflection;

using MethodBody = Mono.Cecil.Cil.MethodBody;
using Mono.Cecil.Cil;

namespace NetInject.Cecil
{
    public static class CecilHelper
    {
        private static readonly StringComparison ignoreCase = StringComparison.InvariantCultureIgnoreCase;

        public static IEnumerable<TypeDefinition> GetAllTypes(this AssemblyDefinition ass)
            => ass.Modules.SelectMany(m => m.GetAllTypes());

        public static IEnumerable<TypeDefinition> GetAllTypes(this ModuleDefinition mod)
            => mod.Types.SelectMany(t => t.GetAllTypes());

        public static IEnumerable<TypeDefinition> GetAllTypes(this TypeDefinition type)
            => (new[] { type }).Concat(type.NestedTypes.SelectMany(t => t.GetAllTypes()));

        public static IEnumerable<TypeReference> GetAllTypeRefs(this AssemblyDefinition ass)
            => ass.Modules.SelectMany(m => m.GetTypeReferences());

        public static IEnumerable<MemberReference> GetAllMemberRefs(this AssemblyDefinition ass)
            => ass.Modules.SelectMany(m => m.GetMemberReferences());

        public static bool IsStandardLib(string key)
            => key == "mscorlib" || key == "System" ||
               key == "System.Core" || key == "Microsoft.CSharp";

        public static bool IsGenerated(AssemblyDefinition ass)
            => ass.GetAttribute<AssemblyMetadataAttribute>().Any(a => a.Value.Equals(nameof(NetInject), ignoreCase));

        public static bool IsDelegate(this TypeDefinition type)
            => type?.BaseType?.FullName == typeof(System.MulticastDelegate).FullName
               || type?.BaseType?.FullName == typeof(System.Delegate).FullName;

        public static string GetParamStr(IMetadataTokenProvider meth)
            => meth.ToString().Split(new[] { '(' }, 2).Last().TrimEnd(')');

        public static bool IsInStandardLib(this TypeReference type)
        {
            var assRef = type.Scope as AssemblyNameReference;
            var modRef = type.Scope as ModuleDefinition;
            return (assRef == null || IsStandardLib(assRef.Name)) && modRef == null;
        }

        public static bool ContainsType(AssemblyNameReference assRef, TypeReference typRef)
            => assRef.FullName == (typRef.Scope as AssemblyNameReference)?.FullName;

        public static bool ContainsMember(AssemblyNameReference assRef, MemberReference mbmRef)
            => ContainsType(assRef, mbmRef.DeclaringType);

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

        public static bool IsBaseCandidate(this TypeDefinition myTypeDef)
            => !myTypeDef.IsValueType && !myTypeDef.IsSpecialName && !myTypeDef.IsSealed
               && !myTypeDef.IsRuntimeSpecialName && !myTypeDef.IsPrimitive
               && !myTypeDef.IsInterface && !myTypeDef.IsArray &&
               (myTypeDef.IsPublic || myTypeDef.IsNestedPublic) &&
               myTypeDef.IsClass;

        public static IEnumerable<TypeDefinition> GetDerivedTypes(this AssemblyDefinition ass,
            TypeReference baseType)
            => ass.GetAllTypes().Where(type => type.BaseType == baseType);

        public static IEnumerable<MemberReference> GetAllMembers(this TypeDefinition typeDef)
        {
            var evts = typeDef.Events.Cast<MemberReference>();
            var filds = typeDef.Fields.Cast<MemberReference>();
            var meths = typeDef.Methods.Cast<MemberReference>();
            var props = typeDef.Properties.Cast<MemberReference>();
            return evts.Concat(filds).Concat(meths).Concat(props).Distinct();
        }

        public static TypeReference[] GetDistinctTypes(this MethodDefinition meth)
            => new[] { meth.ReturnType }.Concat(meth.Parameters.Select(p => p.ParameterType))
            .Distinct().Where(t => !t.IsInStandardLib()).ToArray();

        public static bool Match(this TypeReference first, TypeReference second)
        {
            var firstKey = first.Name.ToLowerInvariant();
            var secondKey = second.Name.ToLowerInvariant();
            return firstKey == secondKey;
        }

        public static void PatchTypes(this MethodDefinition meth,
            IDictionary<TypeReference, TypeDefinition> replaces,
            Action<TypeReference> onReplace)
        {
            TypeDefinition newType;
            if (replaces.TryGetValue(meth.ReturnType, out newType))
            {
                onReplace(meth.ReturnType);
                meth.ReturnType = Import(meth, newType);
            }
            foreach (var ptype in meth.Parameters)
                if (replaces.TryGetValue(ptype.ParameterType, out newType))
                {
                    onReplace(ptype.ParameterType);
                    ptype.ParameterType = Import(meth, newType);
                }
        }

        private static TypeReference Import(MethodDefinition meth, TypeDefinition newType)
            => meth.DeclaringType.Module.ImportReference(newType);

        public static MethodReference Import(MethodBody body, MethodReference newMeth)
            => body.Method.DeclaringType.Module.ImportReference(newMeth);

        public static IEnumerable<IMetadataScope> GetAllExternalRefs(this AssemblyDefinition ass)
            => ass.Modules.SelectMany(m => m.GetAllExternalRefs());

        public static IEnumerable<IMetadataScope> GetAllExternalRefs(this ModuleDefinition mod)
            => mod.AssemblyReferences.OfType<IMetadataScope>().Concat(mod.ModuleReferences);

        public static void Remove(this AssemblyDefinition ass, ModuleReference native)
        {
            foreach (var mod in ass.Modules)
                mod.ModuleReferences.Remove(native);
        }

        public static void Remove(this AssemblyDefinition ass, AssemblyNameReference assembly)
        {
            foreach (var mod in ass.Modules)
                mod.AssemblyReferences.Remove(assembly);
        }

        public static MethodDefinition FindMethodByStr(AssemblyDefinition ass, string methodStr)
            => string.IsNullOrWhiteSpace(methodStr) ? null : ass.GetAllTypes().SelectMany(
                t => t.Methods).SingleOrDefault(m => m.ToString().Contains(methodStr));

        public static MethodDefinition FindMethodByOld(AssemblyDefinition ass, MethodDefinition oldMeth,
            bool ignoreParamPassing = true)
        {
            Func<MethodReference, string> simplify = m => m.ToString().Split(new[] { ':' }, 3).Last();
            foreach (var newMeth in ass.GetAllTypes().SelectMany(t => t.Methods))
            {
                var oldMethKey = simplify(oldMeth);
                var newMethKey = simplify(newMeth);
                if (ignoreParamPassing)
                {
                    oldMethKey = oldMethKey.Replace("&", "");
                    newMethKey = newMethKey.Replace("&", "");
                }
                if (oldMethKey.Contains(newMethKey))
                    return newMeth;
            }
            return null;
        }

        public static Instruction GoBack(this Instruction il, int steps)
        {
            var previous = il;
            for (var i = 0; i < steps; i++)
                previous = previous.Previous;
            return previous;
        }
    }
}