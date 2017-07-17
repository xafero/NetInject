using log4net;
using System.Linq;
using Mono.Cecil;
using System.IO;
using NetInject.API;
using System;
using Mono.Cecil.Cil;

namespace NetInject
{
    static class Weaver
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Weaver));

        internal static int Web(WeaveOptions opts)
        {
            using (var resolv = new DefaultAssemblyResolver())
            {
                resolv.AddSearchDirectory(opts.WorkDir);
                var rparam = new ReaderParameters { AssemblyResolver = resolv };
                var wparam = new WriterParameters();
                using (var stream = new MemoryStream(File.ReadAllBytes(opts.Binary + ".dll")))
                {
                    var ass = AssemblyDefinition.ReadAssembly(stream, rparam);
                    log.Info($"Using binary '{ass.Name.Name}' v{ass.Name.Version}");
                    var types = ass.GetAllTypes().Where(t => !string.IsNullOrWhiteSpace(t.Namespace)).ToArray();
                    log.Info($"Found {types.Length} type(s) in binary!");
                    foreach (var type in types)
                    {
                        log.Info($" * '{type.FullName}'");
                        var file = Path.Combine(opts.WorkDir, type.GetAttribute<AssemblyAttribute>().Single().FileName);
                        using (var mem = new MemoryStream(File.ReadAllBytes(file)))
                        {
                            var dest = AssemblyDefinition.ReadAssembly(mem, rparam);
                            var isDirty = false;
                            log.Info($"   --> '{dest.Name.Name}' v{dest.Name.Version}");
                            var destType = dest.GetAllTypes().First(t => t.FullName == type.FullName);
                            foreach (var meth in type.Methods)
                            {
                                var bestMatch = destType.Methods.FirstOrDefault(m => m.ToString() == meth.ToString())
                                    ?? destType.Methods.FirstOrDefault(m => m.FullName == meth.FullName)
                                    ?? destType.Methods.FirstOrDefault(m => m.Name == meth.Name);
                                log.Info($"       --> '{bestMatch}");
                                bestMatch.RemovePInvoke();
                                ReplaceBody(bestMatch, meth);
                                isDirty = true;
                            }
                            if (!isDirty)
                                continue;
                            dest.Write(file, wparam);
                            log.InfoFormat($"Replaced something in '{dest}'!");
                        }
                    }
                }
                return 0;
            }
        }

        static void ReplaceBody(MethodDefinition dest, MethodDefinition source)
        {
            var mod = dest.Body.Method.Module;
            var proc = dest.Body.GetILProcessor();
            foreach (var srcInstr in source.Body.Instructions)
            {
                var opcode = srcInstr.OpCode;
                var operand = srcInstr.Operand;
                Instruction instr;
                var fieldRef = operand as FieldReference;
                if (fieldRef != null)
                    instr = proc.Create(opcode, mod.ImportReference(fieldRef));
                else
                {
                    instr = proc.Create(srcInstr.OpCode);
                    instr.Operand = operand;
                }
                proc.Append(instr);
            }
        }
    }
}