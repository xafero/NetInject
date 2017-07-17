using log4net;
using System.Linq;
using Mono.Cecil;
using System.IO;
using static NetInject.IOHelper;
using static NetInject.AssHelper;
using NetInject.API;

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
                        var targetFile = type.GetAttribute<AssemblyAttribute>().Single().FileName;
                        using (var mem = new MemoryStream(File.ReadAllBytes(Path.Combine(opts.WorkDir, targetFile))))
                        {
                            var dest = AssemblyDefinition.ReadAssembly(mem, rparam);
                            log.Info($"   --> '{dest.Name.Name}' v{dest.Name.Version}");





                        }
                    }
                }
                return 0;
            }
        }
    }
}