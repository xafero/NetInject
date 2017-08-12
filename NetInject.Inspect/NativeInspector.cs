using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using NetInject.Cecil;
using static NetInject.Cecil.CecilHelper;
using static NetInject.Cecil.WordHelper;

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

                // PurgedAssemblies purged                
                Process(key, types, nativeRef);

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

        private static string Deobfuscate(string text)
        {
            var buff = new StringBuilder();
            foreach (var letter in text)
                if ((letter >= 'A' && letter <= 'Z') || (letter >= 'a' && letter <= 'z')
                    || (letter >= '0' && letter <= '9') || letter == '.' || letter == '&'
                    || letter == ' ' || letter == '/' || letter == ',')
                    buff.Append(letter);
            return buff.ToString();
        }

        private static string GetParamStr(IMetadataTokenProvider meth)
            => meth.ToString().Split(new[] {'('}, 2).Last().TrimEnd(')');

        private static void Process(string name, IEnumerable<TypeDefinition> types, IMetadataTokenProvider invRef)
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
                var key = $"{nativeMethName}__{retType}__{parms}";
                
                
                
                
            }
        }
    }
}

/*
static void InvertNativeRef(ModuleReference invRef, PurgedAssemblies purged,
            IEnumerable<TypeDefinition> types)
        {            
            PurgedAssembly purge;
            if (!purged.TryGetValue(invRefName, out purge))
                purged[invRefName] = purge = new PurgedAssembly(invRefName, new Version("0.0.0.0"));
            var ptypeName = invRefName.Split('.').First();
            PurgedType ptype;
            if (!purge.Types.TryGetValue(ptypeName, out ptype))
                purge.Types[ptypeName] = ptype = new PurgedType(invRefName, ptypeName);
            
                
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