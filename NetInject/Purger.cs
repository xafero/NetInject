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
using NetInject.Inspect;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using NetInject.Cecil;
using Noaster.Api;
using Noaster.Dist;
using Noast = Noaster.Dist.Noaster;

namespace NetInject
{
    static class Purger
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(Purger));

        static readonly string iocName = "IoC";
        static readonly string cctorName = ".cctor";
        static readonly string ctorName = ".ctor";

        public static readonly string apiSuffix = ".API";
        public static readonly string apiPrefix = "Purge.";

        static readonly StringComparison cmpa = StringComparison.InvariantCulture;
        static readonly StringComparer comp = StringComparer.InvariantCultureIgnoreCase;

        static readonly IParser nativeParser = new Captivator();
        static readonly MethodDefComparer methCmp = new MethodDefComparer();

        internal static int Invert(InvertOptions opts)
        {
            var report = new DependencyReport();
            Usager.Poll(opts, report);
            Log.Info($"{report.Files.Count} files read for metadata.");
            var tempDir = Path.GetFullPath(opts.TempDir);
            Directory.CreateDirectory(tempDir);
            Log.Info($"Temporary directory is '{tempDir}'.");
            var generated = GenerateNamespaces(report);
            var files = generated.GroupBy(g => g.Key).ToArray();
            Log.Info($"Generating {files.Length} packages...");
            foreach (var file in files)
            {
                var nsps = file.Select(f => f.Value).ToArray();
                var code = string.Join(Environment.NewLine, nsps.Select(n => n.ToString()));
                var filePath = Path.Combine(tempDir, file.Key);
                Log.Info($"'{ToRelativePath(tempDir, filePath)}' [{nsps.Length} namespace(s)]");
                File.WriteAllText(filePath, code, Encoding.UTF8);
            }
            Log.Info($"Done.");

            

            /*var purged = new PurgedAssemblies();
            var filesToWatch = new HashSet<string>();
            using (var resolv = new DefaultAssemblyResolver())
            {
                resolv.AddSearchDirectory(opts.WorkDir);
                var rparam = new ReaderParameters { AssemblyResolver = resolv };
                var wparam = new WriterParameters();
                var files = GetAssemblyFiles(opts.WorkDir).ToArray();
                log.Info($"Found {files.Length} file(s)!");
                foreach (var file in files)
                    using (var stream = new MemoryStream(File.ReadAllBytes(file)))
                    using (var ass = ReadAssembly(stream, rparam, file))
                    {
                        if (ass == null)
                            continue;
                        log.Info($"'{ass.FullName}'");
                        var isFileDirty = false;
                        Invert(ass, opts, wparam, file, ref isFileDirty, purged);
                        if (!isFileDirty)
                            continue;
                        filesToWatch.Add(file);
                    }
                if (filesToWatch.Count >= 1 && purged.Count >= 1)
                {
                    var gens = GenerateCode(purged, opts.TempDir, opts.WorkDir, rparam)
                        .ToDictionary(k => k.Name.Name, v => v);
                    var mappings = purged.GetNativeMappings(apiPrefix).ToArray();
                    foreach (var file in filesToWatch)
                        using (var stream = new MemoryStream(File.ReadAllBytes(file)))
                        using (var ass = AssemblyDefinition.ReadAssembly(stream, rparam))
                        {
                            log.Info($"... '{ass.FullName}'");
                            ReplaceCalls(ass, gens, mappings);
                            ass.Write(file, wparam);
                            log.InfoFormat($"Replaced something in '{ass}'!");
                        }
                }
            }
            if (filesToWatch.Count >= 1 && purged.Count >= 1)
            {
                log.InfoFormat("Added '{0}'!", CopyTypeRef<IVessel>(opts.WorkDir));
                log.InfoFormat("Added '{0}'!", CopyTypeRef<AutofacContainer>(opts.WorkDir));
                log.InfoFormat("Added '{0}'!", CopyTypeRef<MoqContainer>(opts.WorkDir));
                log.InfoFormat("Added '{0}'!", CopyTypeRef<DefaultVessel>(opts.WorkDir));
            }*/
            return 0;
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
                    var nspName = group.Key;
                    var nsp = Noast.Create<INamespace>(nspName);
                    nsp.AddUsing("System");
                    foreach (var twik in group)
                    {
                        var type = twik.Value;
                        switch (type.Kind)
                        {
                            case TypeKind.Interface:
                                Noast.Create<IInterface>(type.Name, nsp);
                                break;
                            case TypeKind.Class:
                                Noast.Create<IClass>(type.Name, nsp);
                                break;
                            case TypeKind.Delegate:
                                Noast.Create<IDelegate>(type.Name, nsp);
                                break;
                            case TypeKind.Enum:
                                Noast.Create<IEnum>(type.Name, nsp);
                                break;
                            case TypeKind.Struct:
                                Noast.Create<IStruct>(type.Name, nsp);
                                break;
                        }
                    }
                    yield return new KeyValuePair<string, INamespace>(fileName, nsp);
                }
            }
        }

        /*      static void Invert(AssemblyDefinition ass, InvertOptions opts,
                  WriterParameters wparam, string file, ref bool isOneDirty,
                  PurgedAssemblies purged)
              {
                  var assRefs = ass.GetAllExternalRefs().ToArray();
                  var assTypes = ass.Modules.SelectMany(m => m.GetTypeReferences()).ToArray();
                  var assMembs = ass.Modules.SelectMany(m => m.GetMemberReferences()).ToArray();
                  var isDirty = false;
                  foreach (var invRef in assRefs.Where(r => opts.Assemblies.Contains(r.Name, comp)))
                  {
                      var assRef = invRef as AssemblyNameReference;
                      if (assRef != null)
                          InvertAssemblyRef(assRef, purged, assTypes, assMembs);
                      var modRef = invRef as ModuleReference;
                      if (modRef != null)
                          InvertNativeRef(modRef, purged, ass.GetAllTypes());
                      // Inject container initializer
                      AddOrReplaceModuleSetup(ass, AddOrReplaceIoc);
                      // Set dirty flag
                      isDirty = true;
                      isOneDirty = true;
                  }
                  if (!isDirty)
                      return;
                  ass.Write(file, wparam);
                  log.InfoFormat($"Purged something in '{ass}'!");
              }
      
              static void InvertAssemblyRef(AssemblyNameReference invRef, PurgedAssemblies purged,
                  TypeReference[] assTypes, MemberReference[] assMembs)
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
                      var myTypeDef = myType.Resolve();
                      if (myTypeDef.IsEnum)
                      {
                          foreach (var enumFld in myTypeDef.Fields.Where(f => !f.Name.EndsWith("__", cmpa)).ToArray())
                              ptype.Values[enumFld.Name] = new PurgedEnumVal(enumFld.Name);
                      }
                      if (myTypeDef.IsClass && myTypeDef.BaseType.FullName == typeof(MulticastDelegate).FullName)
                      {
                          var dlgtSig = myTypeDef.Methods.First(m => m.Name == "Invoke");
                          var dlgtMeth = new PurgedMethod(dlgtSig.Name)
                          {
                              ReturnType = dlgtSig.ReturnType.FullName
                          };
                          foreach (var dlgtParm in dlgtSig.Parameters)
                              dlgtMeth.Parameters.Add(new PurgedParam
                              {
                                  Name = Escape(dlgtParm.Name),
                                  ParamType = dlgtParm.ParameterType.FullName
                              });
                          ptype.Methods[dlgtMeth.Name] = dlgtMeth;
                      }
                      if (!myTypeDef.IsValueType && !myTypeDef.IsSpecialName && !myTypeDef.IsSealed
                          && !myTypeDef.IsRuntimeSpecialName && !myTypeDef.IsPrimitive
                          && !myTypeDef.IsInterface && !myTypeDef.IsArray && (myTypeDef.IsPublic
                          || myTypeDef.IsNestedPublic) && myTypeDef.IsClass)
                      {
                          var virtuals = myTypeDef.Methods.Where(m => m.IsVirtual || m.IsAbstract).ToArray();
                          if (virtuals.Any())
                          {
                              var derived = myType.Module.Assembly.GetDerivedTypes(myType).ToArray();
                              if (derived.Any())
                              {
                                  var overrides = virtuals.Intersect(derived.SelectMany(d => d.Methods)
                                      .Where(m => m.IsAbstract || m.IsVirtual), methCmp).ToArray();
                                  if (overrides.Any())
                                  {
                                      var otypeName = myType.Name;
                                      var otypeFqn = myType.FullName;
                                      PurgedType otype;
                                      if (!purge.Types.TryGetValue(otypeFqn, out otype))
                                          purge.Types[otypeFqn] = otype = new PurgedType(myType.Namespace, otypeName);
                                      HandleAbstractClass(otype, overrides);
                                  }
                              }
                          }
                      }
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
                          var pmd = (MethodDefinition)myMember.Resolve();
                          if (pmethod.Parameters.Count == 0)
                              foreach (var parm in pmd.Parameters)
                              {
                                  var pparm = new PurgedParam
                                  {
                                      Name = Escape(parm.Name),
                                      ParamType = parm.ParameterType.FullName
                                  };
                                  pmethod.Parameters.Add(pparm);
                              }
                          if (pmd.ReturnType.FullName != typeof(void).FullName)
                              pmethod.ReturnType = pmd.ReturnType.FullName;
                      }
                  }
              }
      
              static void InvertNativeRef(ModuleReference invRef, PurgedAssemblies purged,
                  IEnumerable<TypeDefinition> types)
              {
                  log.Info($" - '{invRef}'");
                  var invRefName = Capitalize(Path.GetFileNameWithoutExtension(invRef.Name));
                  PurgedAssembly purge;
                  if (!purged.TryGetValue(invRefName, out purge))
                      purged[invRefName] = purge = new PurgedAssembly(invRefName, new Version("0.0.0.0"));
                  var ptypeName = invRefName.Split('.').First();
                  PurgedType ptype;
                  if (!purge.Types.TryGetValue(ptypeName, out ptype))
                      purge.Types[ptypeName] = ptype = new PurgedType(invRefName, ptypeName);
                  foreach (var type in types)
                      foreach (var meth in type.Methods)
                      {
                          PInvokeInfo pinv;
                          if (!meth.HasPInvokeInfo || invRef != (pinv = meth.PInvokeInfo).Module)
                              continue;
                          var nativeTypeName = invRefName;
                          var nativeMethName = pinv.EntryPoint;
                          PurgedMethod pmethod;
                          if (!ptype.Methods.TryGetValue(nativeMethName, out pmethod))
                              ptype.Methods[nativeMethName] = pmethod = new PurgedMethod(nativeMethName);
                          foreach (var parm in meth.Parameters)
                          {
                              var mparm = new PurgedParam
                              {
                                  Name = Escape(parm.Name),
                                  ParamType = parm.ParameterType.FullName
                              };
                              pmethod.Parameters.Add(mparm);
                          }
                          if (meth.ReturnType.FullName != typeof(void).FullName)
                              pmethod.ReturnType = meth.ReturnType.FullName;
                          pmethod.Refs.Add(meth.FullName);
                      }
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
      
              static IEnumerable<AssemblyDefinition> GenerateCode(PurgedAssemblies purged, string tempDir,
                  string workDir, ReaderParameters rparam)
              {
                  foreach (var purge in purged)
                  {
                      var ass = purge.Value;
                      var fileName = Path.Combine(tempDir, $"{ass.Name}.cs");
                      if (!Directory.Exists(tempDir))
                          Directory.CreateDirectory(tempDir);
                      using (var stream = File.CreateText(fileName))
                      {
                          foreach (var pair in ass.Types.GroupBy(t => t.Value.Namespace))
                          {
                              var nsp = Noast.Create<INamespace>($"{apiPrefix}{pair.Key}");
                              nsp.AddUsing("System");
                              nsp.AddUsing("System.Drawing");
                              foreach (var type in pair)
                              {
                                  var name = type.Value.Name;
                                  if (type.Value.Values.Any())
                                  {
                                      var enu = Noast.Create<IEnum>(name, nsp);
                                      foreach (var val in type.Value.Values)
                                          enu.Values.Add(Noast.Create<IEnumVal>(val.Value.Name));
                                      continue;
                                  }
                                  var meths = type.Value.Methods;
                                  if (meths.FirstOrDefault().Value?.Name == "Invoke")
                                  {
                                      var dlgt = Noast.Create<IDelegate>(name, nsp);
                                      foreach (var dparm in meths.First().Value.Parameters)
                                          dlgt.AddParameter(dparm.Name, dparm.ParamType);
                                      continue;
                                  }
                                  if (!name.StartsWith("I", cmpa))
                                      name = $"I{name}";
                                  var typ = Noast.Create<IInterface>(name, nsp);
                                  foreach (var meth in type.Value.Methods)
                                  {
                                      var mmeth = meth.Value;
                                      var cmeth = Noast.Create<IMethod>(mmeth.Name);
                                      foreach (var parm in meth.Value.Parameters)
                                      {
                                          var mparm = Noast.Create<IParameter>(parm.Name);
                                          mparm.Type = parm.ParamType;
                                          cmeth.Parameters.Add(mparm);
                                      }
                                      if (cmeth.Name == "Dispose")
                                      {
                                          typ.AddImplements("IDisposable");
                                          if (cmeth.Parameters.Count == 0)
                                              continue;
                                      }
                                      if (cmeth.Name == ctorName)
                                      {
                                          var factMethod = Noast.Create<IMethod>($"Create{type.Value.Name}");
                                          factMethod.ReturnType = typ.Name;
                                          foreach (var parm in cmeth.Parameters)
                                              factMethod.Parameters.Add(parm);
                                          var factType = Noast.Create<IInterface>($"I{type.Value.Name}Factory", nsp);
                                          factType.Methods.Add(factMethod);
                                          continue;
                                      }
                                      if (meth.Value.ReturnType != null)
                                          cmeth.ReturnType = meth.Value.ReturnType;
                                      typ.Methods.Add(cmeth);
                                  }
                              }
                              PatchTypes(nsp);
                              stream.Write(nsp);
                          }
                      }
                      var apiName = $"{purge.Value.Name}{apiSuffix}";
                      var apiCSAss = Compiler.CreateAssembly(apiName, new[] { fileName });
                      var apiFile = Path.Combine(workDir, $"{apiName}.dll");
                      File.Copy(apiCSAss.Location, apiFile, true);
                      var apiAssDef = AssemblyDefinition.ReadAssembly(apiFile, rparam);
                      log.Info($"   --> '{apiAssDef}'");
                      yield return apiAssDef;
                  }
              }
      
              static void ReplaceCalls(AssemblyDefinition ass, IDictionary<string, AssemblyDefinition> gens,
                  KeyValuePair<string, string>[] mappings)
              {
                  var membersToDelete = new HashSet<IMetadataTokenProvider>();
                  var myMappings = ToSafeDict(mappings);
                  var types = ass.GetAllTypes().ToArray();
                  var iocType = types.First(t => t.Name == iocName);
                  var iocMeth = iocType.Methods.First(m => m.Name == "GetScope");
                  var resolv = typeof(IVessel).GetMethod("Resolve").MakeGenericMethod(typeof(IDisposable));
                  foreach (var type in types)
                  {
                      foreach (var field in type.Fields)
                      {
                          var genAss = FindGenerated(field.FieldType, gens);
                          if (genAss == null)
                              continue;
                          var newType = genAss.GetAllTypes().FirstOrDefault(
                              t => t.Namespace == $"{apiPrefix}{field.FieldType.Namespace}"
                              && (t.Name == field.FieldType.Name || t.Name == $"I{field.FieldType.Name}"));
                          field.FieldType = type.Module.ImportReference(newType);
                      }
                      foreach (var meth in type.Methods)
                      {
                          if (!meth.HasBody)
                              continue;
                          var ils = meth.Body.GetILProcessor();
                          foreach (var il in meth.Body.Instructions.ToArray())
                          {
                              var opMethDef = il.Operand as MethodDefinition;
                              var opMethRef = il.Operand as MethodReference;
                              if (opMethDef == null && opMethRef == null)
                                  continue;
                              var methType = opMethDef?.DeclaringType ?? opMethRef.DeclaringType;
                              var methName = opMethDef?.Name ?? opMethRef.Name;
                              var methId = opMethDef?.ToString() ?? opMethRef.ToString();
                              var genAss = FindGenerated(methType, gens);
                              TypeDefinition newType = null;
                              MethodDefinition newMeth = null;
                              if (genAss == null)
                              {
                                  var nativeResolv = nativeParser.Parse(il, methId, myMappings, gens);
                                  newType = nativeResolv?.NewType;
                                  newMeth = nativeResolv?.NewMethod;
                              }
                              if (genAss == null && il.OpCode == OpCodes.Ldftn)
                              {
                                  foreach (var parm in opMethDef.Parameters)
                                  {
                                      var parmAss = FindGenerated(parm.ParameterType, gens);
                                      if (parmAss == null)
                                          continue;
                                      var parmType = FindType(parmAss, parm.ParameterType);
                                      if (parmType == null)
                                          continue;
                                      parm.ParameterType = type.Module.ImportReference(parmType);
                                  }
                              }
                              else if (genAss != null && il.OpCode == OpCodes.Newobj)
                              {
                                  newType = genAss.GetAllTypes().FirstOrDefault(
                                      t => t.Namespace == $"{apiPrefix}{methType.Namespace}"
                                      && (t.Name == $"I{methType.Name}Factory" || t.Name == methType.Name));
                                  newMeth = newType?.Methods.FirstOrDefault(m => m.Name == $"Create{methType.Name}" || m.Name == ctorName);
                              }
                              else if (genAss != null && (il.OpCode == OpCodes.Call || il.OpCode == OpCodes.Callvirt))
                              {
                                  newType = FindType(genAss, methType);
                                  newMeth = newType.Methods.FirstOrDefault(m => m.Name == methName);
                              }
                              if (newMeth == null || newType == null)
                                  continue;
                              log.Info($"   ::> '{newMeth}'");
                              if (newMeth.Name == ctorName)
                              {
                                  il.Operand = type.Module.ImportReference(newMeth);
                                  continue;
                              }
                              var isStatic = opMethDef?.IsStatic ?? !opMethRef.HasThis;
                              if (methName == ctorName || isStatic)
                              {
                                  var stepsBack = newMeth.Parameters.Count;
                                  var ilStart = il.GoBack(stepsBack);
                                  ils.InsertBefore(ilStart, ils.Create(OpCodes.Call, type.Module.ImportReference(iocMeth)));
                                  var impResolv = (GenericInstanceMethod)type.Module.ImportReference(resolv);
                                  impResolv.GenericArguments[0] = type.Module.ImportReference(newType);
                                  ils.InsertBefore(ilStart, ils.Create(OpCodes.Callvirt, impResolv));
                              }
                              membersToDelete.Add((IMetadataTokenProvider)il.Operand);
                              ils.Replace(il, ils.Create(OpCodes.Callvirt, type.Module.ImportReference(newMeth)));
                          }
                      }
                  }
                  foreach (var member in nativeParser.MembersToDelete.Concat(membersToDelete))
                  {
                      var methDef = member as MethodDefinition;
                      ass.Remove(methDef?.PInvokeInfo?.Module);
                      methDef?.DeclaringType?.Methods.Remove(methDef);
                      var methRef = member as MethodReference;
                      ass.Remove(methRef?.DeclaringType?.Scope as AssemblyNameReference);
                  }
              }
      
              static TypeDefinition FindType(AssemblyDefinition ass, TypeReference type) => ass.GetAllTypes().FirstOrDefault(
                  t => t.Namespace == $"{apiPrefix}{type.Namespace}" && t.Name == $"I{type.Name}");
      
              static AssemblyDefinition FindGenerated(TypeReference origType, IDictionary<string, AssemblyDefinition> gens)
              {
                  var origAss = origType.Scope as AssemblyNameReference;
                  if (origAss == null)
                      return null;
                  AssemblyDefinition genAss;
                  if (!gens.TryGetValue($"{origAss.Name}{apiSuffix}", out genAss))
                      return null;
                  return genAss;
              }
      
              static void PatchTypes(INamespace nsp)
              {
                  var names = nsp.Members.OfType<INamed>().Select(c => c.Name).Distinct().ToArray();
                  foreach (var dlgt in nsp.Members.OfType<IDelegate>())
                      PatchTypes(dlgt, nsp.Name, names);
                  foreach (var cla in nsp.Members.OfType<IClass>())
                      foreach (var meth in cla.Methods)
                          PatchTypes(meth, nsp.Name, names);
              }
      
              static void PatchTypes(IHasParameters parms, string nspName, IEnumerable<string> names)
              {
                  foreach (var dparm in parms.Parameters)
                  {
                      var dparamNsp = dparm.Type.Substring(0, dparm.Type.LastIndexOf('.'));
                      if ((apiPrefix + dparamNsp) != nspName)
                          continue;
                      var dparamName = 'I' + dparm.Type.Substring(dparamNsp.Length).TrimStart('.');
                      if (!names.Contains(dparamName))
                          continue;
                      dparm.Type = $"{nspName}.{dparamName}";
                  }
              }
      
              static void HandleAbstractClass(PurgedType fake, MethodDefinition[] overrides)
              {
                  foreach (var overrid in overrides)
                  {
                      PurgedMethod pmethod;
                      if (!fake.Methods.TryGetValue(overrid.FullName, out pmethod))
                          fake.Methods[overrid.FullName] = pmethod = new PurgedMethod(overrid.Name);
                      if (pmethod.Parameters.Count == 0)
                          foreach (var parm in overrid.Parameters)
                          {
                              var pparm = new PurgedParam
                              {
                                  Name = Escape(parm.Name),
                                  ParamType = parm.ParameterType.FullName
                              };
                              pmethod.Parameters.Add(pparm);
                          }
                      if (overrid.ReturnType.FullName != typeof(void).FullName)
                          pmethod.ReturnType = overrid.ReturnType.FullName;
                  }
              }*/
    }
}