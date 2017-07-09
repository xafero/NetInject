using log4net;
using Mono.Cecil;
using NetInject.Code;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using static NetInject.IOHelper;

namespace NetInject
{
    class Purifier
    {
        const string prefix = "dll_";

        static readonly ILog log = LogManager.GetLogger(typeof(Patcher));

        internal static int Clean(PurifyOptions opts)
        {
            var files = GetAssemblyFiles(opts.WorkDir).ToArray();
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
                        foreach (var meth in methods)
                            cla.Methods.Add(meth);
                        nsp.Classes.Add(cla);
                        pinvoke.WriteUsings();
                        pinvoke.WriteNamespaces();
                    }
                    log.Info($"   --> '{apiCS}'");
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
                        foreach (var meth in methods.SelectMany(DelegateInterface))
                            cla.Methods.Add(meth);
                        cla = new CSharpClass("MonoPlatform");
                        cla.Modifiers.Clear();
                        cla.Modifiers.Add("internal");
                        cla.Bases.Add(apiName + ".IPlatform");
                        nsp.Classes.Add(cla);
                        foreach (var meth in methods.SelectMany(ImplementInterface))
                            cla.Methods.Add(meth);
                        cla = new CSharpClass("Platforms");
                        cla.Modifiers.Clear();
                        cla.Modifiers.Add("internal");
                        cla.Modifiers.Add("static");
                        cla.Methods.Add(CreateSwitchMethod());
                        nsp.Classes.Add(cla);
                        cla = new CSharpClass("Smuggler");
                        cla.Modifiers.Add("static");
                        foreach (var meth in methods.SelectMany(WrapInterface))
                            cla.Methods.Add(meth);
                        nsp.Classes.Add(cla);
                        pinvoke.WriteUsings();
                        pinvoke.WriteNamespaces();
                    }
                    log.Info($"   --> '{implCS}'");

                    // PurifyCalls(ass);
                    // ass.Write(file, wparam);
                }

            // log.InfoFormat("Added '{0}'!", CopyTypeRef<IInvocationHandler>(opts.WorkDir));
            // log.InfoFormat("Added '{0}'!", CopyTypeRef<InteractiveHandler>(opts.WorkDir));

            return 0;
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

        static IEnumerable<CSharpMethod> WrapInterface(CSharpMethod externMeth)
        {
            var meth = new CSharpMethod(externMeth.Name);
            meth.Modifiers.Clear();
            meth.Modifiers.Add("public");
            meth.Modifiers.Add("static");
            meth.Body = $"Platforms.GetInstance().{externMeth.Name}()";
            meth.ReturnType = externMeth.ReturnType;
            yield return meth;
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
            meth.Body = $"{prefix}{externMeth.Name}()";
            meth.ReturnType = externMeth.ReturnType;
            yield return meth;
        }

        static IEnumerable<CSharpMethod> CollectPInvokes(AssemblyDefinition ass)
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
                        yield return gen;
                    }
        }
    }
}