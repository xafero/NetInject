using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using System;
using System.Reflection;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using Mono.Cecil.Cil;
using System.IO;

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
               key == "System.Core" || key == "Microsoft.CSharp" ||
               key == "System.Xml" || key == "System.Configuration" ||
               key == "System.Data.SqlXml" || key == "System.Web";

        public static bool IsStandardMod(string key)
            => key == "CommonLanguageRuntimeLibrary" ||
               IsStandardLib(Path.GetFileNameWithoutExtension(key));

        public static bool IsGenerated(AssemblyDefinition ass)
            => ass.GetAttribute<AssemblyMetadataAttribute>().Any(a => a.Value.Equals(nameof(NetInject), ignoreCase));

        public static bool IsDelegate(this TypeDefinition type)
            => type?.BaseType?.FullName == typeof(System.MulticastDelegate).FullName
               || type?.BaseType?.FullName == typeof(System.Delegate).FullName;

        public static string GetParamStr(IMetadataTokenProvider meth)
            => meth.ToString().Split(new[] { '(' }, 2).Last().TrimEnd(')');

        public static IEnumerable<TypeReference> UnpackGenerics(this TypeReference type)
        {
            GenericInstanceType genType;
            return type.IsGenericInstance && (genType = (GenericInstanceType)type).HasGenericArguments ?
                new[] { genType.ElementType }.Concat(genType.GenericArguments.SelectMany(a => a.UnpackGenerics()))
                : new[] { type };
        }

        public static bool IsInStandardLib(this TypeReference typeRef)
            => IsInStandardLib(typeRef.UnpackGenerics());

        private static bool IsInStandardLib(this IEnumerable<TypeReference> typeRefs)
            => typeRefs.All(IsInStandardLibPrivate);

        private static bool IsInStandardLibPrivate(TypeReference type)
        {
            var modRef = type.Scope as ModuleDefinition;
            if (modRef != null && IsStandardMod(modRef.Name))
                return true;
            var assRef = type.Scope as AssemblyNameReference;
            if (assRef != null && IsStandardLib(assRef.Name))
                return true;
            return false;
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

        public static TypeReference[] CollectDistinctTypes(this MethodDefinition meth)
        {
            ITypeCollector collector = new TypeCollector();
            collector.Collect(meth);
            return collector.Types.ToArray();
        }

        public static bool Match(this TypeReference first, TypeReference second)
        {
            const char refChar = '&';
            var firstKey = first.Name.TrimEnd(refChar).ToLowerInvariant();
            var secondKey = second.Name.TrimEnd(refChar).ToLowerInvariant();
            return firstKey == secondKey;
        }

        public static MethodReference Import(MethodBody body, MethodReference newMeth)
            => body.Method.DeclaringType.Module.ImportReference(newMeth);

        public static IEnumerable<TypeReference> FindDistinctRefs(TypeDefinition typeDef)
            => new[] { typeDef.BaseType }.Concat(typeDef.Interfaces.Select(i => i.InterfaceType))
                .Concat(typeDef.Events.Select(e => e.EventType))
                .Concat(typeDef.Properties.Select(p => p.PropertyType))
                .Concat(typeDef.Fields.Select(f => f.FieldType))
                .Concat(typeDef.Methods.Select(m => m.ReturnType))
                .Concat(typeDef.Methods.SelectMany(m => m.Parameters)
                    .Concat(typeDef.Properties.SelectMany(p => p.Parameters))
                    .Select(p => p.ParameterType))
                .Concat(typeDef.NestedTypes)
                .Distinct().Where(t => !(t?.IsInStandardLib() ?? true));

        public static IEnumerable<IMetadataScope> GetAllExternalRefs(this AssemblyDefinition ass)
            => ass.Modules.SelectMany(m => m.GetAllExternalRefs());

        public static IEnumerable<IMetadataScope> GetAllExternalRefs(this ModuleDefinition mod)
            => mod.AssemblyReferences.OfType<IMetadataScope>().Concat(mod.ModuleReferences);

        public static void Remove(this AssemblyDefinition ass, ModuleReference native)
        {
            foreach (var mod in ass.Modules)
                if (!mod.ModuleReferences.Remove(native))
                    throw new InvalidOperationException(mod.Name);
        }

        public static void Remove(this AssemblyDefinition ass, AssemblyNameReference assembly)
        {
            foreach (var mod in ass.Modules)
                if (!mod.AssemblyReferences.Remove(assembly))
                    throw new InvalidOperationException(mod.Name);
        }

        public static MethodDefinition FindMethodByStr(AssemblyDefinition ass, string methodStr)
            => string.IsNullOrWhiteSpace(methodStr)
                ? null
                : ass.GetAllTypes().SelectMany(
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

        public static bool HasNoUsefulOperand(this Instruction instr)
            => instr.Operand == null || instr.Operand is string
               || instr.Operand is Instruction || instr.Operand is Instruction[]
               || instr.Operand.GetType().IsPrimitive;

        #region Try resolve

        public static MethodDefinition TryResolve(this MethodReference r)
        {
            try
            {
                return r.Resolve();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static FieldDefinition TryResolve(this FieldReference r)
        {
            try
            {
                return r.Resolve();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static EventDefinition TryResolve(this EventReference r)
        {
            try
            {
                return r.Resolve();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static VariableDefinition TryResolve(this VariableReference r)
        {
            try
            {
                return r.Resolve();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static ParameterDefinition TryResolve(this ParameterReference r)
        {
            try
            {
                return r.Resolve();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static TypeDefinition TryResolve(this TypeReference r)
        {
            try
            {
                return r.Resolve();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static PropertyDefinition TryResolve(this PropertyReference r)
        {
            try
            {
                return r.Resolve();
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion
    }
}