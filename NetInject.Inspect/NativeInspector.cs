using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using NetInject.Cecil;
using static NetInject.Cecil.WordHelper;
using static NetInject.Cecil.CecilHelper;

namespace NetInject.Inspect
{
    public class NativeInspector : IInspector
    {
        private static readonly StringComparer Comp = StringComparer.InvariantCultureIgnoreCase;

        public IList<string> Filters { get; }
        private ManagedInspector Managed { get; }

        public NativeInspector(IEnumerable<string> filters)
        {
            Filters = filters.ToList();
            Managed = new ManagedInspector(new string[0]);
        }

        public int Inspect(AssemblyDefinition ass, IDependencyReport report)
        {
            var natives = 0;
            var types = ass.GetAllTypes().ToArray();
            foreach (var nativeRef in ass.Modules.SelectMany(m => m.ModuleReferences)
                .Where(r => Filters.Count < 1 || Filters.Contains(r.Name, Comp)))
            {
                var key = NormalizeNativeRef(nativeRef);
                ISet<string> list;
                if (!report.NativeRefs.TryGetValue(key, out list))
                    report.NativeRefs[key] = list = new SortedSet<string>();
                list.Add(ass.FullName);
                Process(key, types, nativeRef, report);
                natives++;
            }
            return natives;
        }

        private static string NormalizeNativeRef(IMetadataScope nativeRef)
        {
            var name = nativeRef.Name;
            name = name.ToLowerInvariant();
            const string suffix = ".dll";
            if (!name.EndsWith(suffix))
                name = $"{name}{suffix}";
            return name;
        }

        private void Process(string name, IEnumerable<TypeDefinition> types,
            IMetadataTokenProvider invRef, IDependencyReport report)
        {
            var units = report.Units;
            var nativeTypeName = Capitalize(Path.GetFileNameWithoutExtension(name));
            var collot = new TypeCollector();
            INamingStrategy nameArgStrategy = null;
            foreach (var meth in types.SelectMany(t => t.Methods))
            {
                PInvokeInfo pinv;
                if (!meth.HasPInvokeInfo || invRef != (pinv = meth.PInvokeInfo).Module)
                    continue;
                var nativeMethName = pinv.EntryPoint;
                var retType = Deobfuscate(meth.ReturnType.ToString());
                var parms = Deobfuscate(GetParamStr(meth));
                var key = $"{nativeMethName} {retType} {parms}";
                IUnit unit;
                if (!units.TryGetValue(name, out unit))
                    units[name] = unit = new AssemblyUnit(nativeTypeName, new Version("0.0.0.0"));
                IType ptype;
                if (!unit.Types.TryGetValue(nativeTypeName, out ptype))
                    unit.Types[nativeTypeName] = ptype = new AssemblyType(nativeTypeName, TypeKind.Class);
                nameArgStrategy = new NativeArgNameStrategy(ptype);
                collot.Collect(meth);
                var newRetType = nameArgStrategy.GetName(meth.ReturnType);
                var methRetType = Deobfuscate(meth.ReturnType.FullName);
                if (newRetType != null)
                    methRetType = newRetType;
                IMethod pmethod;
                if (!ptype.Methods.TryGetValue(key, out pmethod))
                    ptype.Methods[key] = pmethod = new AssemblyMethod(nativeMethName, methRetType);
                pmethod.Parameters.Clear();
                foreach (var parm in meth.Parameters)
                {
                    var newParmType = nameArgStrategy.GetName(parm.ParameterType);
                    var mparmType = Deobfuscate(parm.ParameterType.FullName);
                    if (newParmType != null)
                        mparmType = newParmType;
                    var mparm = new MethodParameter(parm.Name, mparmType);
                    pmethod.Parameters.Add(mparm);
                }
                const StringSplitOptions sso = StringSplitOptions.None;
                var text = $"{meth}".Split(new[] { $"{meth.ReturnType}" }, 2, sso).Last().Trim();
                pmethod.Aliases.Add(Deobfuscate(text));
            }
            if (nameArgStrategy != null)
                foreach (var type in collot.Types)
                    CheckAndInclude(report, type, nameArgStrategy);
        }

        private void CheckAndInclude(IDependencyReport report, TypeReference type, INamingStrategy strategy)
        {
            if (type?.IsInStandardLib() ?? true)
                return;
            var typeDef = type.Resolve();
            Managed.InspectType(report, type, typeDef, null, strategy);
        }

        private class NativeArgNameStrategy : INamingStrategy
        {
            private readonly IType _type;

            public NativeArgNameStrategy(IType type)
            {
                _type = type;
            }

            public string GetName(AssemblyDefinition ass) => _type.Namespace;

            public string GetName(TypeDefinition type) =>
                type.IsInStandardLib() ? null : $"{_type.Namespace}.{type.Name}";

            public string GetName(TypeReference type) =>
                type.IsInStandardLib() ? null : $"{_type.Namespace}.{type.Name}";
        }
    }
}