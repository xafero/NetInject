using log4net;
using Mono.Cecil;
using NetInject.Code;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using static NetInject.IOHelper;
using Mono.Cecil.Cil;

namespace NetInject
{
    class Purifier
    {
        const string prefix = "dll_";

        static readonly ILog log = LogManager.GetLogger(typeof(Patcher));

        internal static int Clean(PurifyOptions opts)
        {
            var files = GetAssemblyFiles(opts.WorkDir).Where(f => !f.Contains(".OS.")).ToArray();
            log.Info($"Found {files.Length} files!");
            var resolv = new DefaultAssemblyResolver();
            resolv.AddSearchDirectory(opts.WorkDir);
            var rparam = new ReaderParameters { AssemblyResolver = resolv };
            var wparam = new WriterParameters();
            var halDir = Path.Combine(opts.CodeDir);
            halDir = Directory.CreateDirectory(halDir).FullName;
            foreach (var file in files)
                using (var stream = new MemoryStream(File.ReadAllBytes(file)))
                {
                    var ass = AssemblyDefinition.ReadAssembly(stream, rparam);
                    log.Info($" - '{ass.FullName}'");
                    var methods = CollectPInvokes(ass).ToList();
                    if (!methods.Any())
                        continue;
                    var apiName = ass.Name.Name + ".OS.Api";
                    var apiCS = Path.Combine(halDir, apiName + ".cs");
                    var implName = ass.Name.Name + ".OS.Impl";
                    var implCS = Path.Combine(halDir, implName + ".cs");
                    using (var pinvoke = new CSharpWriter(File.Create(apiCS)))
                    {
                        pinvoke.Usings.Add("System");
                        pinvoke.Usings.Add("System.Runtime.InteropServices.ComTypes");
                        var nsp = new CSharpNamespace(apiName);
                        pinvoke.Namespaces.Add(nsp);
                        var cla = new CSharpClass("IPlatform") { Kind = UnitKind.Interface };
                        foreach (var meth in methods.Select(m => m.Item1))
                            cla.Methods.Add(meth);
                        nsp.Classes.Add(cla);
                        pinvoke.WriteUsings();
                        pinvoke.WriteNamespaces();
                    }
                    string f;
                    var apiCSAss = Compiler.CreateAssembly(apiName, new[] { apiCS });
                    File.Copy(apiCSAss.Location, f = Path.Combine(halDir, apiName + ".dll"), true);
                    log.Info($"   --> '{Path.GetFileName(apiCS)}' ({Path.GetFileName(f)})");
                    var apiAssDef = AssemblyDefinition.ReadAssembly(f, rparam);
                    using (var pinvoke = new CSharpWriter(File.Create(implCS)))
                    {
                        pinvoke.Usings.Add("System");
                        pinvoke.Usings.Add("System.Runtime.InteropServices");
                        pinvoke.Usings.Add("System.Runtime.InteropServices.ComTypes");
                        var nsp = new CSharpNamespace(implName);
                        pinvoke.Namespaces.Add(nsp);
                        var cla = new CSharpClass("Win32Platform");
                        cla.Modifiers.Clear();
                        cla.Modifiers.Add("internal");
                        cla.Bases.Add(apiName + ".IPlatform");
                        nsp.Classes.Add(cla);
                        foreach (var meth in methods.Select(m => m.Item1).SelectMany(DelegateInterface))
                            cla.Methods.Add(meth);
                        cla = new CSharpClass("MonoPlatform");
                        cla.Modifiers.Clear();
                        cla.Modifiers.Add("internal");
                        cla.Bases.Add(apiName + ".IPlatform");
                        nsp.Classes.Add(cla);
                        foreach (var meth in methods.Select(m => m.Item1).SelectMany(ImplementInterface))
                            cla.Methods.Add(meth);
                        cla = new CSharpClass("Platforms");
                        cla.Modifiers.Clear();
                        cla.Modifiers.Add("public");
                        cla.Modifiers.Add("static");
                        cla.Methods.Add(CreateSwitchMethod());
                        nsp.Classes.Add(cla);
                        pinvoke.WriteUsings();
                        pinvoke.WriteNamespaces();
                    }
                    var implCSAss = Compiler.CreateAssembly(implName, new[] { implCS }, new[] { f });
                    File.Copy(implCSAss.Location, f = Path.Combine(halDir, implName + ".dll"), true);
                    log.Info($"   --> '{Path.GetFileName(implCS)}' ({Path.GetFileName(f)})");
                    var implAssDef = AssemblyDefinition.ReadAssembly(f, rparam);
                    var iplat = apiAssDef.GetAllTypes().First(t => t.Name == "IPlatform");
                    var platfs = implAssDef.GetAllTypes().First(t => t.Name == "Platforms");
                    var platGet = platfs.Methods.First();
                    PurifyCalls(methods.Select(m => m.Item2), platGet, iplat);
                    log.InfoFormat("   added '{0}'!", CopyTypeRef(apiCSAss, opts.WorkDir));
                    log.InfoFormat("   added '{0}'!", CopyTypeRef(implCSAss, opts.WorkDir));
                    ass.Write(file, wparam);
                }
            return 0;
        }

