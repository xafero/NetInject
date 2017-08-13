using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        public NativeInspector(IEnumerable<string> filters)
        {
            Filters = filters.ToList();
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
                Process(key, types, nativeRef, report.Units);
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

        private static void Process(string name, IEnumerable<TypeDefinition> types,
            IMetadataTokenProvider invRef, IDictionary<string, IUnit> units)
        {
            var nativeTypeName = Capitalize(Path.GetFileNameWithoutExtension(name));
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
                    unit.Types[nativeTypeName] = ptype = new AssemblyType(nativeTypeName);
                IMethod pmethod;
                if (!ptype.Methods.TryGetValue(key, out pmethod))
                    ptype.Methods[key] = pmethod
                        = new AssemblyMethod(nativeMethName, Deobfuscate(meth.ReturnType.FullName));
                CheckAndInclude(meth.ReturnType);
                pmethod.Parameters.Clear();
                foreach (var parm in meth.Parameters)
                {
                    var mparm = new MethodParameter(parm.Name, Deobfuscate(parm.ParameterType.FullName));
                    pmethod.Parameters.Add(mparm);
                    CheckAndInclude(parm.ParameterType);
                }
                const StringSplitOptions sso = StringSplitOptions.None;
                var text = $"{meth}".Split(new[] {$"{meth.ReturnType}"}, 2, sso).Last().Trim();
                pmethod.Aliases.Add(Deobfuscate(text));
            }
        }

        private static void CheckAndInclude(TypeReference type)
        {
            var assRef = type.Scope as AssemblyNameReference;
            var modRef = type.Scope as ModuleDefinition;
            if ((assRef == null || IsStandardLib(assRef.Name)) && modRef == null)
                return;
            var typeDef = type.Resolve();
            var kind = typeDef.GetTypeKind();
            Console.WriteLine(kind + " " + Deobfuscate(typeDef + "")); // TODO: Include used types recursively!
        }
    }
}