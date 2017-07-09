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
                    ass.MainModule.AssemblyReferences.Add(apiCSAss.ToRef());
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
                    ass.MainModule.AssemblyReferences.Add(implCSAss.ToRef());

                    // PurifyCalls(methods.Select(m => m.Item2));

                    log.InfoFormat("   added '{0}'!", CopyTypeRef(apiCSAss, opts.WorkDir));
                    log.InfoFormat("   added '{0}'!", CopyTypeRef(implCSAss, opts.WorkDir));
                    ass.Write(file, wparam);
                }
            return 0;
        }

        static void PurifyCalls(IEnumerable<MethodDefinition> meths)
        {
            foreach (var meth in meths)
            {
                meth.Attributes &= ~MethodAttributes.PInvokeImpl;
                meth.IsPreserveSig = false;
                var body = meth.Body ?? (meth.Body = new MethodBody(meth));
                var proc = body.GetILProcessor();
                body.Instructions.Add(proc.Create(OpCodes.Nop));
                body.Instructions.Add(proc.Create(OpCodes.Ldstr, "Hello!"));
                var redirected = meth.Module.ImportReference(typeof(Console).GetMethod("WriteLine", new[] { typeof(string) }));
                body.Instructions.Add(proc.Create(OpCodes.Call, redirected));
                body.Instructions.Add(proc.Create(OpCodes.Nop));
                body.Instructions.Add(proc.Create(OpCodes.Ldc_I4_0));
                body.Instructions.Add(proc.Create(OpCodes.Ret));
            }

            /*
                var meth = new CSharpMethod(externMeth.Name);
                meth.Modifiers.Clear();
                meth.Modifiers.Add("public");
                meth.Modifiers.Add("static");
                meth.Body = $"Platforms.GetInstance().{externMeth.Name}()";
                meth.ReturnType = externMeth.ReturnType;
                yield return meth;
                */
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