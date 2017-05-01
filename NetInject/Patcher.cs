using log4net;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NetInject.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WildcardMatch;
using static NetInject.IOHelper;

namespace NetInject
{
    class Patcher
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Patcher));

        internal static int Modify(PatchOptions opts)
        {
            var files = GetAssemblyFiles(opts.WorkDir).ToArray();
            log.Info($"Found {files.Length} files!");
            var resolv = new DefaultAssemblyResolver();
            resolv.AddSearchDirectory(opts.WorkDir);
            var rparam = new ReaderParameters { AssemblyResolver = resolv };
            var wparam = new WriterParameters();
            var patches = opts.Patches.Select(p =>
            {
                var pts = p.Split(new[] { '=' }, 2);
                var val = pts.Last();
                pts = pts.First().Split(new[] { ':' }, 2);
                var type = pts.First();
                var meth = pts.Last();
                log.InfoFormat(" ({0}) {1} should use '{2}'...", type, meth, val);
                return Tuple.Create(type, meth, val);
            }).ToArray();
            foreach (var file in files)
                using (var stream = new MemoryStream(File.ReadAllBytes(file)))
                {
                    var ass = AssemblyDefinition.ReadAssembly(stream, rparam);
                    log.Info($" - '{ass.FullName}'");
                    PatchCalls(ass, patches);
                    ass.Write(file, wparam);
                }
            var api = typeof(IInvocationHandler).Assembly;
            var apiName = Path.GetFileName(api.Location);
            var apiLib = Path.Combine(opts.WorkDir, apiName);
            if (File.Exists(apiLib))
                File.Delete(apiLib);
            File.Copy(api.Location, apiLib);
            log.InfoFormat("Added '{0}'!", api);
            return 0;
        }

        internal static void PatchCalls(AssemblyDefinition ass,
            params Tuple<string, string, string>[] patches)
        {
            var handler = typeof(IInvocationHandler);
            foreach (var module in ass.Modules)
            {
                var tref = module.ImportReference(handler);
                var meth = module.ImportReference(handler.GetMethods().First());
                PatchCalls(tref, meth, ass, module.Types, patches);
            }
        }

        const StringComparison cmp = StringComparison.InvariantCultureIgnoreCase;
        const string patcherFieldName = "__patcher__";
        const FieldAttributes patcherFieldAttr = FieldAttributes.Private | FieldAttributes.Static;

        internal static void PatchCalls(TypeReference pRef, MethodReference invoke,
            AssemblyDefinition ass, IEnumerable<TypeDefinition> types,
            params Tuple<string, string, string>[] patches)
        {
            foreach (var type in types)
            {
                if (type.NestedTypes.Count >= 1)
                    PatchCalls(pRef, invoke, ass, type.NestedTypes, patches);
                var fewer = patches.Where(p => p.Item1.WildcardMatch(type.FullName)).ToArray();
                if (fewer.Length < 1)
                    continue;
                var fref = new FieldReference(patcherFieldName, pRef, type);
                var patched = 0;
                foreach (var meth in type.Methods)
                    if (meth.Body != null)
                    {
                        var matches = fewer.Where(f => f.Item2.WildcardMatch(meth.Name)).ToArray();
                        if (matches.Length < 1)
                            continue;
                        var mod = type.Module;
                        var il = meth.Body.Instructions;
                        var i = 0;
                        if (!meth.IsStatic)
                            il.Insert(i++, Instruction.Create(OpCodes.Ldarg_0));
                        il.Insert(i++, Instruction.Create(OpCodes.Nop));
                        il.Insert(i++, Instruction.Create(OpCodes.Ldsfld, fref));
                        if (meth.IsStatic)
                            il.Insert(i++, Instruction.Create(OpCodes.Ldnull));
                        else
                            il.Insert(i++, Instruction.Create(OpCodes.Ldarg_0));
                        il.Insert(i++, Instruction.Create(OpCodes.Ldstr, meth.FullName));
                        var objType = mod.ImportReference(typeof(object));
                        var paramCount = meth.Parameters.Count;
                        il.Insert(i++, Instruction.Create(OpCodes.Ldc_I4, (ushort)paramCount));
                        il.Insert(i++, Instruction.Create(OpCodes.Newarr, objType));
                        var j = 0;
                        foreach (var parm in meth.Parameters)
                        {
                            il.Insert(i++, Instruction.Create(OpCodes.Dup));
                            il.Insert(i++, Instruction.Create(OpCodes.Ldc_I4, (ushort)j++));
                            il.Insert(i++, Instruction.Create(OpCodes.Ldarg, parm));
                            il.Insert(i++, Instruction.Create(OpCodes.Stelem_Ref));
                        }
                        il.Insert(i++, Instruction.Create(OpCodes.Callvirt, invoke));
                        if (!meth.ReturnType.Name.Equals("void", cmp))
                            il.Insert(i++, Instruction.Create(OpCodes.Castclass, meth.ReturnType));
                        il.Insert(i++, Instruction.Create(OpCodes.Ret));
                        patched++;
                    }
                if (patched >= 1)
                    log.InfoFormat("Patched {0} method(s) in '{1}'!", patched, type.FullName);
                else
                    continue;
                var field = type.Fields.FirstOrDefault(f => f.Name.Equals(patcherFieldName, cmp));
                if (field == null)
                {
                    field = new FieldDefinition(patcherFieldName, patcherFieldAttr, pRef);
                    type.Fields.Add(field);
                }
                else
                {
                    field.Name = patcherFieldName;
                    field.Attributes = patcherFieldAttr;
                    field.FieldType = pRef;
                }
            }
        }
    }
}