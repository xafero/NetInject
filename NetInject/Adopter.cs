using log4net;
using Mono.Cecil;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using static NetInject.IOHelper;

namespace NetInject
{
    internal static class Adopter
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Adopter));

        internal static int Force(AdoptOptions opts)
        {
            var files = GetAssemblyFiles(opts.WorkDir).ToArray();
            log.Info($"Found {files.Length} files!");
            var resolv = new DefaultAssemblyResolver();
            resolv.AddSearchDirectory(opts.WorkDir);
            var rparam = new ReaderParameters { AssemblyResolver = resolv };
            var wparam = new WriterParameters();
            var @case = StringComparison.InvariantCultureIgnoreCase;
            foreach (var file in files)
                using (var stream = new MemoryStream(File.ReadAllBytes(file)))
                {
                    var ass = AssemblyDefinition.ReadAssembly(stream, rparam);
                    log.Info($" * '{ass.Name.Name}' v{ass.Name.Version}");
                    var isDirty = false;
                    foreach (var mod in ass.Modules)
                    {
                        var refs = mod.AssemblyReferences.ToArray();
                        var wantedRefs = refs.Where(r => r.Name.Equals(opts.Parent, @case));
                        foreach (var @ref in wantedRefs)
                        {
                            log.Info($"     {ToShort(@ref.FullName)}");
                            var refFile = opts.Foster;
                            if (!File.Exists(refFile))
                                refFile = $"{refFile}.dll";
                            var refAss = Assembly.ReflectionOnlyLoadFrom(refFile);
                            var name = refAss.GetName();
                            @ref.Culture = name.CultureName;
                            @ref.Name = name.Name;
                            @ref.PublicKey = name.GetPublicKey();
                            @ref.PublicKeyToken = name.GetPublicKeyToken();
                            @ref.Version = name.Version;
                            log.InfoFormat("     added '{0}'!", ToShort(CopyTypeRef(refAss, opts.WorkDir)));
                            log.Info($"     replaced to => {ToShort(refAss)}");
                            isDirty = true;
                        }
                    }
                    if (!isDirty)
                        continue;
                    ass.Write(file, wparam);
                    log.InfoFormat($"Replaced something in '{ass}'!");
                }
            return 0;
        }

        private static string ToShort(Assembly assembly) => ToShort(assembly.FullName);

        private static string ToShort(string fullName) => fullName.Replace("Culture=neutral, ", "")
            .Replace(", PublicKeyToken=null", "");
    }
}