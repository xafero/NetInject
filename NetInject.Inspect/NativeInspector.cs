using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using NetInject.Cecil;

using static NetInject.Cecil.CecilHelper;

namespace NetInject.Inspect
{
    public class NativeInspector : IInspector
    {
        private static readonly StringComparer Comp
            = StringComparer.InvariantCultureIgnoreCase;

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
                .Where(r => Filters.Contains(r.Name, Comp)))
            {
                var key = NormalizeNativeRef(nativeRef);
                ISet<string> list;
                if (!report.NativeRefs.TryGetValue(key, out list))
                    report.NativeRefs[key] = list = new SortedSet<string>();
                list.Add(ass.FullName);
                
                // PurgedAssemblies purged
                
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
    }
}

/*
static void InvertNativeRef(ModuleReference invRef, PurgedAssemblies purged,
            IEnumerable<TypeDefinition> types)
        {
            log.Info($" - '{invRef}'");
            var invRefName = Capitalize(Path.GetFileNameWithoutExtension(invRef.Name));
            PurgedAssembly purge;
            if (!purged.TryGetValue(invRefName, out purge))
                purged[invRefName] = purge = new PurgedAssembly(invRefName, new Version("0.0.0.0"));
            var ptypeName = invRefName.Split('.').First();
            PurgedType ptype;
            if (!purge.Types.TryGetValue(ptypeName, out ptype))
                purge.Types[ptypeName] = ptype = new PurgedType(invRefName, ptypeName);
            foreach (var type in types)
            foreach (var meth in type.Methods)
            {
                PInvokeInfo pinv;
                if (!meth.HasPInvokeInfo || invRef != (pinv = meth.PInvokeInfo).Module)
                    continue;
                var nativeTypeName = invRefName;
                var nativeMethName = pinv.EntryPoint;
                PurgedMethod pmethod;
                if (!ptype.Methods.TryGetValue(nativeMethName, out pmethod))
                    ptype.Methods[nativeMethName] = pmethod = new PurgedMethod(nativeMethName);
                foreach (var parm in meth.Parameters)
                {
                    var mparm = new PurgedParam
                    {
                        Name = Escape(parm.Name),
                        ParamType = parm.ParameterType.FullName
                    };
                    pmethod.Parameters.Add(mparm);
                }
                if (meth.ReturnType.FullName != typeof(void).FullName)
                    pmethod.ReturnType = meth.ReturnType.FullName;
                pmethod.Refs.Add(meth.FullName);
            }
        }
        
        */
        
        