        static void PurifyCalls(IEnumerable<MethodDefinition> meths, MethodDefinition platGet, TypeDefinition iplat)
        {
            foreach (var meth in meths)
            {
                meth.Attributes &= ~MethodAttributes.PInvokeImpl;
                meth.IsPreserveSig = false;
                var body = meth.Body ?? (meth.Body = new MethodBody(meth));
                var proc = body.GetILProcessor();
                var redirector = meth.Module.ImportReference(platGet);
                body.Instructions.Add(proc.Create(OpCodes.Call, redirector));
                var target = iplat.Methods.FirstOrDefault(m => m.Name == meth.Name);
                if (target != null)
                {
                    var redirected = meth.Module.ImportReference(target);
                    for (var i = 0; i < meth.Parameters.Count; i++)
                        body.Instructions.Add(proc.Create(OpCodes.Ldarg, i));
                    body.Instructions.Add(proc.Create(OpCodes.Callvirt, redirected));
                }
                if (meth.ReturnType.Name == "void")
                    body.Instructions.Add(proc.Create(OpCodes.Nop));
                body.Instructions.Add(proc.Create(OpCodes.Ret));
            }
        }

        static CSharpMethod CreateSwitchMethod()
        {
            var meth = new CSharpMethod("GetInstance");
            meth.ReturnType = "Api.IPlatform";
            meth.Modifiers.Clear();
            meth.Modifiers.Add("public");
            meth.Modifiers.Add("static");
            meth.Body = "if (Environment.OSVersion.Platform != PlatformID.Win32NT) return new Win32Platform(); return new MonoPlatform();";
            return meth;
        }

        static IEnumerable<CSharpMethod> ImplementInterface(CSharpMethod externMeth)
        {
            var meth = new CSharpMethod(externMeth.Name);
            meth.Modifiers.Clear();
            meth.Modifiers.Add("public");
            meth.Body = $"throw new NotImplementedException(\"Sorry!\");";
            meth.ReturnType = externMeth.ReturnType;
            yield return meth;
        }

        static IEnumerable<CSharpMethod> DelegateInterface(CSharpMethod externMeth)
        {
            var meth = new CSharpMethod(externMeth);
            meth.Name = $"{prefix}{externMeth.Name}";
            yield return meth;
            meth = new CSharpMethod(externMeth.Name);
            meth.Modifiers.Clear();
            meth.Modifiers.Add("public");
            meth.ReturnType = externMeth.ReturnType;
            var preamble = meth.ReturnType.ToLowerInvariant() == "void" ? "" : "return ";
            meth.Body = $"{preamble}{prefix}{externMeth.Name}();";
            yield return meth;
        }

        static IEnumerable<Tuple<CSharpMethod, MethodDefinition>> CollectPInvokes(AssemblyDefinition ass)
        {
            var methodSigs = new List<string>();
            foreach (var type in ass.GetAllTypes())
                foreach (var meth in type.Methods)
                    if (meth.HasPInvokeInfo)
                    {
                        var pinv = meth.PInvokeInfo;
                        var name = meth.Name;
                        if (!Enumerable.Range('A', 26).Contains(name[0]))
                            name = pinv.EntryPoint;
                        if (name.StartsWith("#", StringComparison.InvariantCulture))
                            name = Path.GetFileNameWithoutExtension(pinv.Module.Name) + name.Substring(1);
                        var key = (pinv.Module.Name + "/" + pinv.EntryPoint + "/" + name).ToLowerInvariant();
                        if (methodSigs.Contains(key))
                            continue;
                        var gen = new CSharpMethod(name);
                        gen.ReturnType = meth.ReturnType.Name;
                        var attr = new CSharpAttribute(typeof(DllImportAttribute).Name);
                        attr.Value = $"\"{pinv.Module}\"";
                        attr.Properties["CharSet"] = pinv.ToCharset();
                        attr.Properties["SetLastError"] = pinv.SupportsLastError;
                        var entryPoint = pinv.EntryPoint;
                        attr.Properties["EntryPoint"] = $"\"{entryPoint}\"";
                        gen.Attributes.Add(attr);
                        methodSigs.Add(key);
                        if (Type.GetType(meth.ReturnType.FullName) == null)
                            continue;
                        yield return Tuple.Create(gen, meth);
                    }
        }
    }
}