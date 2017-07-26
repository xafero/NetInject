using System.Linq;
using System.IO;

using log4net;
using NetInject.API;
using NetInject.Autofac;
using NetInject.Moq;
using NetInject.IoC;
using Mono.Cecil;
using Mono.Cecil.Cil;

using static NetInject.IOHelper;
using static NetInject.AssHelper;

using MethodAttr = Mono.Cecil.MethodAttributes;
using TypeAttr = Mono.Cecil.TypeAttributes;
using FieldAttr = Mono.Cecil.FieldAttributes;
using System.Text;
using NetInject.Purge;
using System;
using NetInject.Code;

namespace NetInject
{
    static class Purger
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Purger));

        static readonly string iocName = "IoC";
        static readonly string cctorName = ".cctor";

        internal static int Invert(InvertOptions opts)
        {
            var purged = new PurgedAssemblies();
            var isDirty = false;
            using (var resolv = new DefaultAssemblyResolver())
            {
                resolv.AddSearchDirectory(opts.WorkDir);
                var rparam = new ReaderParameters { AssemblyResolver = resolv };
                var wparam = new WriterParameters();
                var files = GetAssemblyFiles(opts.WorkDir).ToArray();
                log.Info($"Found {files.Length} file(s)!");
                foreach (var file in files)
                    using (var stream = new MemoryStream(File.ReadAllBytes(file)))
                    {
                        var ass = AssemblyDefinition.ReadAssembly(stream, rparam);
                        log.Info($"'{ass.FullName}'");
                        Invert(ass, opts, wparam, file, ref isDirty, purged);
                    }
            }
            if (isDirty)
            {
                GenerateCode(purged);
                // TODO
                log.InfoFormat("Added '{0}'!", CopyTypeRef<IVessel>(opts.WorkDir));
                log.InfoFormat("Added '{0}'!", CopyTypeRef<AutofacContainer>(opts.WorkDir));
                log.InfoFormat("Added '{0}'!", CopyTypeRef<MoqContainer>(opts.WorkDir));
                log.InfoFormat("Added '{0}'!", CopyTypeRef<DefaultVessel>(opts.WorkDir));
            }
            return 0;
        }

        static void Invert(AssemblyDefinition ass, InvertOptions opts,
            WriterParameters wparam, string file, ref bool isOneDirty,
            PurgedAssemblies purged)
        {
            var assRefs = ass.Modules.SelectMany(m => m.AssemblyReferences).ToArray();
            var assTypes = ass.Modules.SelectMany(m => m.GetTypeReferences()).ToArray();
            var assMembs = ass.Modules.SelectMany(m => m.GetMemberReferences()).ToArray();
            var isDirty = false;
            foreach (var invRef in assRefs.Where(r => opts.Assemblies.Contains(r.Name)))
            {
                log.Info($" - '{invRef.FullName}'");
                PurgedAssembly purge;
                if (!purged.TryGetValue(invRef.FullName, out purge))
                    purged[invRef.FullName] = purge = new PurgedAssembly(invRef.Name, invRef.Version);
                var myTypes = assTypes.Where(t => ContainsType(invRef, t)).ToArray();
                var myMembers = assMembs.Where(m => ContainsMember(invRef, m)).GroupBy(m => m.DeclaringType).ToArray();
                foreach (var myType in myTypes)
                {
                    PurgedType ptype;
                    if (!purge.Types.TryGetValue(myType.FullName, out ptype))
                        purge.Types[myType.FullName] = ptype = new PurgedType(myType.Namespace, myType.Name);
                }
                foreach (var myPair in myMembers)
                {
                    var myType = myPair.Key;
                    PurgedType ptype;
                    if (!purge.Types.TryGetValue(myType.FullName, out ptype))
                        purge.Types[myType.FullName] = ptype = new PurgedType(myType.Namespace, myType.Name);
                    foreach (var myMember in myPair)
                    {
                        PurgedMethod pmethod;
                        if (!ptype.Methods.TryGetValue(myMember.FullName, out pmethod))
                            ptype.Methods[myMember.FullName] = pmethod = new PurgedMethod(myMember.Name);
                    }
                }
                // Inject container initializer
                AddOrReplaceModuleSetup(ass, AddOrReplaceIoc);
                // Set dirty flag
                isDirty = true;
                isOneDirty = true;
            }
            if (!isDirty)
                return;
            ass.Write(file, wparam);
            log.InfoFormat($"Replaced something in '{ass}'!");
        }

        static bool ContainsType(AssemblyNameReference assRef, TypeReference typRef)
          => assRef.FullName == (typRef.Scope as AssemblyNameReference)?.FullName;

        static bool ContainsMember(AssemblyNameReference assRef, MemberReference mbmRef)
          => ContainsType(assRef, mbmRef.DeclaringType);

        static void AddOrReplaceIoc(ILProcessor il)
        {
            var mod = il.Body.Method.DeclaringType.Module;
            var myNamespace = mod.Types.Select(t => t.Namespace)
                .Where(n => !string.IsNullOrWhiteSpace(n)).OrderBy(n => n.Length).First();
            var attr = TypeAttr.Class | TypeAttr.Public | TypeAttr.Sealed | TypeAttr.Abstract | TypeAttr.BeforeFieldInit;
            var objBase = mod.ImportReference(typeof(object));
            var type = new TypeDefinition(myNamespace, iocName, attr, objBase);
            var oldType = mod.Types.FirstOrDefault(t => t.FullName == type.FullName);
            if (oldType != null)
                mod.Types.Remove(oldType);
            mod.Types.Add(type);
            var vesselRef = mod.ImportReference(typeof(IVessel));
            var fieldAttr = FieldAttr.Static | FieldAttr.Private;
            var contField = new FieldDefinition("scope", fieldAttr, vesselRef);
            type.Fields.Add(contField);
            var getAttrs = MethodAttr.Static | MethodAttr.Public | MethodAttr.SpecialName | MethodAttr.HideBySig;
            var getMethod = new MethodDefinition($"GetScope", getAttrs, vesselRef);
            type.Methods.Add(getMethod);
            var gmil = getMethod.Body.GetILProcessor();
            gmil.Append(gmil.Create(OpCodes.Ldsfld, contField));
            gmil.Append(gmil.Create(OpCodes.Ret));
            var voidRef = mod.ImportReference(typeof(void));
            var constrAttrs = MethodAttr.Static | MethodAttr.SpecialName | MethodAttr.RTSpecialName
                | MethodAttr.Private | MethodAttr.HideBySig;
            var constr = new MethodDefinition(cctorName, constrAttrs, voidRef);
            type.Methods.Add(constr);
            var cil = constr.Body.GetILProcessor();
            var multiMeth = typeof(DefaultVessel).GetConstructors().First();
            var multiRef = mod.ImportReference(multiMeth);
            cil.Append(cil.Create(OpCodes.Newobj, multiRef));
            cil.Append(cil.Create(OpCodes.Stsfld, contField));
            cil.Append(cil.Create(OpCodes.Ret));
            il.Append(il.Create(OpCodes.Call, getMethod));
            il.Append(il.Create(OpCodes.Pop));
            il.Append(il.Create(OpCodes.Ret));
        }

        static void GenerateCode(PurgedAssemblies purged)
        {
            foreach (var purge in purged)
            {
                var ass = purge.Value;
                var fileName = ass.Name + ".cs";
                using (var stream = File.Create(fileName))
                using (var writer = new CSharpWriter(stream))
                {
                    writer.Usings.Add("System");
                    var pairs = ass.Types.GroupBy(t => t.Value.Namespace);
                    foreach (var pair in pairs)
                    {
                        var nsp = new CSharpNamespace($"Purge.{pair.Key}");
                        foreach (var type in pair)
                        {
                            var name = type.Value.Name;
                            if (!name.StartsWith("I"))
                                name = $"I{name}";
                            var typ = new CSharpClass(name)
                            {
                                Kind = UnitKind.Interface
                            };
                            foreach (var meth in type.Value.Methods)
                            {
                                var mmeth = meth.Value;
                                var cmeth = new CSharpMethod(mmeth.Name);
                                if (cmeth.Name == "Dispose")
                                {
                                    typ.Bases.Add("IDisposable");
                                    continue;
                                }
                                if (cmeth.Name == ".ctor")
                                {
                                    var factMethod = new CSharpMethod($"Create{type.Value.Name}")
                                    {
                                        ReturnType = typ.Name
                                    };
                                    var factType = new CSharpClass($"I{type.Value.Name}Factory")
                                    {
                                        Kind = UnitKind.Interface
                                    };
                                    factType.Methods.Add(factMethod);
                                    nsp.Classes.Add(factType);
                                    continue;
                                }
                                typ.Methods.Add(cmeth);
                            }
                            nsp.Classes.Add(typ);
                        }
                        writer.Namespaces.Add(nsp);
                    }
                    writer.WriteUsings();
                    writer.WriteNamespaces();
                }

                System.Diagnostics.Process.Start(fileName);
            }
        }
    }
}