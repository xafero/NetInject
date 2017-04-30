using log4net;
using Mono.Cecil;
using System.IO;
using System.Linq;

using static NetInject.IOHelper;
using static NetInject.AssHelper;

namespace NetInject
{
    internal class Signer
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Signer));

        internal static int Unsign(UnsignOptions opts)
        {
            var inputFiles = GetAllFiles(opts.InputDir).ToArray();
            log.Info($"Found {inputFiles.Length} files!");
            Directory.CreateDirectory(opts.OutputDir);
            var resolv = new DefaultAssemblyResolver();
            resolv.AddSearchDirectory(opts.InputDir);
            var rparam = new ReaderParameters { AssemblyResolver = resolv };
            var wparam = new WriterParameters();
            foreach (var input in inputFiles)
            {
                var relative = input.Substring(opts.InputDir.Length)
                    .TrimStart(Path.DirectorySeparatorChar);
                var outFile = Path.Combine(opts.OutputDir, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(outFile));
                var ext = Path.GetExtension(input.ToLowerInvariant());
                if (ext != ".dll" && ext != ".exe")
                {
                    if (File.Exists(outFile))
                    {
                        EnsureWritable(outFile);
                        File.Delete(outFile);
                    }
                    File.Copy(input, outFile);
                    EnsureWritable(outFile);
                    continue;
                }
                var ass = AssemblyDefinition.ReadAssembly(input, rparam);
                log.Info($" - '{ass.FullName}'");
                log.Info($"   --> '{outFile}'");
                RemoveSigning(ass, opts.UnsignKeys);
                RemoveSignedRefs(ass.Modules, opts.UnsignKeys);
                ass.Write(outFile, wparam);
            }
            return 0;
        }
    }
}