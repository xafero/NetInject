using System;
using log4net;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Newtonsoft.Json;
using static NetInject.IOHelper;
using static NetInject.AssHelper;

namespace NetInject
{
    public class DependencyReport
    {
        public ISet<string> FoundFiles { get; set; }
        public IDictionary<string, ISet<string>> NativeReferences { get; set; }
        public IDictionary<string, ISet<string>> ManagedReferences { get; set; }

        public DependencyReport()
        {
            FoundFiles = new SortedSet<string>();
            NativeReferences = new SortedDictionary<string, ISet<string>>();
            ManagedReferences = new SortedDictionary<string, ISet<string>>();
        }
    }

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
                foreach (var file in files)
                    Poll(file, rparam, report);
            }
            Console.WriteLine(JsonConvert.SerializeObject(report, Formatting.Indented));
            return 0;
        }

        private static void Poll(string file, ReaderParameters rparam, DependencyReport report)
        {
            using (var ass = ReadAssembly(null, rparam, file))
            {
                if (ass == null)
                    return;
                var natives = 0;
                var manageds = 0;
                foreach (var nativeRef in ass.Modules.SelectMany(m => m.ModuleReferences))
                {
                    var key = NormalizeNativeRef(nativeRef);
                    ISet<string> list;
                    if (!report.NativeReferences.TryGetValue(key, out list))
                        report.NativeReferences[key] = list = new SortedSet<string>();
                    list.Add(ass.FullName);
                    natives++;
                }
                foreach (var assRef in ass.Modules.SelectMany(m => m.AssemblyReferences))
                {
                    var key = assRef.Name;
                    if (key == "mscorlib" || key == "System" || key == "System.Core" || key == "Microsoft.CSharp")
                        continue;
                    ISet<string> list;
                    if (!report.ManagedReferences.TryGetValue(key, out list))
                        report.ManagedReferences[key] = list = new SortedSet<string>();
                    list.Add(ass.FullName);
                    manageds++;
                }
                if (natives < 1 && manageds < 1)
                    return;
                Log.Info($"'{ass.FullName}' ({natives} native & {manageds} managed refs)");
                report.FoundFiles.Add(Path.GetFullPath(file));
            }
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