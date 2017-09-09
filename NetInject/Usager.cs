using log4net;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using NetInject.Inspect;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static NetInject.IOHelper;
using static NetInject.AssHelper;
using static NetInject.Cecil.CecilHelper;

namespace NetInject
{
    internal static class Usager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Usager));

        internal static int Poll(IUsageOpts opts)
        {
            var report = new DependencyReport();
            Poll(opts, report);
            var outFile = Path.GetFullPath("report.json");
            var jopts = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                Converters = {new StringEnumConverter()},
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
            var json = JsonConvert.SerializeObject(report, jopts);
            File.WriteAllText(outFile, json, Encoding.UTF8);
            Log.Info($"Report is in '{outFile}'.");
            return 0;
        }

        internal static void Poll(IUsageOpts opts, DependencyReport report)
        {
            var workDir = Path.GetFullPath(opts.WorkDir);
            var files = GetAssemblyFiles(workDir).ToArray();
            Log.Info($"Found {files.Length} file(s) in '{workDir}'!");
            using (var resolv = new DefaultAssemblyResolver())
            {
                resolv.AddSearchDirectory(workDir);
                var rparam = new ReaderParameters {AssemblyResolver = resolv};
                var nativeInsp = new NativeInspector(opts.Assemblies);
                var managedInsp = new ManagedInspector(opts.Assemblies);
                foreach (var file in files)
                    Poll(file, rparam, report, nativeInsp, managedInsp);
            }
        }

        private static void Poll(string file, ReaderParameters rparam,
            IDependencyReport report, params IInspector[] inspectors)
        {
            using (var ass = ReadAssembly(null, rparam, file))
            {
                if (ass == null || IsStandardLib(ass.Name.Name) || IsGenerated(ass))
                    return;
                var founds = new Dictionary<string, int>();
                foreach (var inspector in inspectors)
                {
                    var count = inspector.Inspect(ass, report);
                    if (count < 1)
                        continue;
                    founds[inspector.GetType().Name] = count;
                }
                if (founds.Count < 1)
                    return;
                Log.Info($"'{ass.FullName}' {string.Join(" ", founds)}");
                report.Files.Add(Path.GetFullPath(file));
            }
        }
    }
}