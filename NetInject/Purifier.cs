using log4net;
using Mono.Cecil;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mono.Cecil.Cil;

using Noaster.Api;
using Noaster.Dist;

using Noast = Noaster.Dist.Noaster;

using static NetInject.IOHelper;
using static NetInject.CodeConvert;

namespace NetInject
{
    static class Purifier
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
                    using (var pinvoke = File.CreateText(apiCS))
                    {
                        var nsp = Noast.Create<INamespace>(apiName);
                        nsp.AddUsing("System");
                        nsp.AddUsing("System.Runtime.InteropServices");
                        nsp.AddUsing("System.Runtime.InteropServices.ComTypes");
                        nsp.AddUsing("System.Text");
                        var cla = Noast.Create<IInterface>("IPlatform", nsp);
                        foreach (var meth in methods.Select(m => m.Item1))
                            cla.Methods.Add(meth);
                        pinvoke.Write(nsp);
                    }
                    string f;
                    var apiCSAss = Compiler.CreateAssembly(apiName, new[] { apiCS });
                    File.Copy(apiCSAss.Location, f = Path.Combine(halDir, apiName + ".dll"), true);
                    log.Info($"   --> '{Path.GetFileName(apiCS)}' ({Path.GetFileName(f)})");
                    var apiAssDef = AssemblyDefinition.ReadAssembly(f, rparam);
                    using (var pinvoke = File.CreateText(implCS))
                    {
                        var nsp = Noast.Create<INamespace>(implName);
                        nsp.AddUsing("System");
                        nsp.AddUsing("System.Runtime.InteropServices");
                        nsp.AddUsing("System.Runtime.InteropServices.ComTypes");
                        nsp.AddUsing("System.Text");
                        var cla = Noast.Create<IClass>("Win32Platform", nsp).With(Visibility.Internal);
                        cla.AddImplements(apiName + ".IPlatform");
                        foreach (var meth in methods.Select(m => m.Item1).SelectMany(DelegateInterface))
                            cla.Methods.Add(meth);
                        cla = Noast.Create<IClass>("MonoPlatform", nsp).With(Visibility.Internal);
                        cla.AddImplements(apiName + ".IPlatform");
                        foreach (var meth in methods.Select(m => m.Item1).SelectMany(ImplementInterface))
                            cla.Methods.Add(meth);
                        cla = Noast.Create<IClass>("Platforms", nsp).With(Visibility.Public).With(Modifier.Static);
                        cla.Methods.Add(CreateSwitchMethod());
                        pinvoke.Write(nsp);
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
            resolv.Dispose();
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

        static IMethod CreateSwitchMethod()
        {
            var meth = Noast.Create<IMethod>("GetInstance").With(Visibility.Public).With(Modifier.Static);
            meth.ReturnType = "Api.IPlatform";
            meth.Body = "if (Environment.OSVersion.Platform == PlatformID.Win32NT) return new Win32Platform(); return new MonoPlatform();";
            return meth;
        }

        static IEnumerable<IMethod> ImplementInterface(IMethod externMeth)
        {
            var meth = Noast.Create<IMethod>(externMeth.Name).With(Visibility.Public);
            meth.ReturnType = externMeth.ReturnType;
            // meth.Body = $"throw new NotImplementedException(\"Sorry!\");";
            if (meth.ReturnType.ToLowerInvariant() == "void")
                meth.Body = "return;";
            else
                meth.Body = $"return default({meth.ReturnType});";
            foreach (var parm in externMeth.Parameters)
                meth.Parameters.Add(parm);
            yield return meth;
        }

        static IEnumerable<IMethod> DelegateInterface(IMethod externMeth)
        {
            var meth = Noast.Create<IMethod>($"{prefix}{externMeth.Name}");   // TODO: method cloning?! externMeth
            yield return meth;
            meth = Noast.Create<IMethod>(externMeth.Name).With(Visibility.Public);
            meth.ReturnType = externMeth.ReturnType;
            var preamble = meth.ReturnType.ToLowerInvariant() == "void" ? "" : "return ";
            var parms = string.Join(", ", externMeth.Parameters.Select(p => (p.IsRef() ? "ref " : "") + p.Name));
            meth.Body = $"{preamble}{prefix}{externMeth.Name}({parms});";
            foreach (var parm in externMeth.Parameters)
                meth.Parameters.Add(parm);
            yield return meth;
        }

        static IEnumerable<Tuple<IMethod, MethodDefinition>> CollectPInvokes(AssemblyDefinition ass)
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
                        var gen = Noast.Create<IMethod>(name);
                        gen.ReturnType = meth.ReturnType.Name;
                        var attr = Noast.Create<IAttribute>(typeof(DllImportAttribute).Name);
                        attr.Values.Add($"\"{pinv.Module}\"");
                        attr.Properties["CharSet"] = pinv.ToCharset();
                        attr.Properties["SetLastError"] = pinv.SupportsLastError;
                        var entryPoint = pinv.EntryPoint;
                        attr.Properties["EntryPoint"] = $"\"{entryPoint}\"";
                        gen.Attributes.Add(attr);
                        foreach (var parm in meth.Parameters)
                            AddParam(parm, gen);
                        methodSigs.Add(key);
                        if (Type.GetType(meth.ReturnType.FullName) == null)
                            continue;
                        if (meth.Parameters.Any(p => Type.GetType(p.ParameterType.FullName) == null))
                            continue;
                        yield return Tuple.Create(gen, meth);
                    }
        }

        static void AddParam(ParameterDefinition parm, IMethod gen)
        {
            var name = parm.Name;
            if (!Enumerable.Range('A', 26).Contains(name.FirstOrDefault()))
                name = "p" + gen.Parameters.Count;
            var par = Noast.Create<IParameter>(name);
            par.Type = Simplify(parm.ParameterType.Name);
            if (parm.ParameterType.IsByReference) par.Modifier = ParamModifier.Ref;
            gen.Parameters.Add(par);
        }
    }
}
