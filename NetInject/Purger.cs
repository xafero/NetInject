using System.Linq;
using System.IO;
using log4net;
using NetInject.API;
using NetInject.Autofac;
using NetInject.Moq;
using NetInject.IoC;
using Mono.Cecil;
using static NetInject.IOHelper;
using static NetInject.Cecil.WordHelper;
using static NetInject.Compiler;
using static NetInject.CodeConvert;
using NetInject.Inspect;
using System.Collections.Generic;
using System;
using System.Text;
using NetInject.Cecil;
using Noaster.Api;
using Noaster.Dist;
using IType = NetInject.Inspect.IType;
using IMethod = Noaster.Api.IMethod;
using IField = Noaster.Api.IField;
using AType = Noaster.Api.IType;
using Noast = Noaster.Dist.Noaster;

namespace NetInject
{
    internal static class Purger
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Purger));

        private static readonly string ctorName = ".ctor";

        public static readonly string apiSuffix = ".API";
        public static readonly string apiPrefix = "Purge.";

        private static readonly StringComparison cmpa = StringComparison.InvariantCulture;
        private static readonly StringComparer comp = StringComparer.InvariantCultureIgnoreCase;

        private static readonly IParser nativeParser = new Captivator();
        private static readonly MethodDefComparer methCmp = new MethodDefComparer();

        internal static int Invert(InvertOptions opts)
        {
            var report = new DependencyReport();
            Usager.Poll(opts, report);
            Log.Info($"{report.Files.Count} file(s) read for metadata.");
            var tempDir = Path.GetFullPath(opts.TempDir);
            Directory.CreateDirectory(tempDir);
            Log.Info($"Temporary directory is '{tempDir}'.");
            var outDir = Path.GetFullPath(opts.OutputDir);
            Directory.CreateDirectory(outDir);
            Log.Info($"Output directory is '{outDir}'.");
            var generated = GenerateNamespaces(report);
            var files = generated.GroupBy(g => g.Key).ToArray();
            Log.Info($"Generating {files.Length} package(s)...");
            var newLine = Environment.NewLine + Environment.NewLine;
            var names = report.ManagedRefs.Keys.Concat(report.NativeRefs.Keys).Distinct().ToArray();
            var toCompile = new List<string>();
            foreach (var file in files)
            {
                var key = file.Key;
                var meta = CreateMetadata(key, names.First(n => Compare(n, key)));
                var nsps = new object[] { meta }.Concat(file.Select(f => f.Value)).ToArray();
                var code = string.Join(newLine, nsps.Select(n => n.ToString()));
                var filePath = Path.Combine(tempDir, key);
                Log.Info($"'{ToRelativePath(tempDir, filePath)}' [{nsps.Length} namespace(s)]");
                File.WriteAllText(filePath, code, Encoding.UTF8);
                toCompile.Add(filePath);
            }
            IFileCopier copier = new FileCopier();
            var workDir = Path.GetFullPath(opts.WorkDir);
            copier.CopyFolder(workDir, outDir);
            Log.Info($"Compiling {toCompile.Count} package(s)...");
            var toInject = new List<string>();
            foreach (var package in toCompile)
            {
                var bytes = (new FileInfo(package)).Length;
                Log.Info($"'{ToRelativePath(tempDir, package)}' [{bytes} bytes]");
                var name = Path.GetFileNameWithoutExtension(package);
                var ass = CreateAssembly(tempDir, name, new[] { package });
                if (ass == null)
                {
                    Log.Error("Sorry, I could not compile everything ;-(");
                    return -1;
                }
                Log.Info($"  --> '{ass.FullName}'");
                toInject.Add(copier.CopyFile(ass.Location, outDir));
            }
            var oneFileOrMore = report.Files.Count >= 1 && files.Length >= 1;
            if (oneFileOrMore)
            {
                Log.Info($"Processing {report.Files.Count} files...");
                ProcessMarkedFiles(workDir, report, toInject, outDir);
            }
            Log.Info($"Ensuring dependencies in '{outDir}'...");
            if (oneFileOrMore)
            {
                Log.InfoFormat(" added '{0}'!", CopyTypeRef<IVessel>(outDir));
                Log.InfoFormat(" added '{0}'!", CopyTypeRef<DefaultVessel>(outDir));
                Log.InfoFormat(" added '{0}'!", CopyTypeRef<MoqContainer>(outDir));
                Log.InfoFormat(" added '{0}'!", CopyTypeRef<AutofacContainer>(outDir));
            }
            Log.Info("Done.");
            return 0;
        }

        private static IMetadata CreateMetadata(string file, string replaces)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var meta = Noast.Create<IMetadata>(name);
            meta.AddUsing("System.Runtime.Versioning");
            meta.TargetFramework = "4.5";
            meta.Metadata[Defaults.Creator] = typeof(Purger).Namespace;
            meta.Metadata[Defaults.Replaces] = replaces;
            return meta;
        }

        private static void ProcessMarkedFiles(string workDir, IDependencyReport report,
            ICollection<string> injectables, string outDir)
        {
            IRewiring rewriter = new PurgeRewriter();
            using (var resolv = new DefaultAssemblyResolver())
            {
                resolv.AddSearchDirectory(workDir);
                var rparam = new ReaderParameters { AssemblyResolver = resolv };
                var wparam = new WriterParameters();
                var injected = injectables.Select(i => AssemblyDefinition.ReadAssembly(i, rparam)).ToArray();
                foreach (var file in report.Files)
                    using (var stream = new MemoryStream(File.ReadAllBytes(file)))
                    using (var ass = AssemblyDefinition.ReadAssembly(stream, rparam))
                    {
                        Log.Info($"... '{ass.FullName}'");
                        rewriter.Rewrite(ass, injected);
                        var outFile = Path.Combine(outDir, Path.GetFileName(file));
                        ass.Write(outFile, wparam);
                        Log.InfoFormat($" inverted code in '{ToRelativePath(workDir, file)}'!");
                    }
                Array.ForEach(injected, i => i.Dispose());
            }
        }

        private static IEnumerable<KeyValuePair<string, INamespace>> GenerateNamespaces(IDependencyReport report)
        {
            foreach (var pair in report.Units)
            {
                var ass = pair.Value;
                var fileName = $"{ass.Name}.cs";
                var groups = pair.Value.Types.GroupBy(u => u.Value.Namespace);
                foreach (var group in groups)
                {
                    var nspName = Deobfuscate(group.Key);
                    var nsp = Noast.Create<INamespace>(nspName);
                    nsp.AddUsing("System");
                    nsp.AddUsing("System.Text");
                    foreach (var twik in group)
                    {
                        var type = twik.Value;
                        var kind = type.Kind;
                        var name = DerivedClassDeobfuscate(type.Name);
                        if (type.Methods.Any(m => m.Value.Aliases.Any()))
                        {
                            kind = TypeKind.Interface;
                            name = $"I{name}";
                        }
                        switch (kind)
                        {
                            case TypeKind.Interface:
                                var intf = Noast.Create<IInterface>(name, nsp).With(Visibility.Public);
                                GenerateMembers(intf, type);
                                break;
                            case TypeKind.Struct:
                                var stru = Noast.Create<IStruct>(name, nsp).With(Visibility.Public);
                                GenerateMembers(stru, type);
                                break;
                            case TypeKind.Class:
                                var clas = Noast.Create<IClass>(name, nsp).With(Visibility.Public);
                                GenerateMembers(clas, type);
                                break;
                            case TypeKind.Delegate:
                                var dlgt = Noast.Create<IDelegate>(name, nsp).With(Visibility.Public);
                                var invoke = type.Methods.Single();
                                foreach (var parm in invoke.Value.Parameters)
                                    dlgt.AddParameter(parm.Name, Simplify(parm.Type));
                                break;
                            case TypeKind.Enum:
                                var enm = Noast.Create<IEnum>(name, nsp).With(Visibility.Public);
                                foreach (var val in type.Values)
                                    enm.AddValue(val.Value.Name);
                                break;
                        }
                    }
                    yield return new KeyValuePair<string, INamespace>(fileName, nsp);
                }
            }
        }

        private static void GenerateMembers(AType holder, IType type)
        {
            var fldHolder = holder as IHasFields;
            if (fldHolder != null)
                foreach (var fld in type.Fields.Values)
                {
                    var myFld = Noast.Create<IField>(fld.Name);
                    myFld.Type = Simplify(DerivedClassDeobfuscate(fld.Type));
                    fldHolder.Fields.Add(myFld);
                }
            var methHolder = holder as IHasMethods;
            if (methHolder != null)
                foreach (var meth in type.Methods.Values)
                {
                    var myMeth = Noast.Create<IMethod>(meth.Name);
                    var parmIndex = 0;
                    foreach (var parm in meth.Parameters)
                    {
                        var parmName = parm.Name;
                        if (string.IsNullOrWhiteSpace(parmName))
                            parmName = $"parm{parmIndex++}";
                        parmName = ToName(Deobfuscate(parmName));
                        var parmType = Simplify(DerivedClassDeobfuscate(parm.Type));
                        myMeth.AddParameter(parmName, parmType);
                    }
                    myMeth.ReturnType = Simplify(DerivedClassDeobfuscate(meth.ReturnType));
                    methHolder.Methods.Add(myMeth);
                }
        }
    }
}