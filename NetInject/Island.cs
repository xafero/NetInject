using System;
using System.Linq;
using log4net;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static NetInject.IOHelper;
using static NetInject.Searcher;

namespace NetInject
{
    internal static class Island
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Island));

        internal static int Replace(IsleOptions opts)
        {
            var term = new SearchMask(opts.Term);
            var files = GetAssemblyFiles(opts.WorkDir).ToArray();
            log.Info($"Found {files.Length} files!");
            var resolv = new DefaultAssemblyResolver();
            resolv.AddSearchDirectory(opts.WorkDir);
            var rparam = new ReaderParameters { AssemblyResolver = resolv };
            var wparam = new WriterParameters();
            foreach (var file in files)
                using (var stream = IntoMemory(file))
                {
                    var tuples = FindInstructions(stream, rparam, term).ToArray();
                    if (tuples.Length < 1)
                        continue;
                    bool isDirty = false;
                    var byMethod = tuples.GroupBy(t => t.Item1.Method).ToArray();
                    foreach (var tuple in byMethod)
                    {
                        var meth = tuple.Key;
                        var instrs = tuple.Select(t => t.Item2).ToArray();
                        Console.WriteLine(meth);
                        var proc = meth.Body.GetILProcessor();
                        foreach (var instr in instrs)
                        {
                            Console.WriteLine($"   {instr}");
                            Type type;
                            var patch = CreateOperation(proc, opts.Patch, out type);
                            Console.Write($" > {patch} (y/n)? ");
                            var ask = Console.ReadLine();
                            if (!ask.Equals("y", StringComparison.InvariantCultureIgnoreCase))
                                continue;
                            proc.Replace(instr, patch);
                            log.InfoFormat(" added '{0}'!", CopyTypeRef(type.Assembly, opts.WorkDir));
                            isDirty = true;
                        }
                    }
                    if (!isDirty)
                        continue;
                    var ass = tuples.First().Item1.Method.Module.Assembly;
                    ass.Write(file, wparam);
                    log.InfoFormat($"Replaced something in '{ass}'!");
                }
            return 0;
        }

        private static Instruction CreateOperation(ILProcessor proc, string patch, out Type type)
        {
            var parts = patch.Split(':');
            var opcode = parts.First();
            var oparg = parts.Last();
            var code = typeof(OpCodes).GetFields().FirstOrDefault(f => f.Name.Equals(
                opcode, StringComparison.InvariantCultureIgnoreCase))?.GetValue(null);
            parts = oparg.Split('@');
            var ass = parts.First();
            var arg = parts.Last();
            type = Type.GetType($"{arg}, {ass}");
            var constr = type.GetConstructors().First();
            var final = proc.Body.Method.Module.ImportReference(constr);
            return proc.Create((OpCode)code, final);
        }
    }
}