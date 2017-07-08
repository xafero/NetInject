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
                        pinvoke.Namespace = apiName;
                        pinvoke.Kind = UnitKind.Interface;
                        pinvoke.Name = "IPlatform";
                        pinvoke.Methods = methods;
                        pinvoke.WriteUsings();
                        pinvoke.WriteNamespace();
                    }
                    log.Info($"   --> '{apiCS}'");
                    using (var pinvoke = new CSharpWriter(File.Create(implCS)))
                    {
                        pinvoke.Usings.Add("System");
                        pinvoke.Usings.Add("System.Runtime.InteropServices");
                        pinvoke.Usings.Add("System.Runtime.InteropServices.ComTypes");
                        pinvoke.Namespace = implName;
                        pinvoke.Name = "Win32Platform";
                        pinvoke.Base = apiName + ".IPlatform";
                        foreach (var meth in methods.SelectMany(ImplementInterface))
                            pinvoke.Methods.Add(meth);
                        pinvoke.Methods.Add(CreateSwitchMethod());
                        pinvoke.WriteUsings();
                        pinvoke.WriteNamespace();
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
            var meth = new CSharpMethod("CreateInstance");
            meth.ReturnType = "Api.IPlatform";
            meth.Modifiers.Clear();
            meth.Modifiers.Add("public");
            meth.Modifiers.Add("static");
            meth.Body = "System.Environment.OSVersion.Platform == PlatformID.Win32NT ? new Win32Platform() : null";
            return meth;
        }

        static IEnumerable<CSharpMethod> ImplementInterface(CSharpMethod externMeth)
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