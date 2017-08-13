using System;
using log4net;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using NetInject.Inspect;
using Newtonsoft.Json;
using static NetInject.IOHelper;
using static NetInject.AssHelper;

namespace NetInject
{
    internal static class Usager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Usager));

        public static int Poll(UsagesOptions opts)
        {
            var report = new DependencyReport();
            var files = GetAssemblyFiles(opts.WorkDir).ToArray();
            Log.Info($"Found {files.Length} file(s)!");
            using (var resolv = new DefaultAssemblyResolver())
            {
                resolv.AddSearchDirectory(opts.WorkDir);
                var rparam = new ReaderParameters {AssemblyResolver = resolv};
                var nativeInsp = new NativeInspector(opts.Assemblies);
                var managedInsp = new ManagedInspector(opts.Assemblies);
                foreach (var file in files)
                    Poll(file, rparam, report, nativeInsp, managedInsp);
            }
            var outFile = Path.GetFullPath("report.json");
            var json = JsonConvert.SerializeObject(report, Formatting.Indented);
            File.WriteAllText(outFile, json, Encoding.UTF8);
            Log.Info($"Report is in '{outFile}'.");
            return 0;
        }

        private static void Poll(string file, ReaderParameters rparam,
            IDependencyReport report, params IInspector[] inspectors)
        {
            using (var ass = ReadAssembly(null, rparam, file))
            {
                if (ass == null)
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