using log4net;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Linq;

using static NetInject.IOHelper;

namespace NetInject
{
    internal class Patcher
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
            foreach (var file in files)
            {
                var ass = AssemblyDefinition.ReadAssembly(file, rparam);
                log.Info($" - '{ass.FullName}'");
                var patches = opts.Patches.Select(p =>
                {
                    var pts = p.Split(new[] { '=' }, 2);
                    var val = pts.Last();
                    pts = pts.First().Split(new[] { ':' }, 2);
                    var type = pts.First();
                    var meth = pts.Last();
                    log.InfoFormat(" ({0}) {1} should return '{2}'...", type, meth, val);
                    return Tuple.Create(type, meth, val);
                });
                PatchCalls(ass, patches.ToArray());
                ass.Write(wparam);
            }
            return 0;
        }

        internal static void PatchCalls(AssemblyDefinition ass,
            params Tuple<string, string, string>[] patches)
        {
            const StringComparison cmp = StringComparison.InvariantCultureIgnoreCase;
            foreach (var type in ass.Modules.SelectMany(m => m.Types))
                foreach (var namesp in patches.Where(p => type.FullName.Equals(p.Item1, cmp)))
                    foreach (var meth in type.Methods.Where(m => m.Name.Equals(namesp.Item2)))
                        InsertReturn(meth.Body.Instructions, namesp.Item3);
        }

        private static void InsertReturn(Collection<Instruction> instrs, string text)
        {
            instrs.Insert(0, Instruction.Create(OpCodes.Ldstr, text));
            instrs.Insert(1, Instruction.Create(OpCodes.Ret));
            log.InfoFormat("Patched and inserted '{0}'!", text);
        }
    }
}