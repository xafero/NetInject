using Mono.Cecil;
using NetInject.Cecil;
using NetInject.Inspect;
using Noaster.Api;
using System.Collections.Generic;
using System.Linq;

using AMA = System.Reflection.AssemblyMetadataAttribute;
using Mono.Cecil.Cil;

namespace NetInject
{
    internal interface IRewiring
    {
        void Rewrite(AssemblyDefinition ass, AssemblyDefinition[] inserts);
    }

    internal interface IRewiring<T> where T : IMetadataScope
    {
        void Rewrite(AssemblyDefinition ass, T myRef, AssemblyDefinition insAss);
    }

    internal class PurgeRewriter : IRewiring
    {
        private readonly IRewiring<AssemblyNameReference> assWire;
        private readonly IRewiring<ModuleReference> modWire;

        public PurgeRewriter()
        {
            assWire = new ManagedPurgeRewriter();
            modWire = new NativePurgeRewriter();
        }

        public void Rewrite(AssemblyDefinition ass, AssemblyDefinition[] inserts)
        {
            var ins = inserts.ToDictionary(k => k.GetAttribute<AMA>().First(
                a => a.Key == Defaults.Replaces).Value.ToLowerInvariant(), v => v);
            foreach (var myRef in ass.GetAllExternalRefs().ToArray())
            {
                AssemblyDefinition insAss;
                var assName = myRef as AssemblyNameReference;
                if (assName != null && ins.TryGetValue(assName.Name.ToLowerInvariant(), out insAss))
                    assWire.Rewrite(ass, assName, insAss);
                var modName = myRef as ModuleReference;
                if (modName != null && ins.TryGetValue(modName.Name.ToLowerInvariant(), out insAss))
                    modWire.Rewrite(ass, modName, insAss);
            }
        }
    }

    internal class NativePurgeRewriter : IRewiring<ModuleReference>
    {
        public void Rewrite(AssemblyDefinition ass, ModuleReference modRef, AssemblyDefinition insAss)
        {
            var pinvokes = ass.GetAllTypes().SelectMany(t => t.Methods).Where(
                m => m.HasPInvokeInfo && m.PInvokeInfo.Module == modRef).ToArray();
            var oldTypes = new HashSet<TypeReference>();
            foreach (var pinvoke in pinvokes)
            {
                var typRefs = pinvoke.GetDistinctTypes().ToDictionary(k => k,
                    v => insAss.GetAllTypes().FirstOrDefault(t => t.Match(v)));
                pinvoke.PatchTypes(typRefs, t => oldTypes.Add(t));
                pinvoke.RemovePInvoke();
                var type = pinvoke.DeclaringType;
                type.Methods.Remove(pinvoke);
            }
            foreach (var meth in ass.GetAllTypes().SelectMany(t => t.Methods).Where(
                m => !m.HasPInvokeInfo && m.HasBody))
                foreach (var instr in meth.Body.Instructions)
                {
                    if (instr.OpCode != OpCodes.Call)
                        continue;
                    var methRef = instr.Operand as MethodReference;
                    if (methRef == null || !pinvokes.Contains(methRef))
                        continue;
                    // TODO: Replace correctly!
                    instr.OpCode = OpCodes.Nop;
                    instr.Operand = null;
                }
            foreach (var oldType in oldTypes.OfType<TypeDefinition>())
            {
                var module = oldType.Module;
                if (module.Assembly != ass)
                    continue;
                var owner = oldType.DeclaringType;
                owner?.NestedTypes?.Remove(oldType);
                module.Types.Remove(oldType);
            }
            ass.Remove(modRef);
        }
    }

    internal class ManagedPurgeRewriter : IRewiring<AssemblyNameReference>
    {
        public void Rewrite(AssemblyDefinition ass, AssemblyNameReference assRef, AssemblyDefinition insAss)
        {
            // TODO: Inject manageds?
            ass.Remove(assRef);
        }
    }

    internal static class PurgerInternal
    {
        private static void ProcessMarkedFiles(string workDir, IDependencyReport report)
        {
            /* var gens = GenerateCode(purged, opts.TempDir, opts.WorkDir, rparam)
                .ToDictionary(k => k.Name.Name, v => v);
                var mappings = purged.GetNativeMappings(apiPrefix).ToArray(); */
            // Inject container initializer ::: AddOrReplaceModuleSetup(ass, AddOrReplaceIoc);
            // ReplaceCalls(ass, gens, mappings);
            // ass.Write(file, wparam);
        }

        private static IEnumerable<AssemblyDefinition> GenerateCode(/*PurgedAssemblies*/object purged, string tempDir,
            string workDir, ReaderParameters rparam)
        {
            yield break;
            /*          foreach (var purge in purged)
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
                      }*/
        }

        static void ReplaceCalls(AssemblyDefinition ass, IDictionary<string, AssemblyDefinition> gens,
            KeyValuePair<string, string>[] mappings)
        {
            /* var membersToDelete = new HashSet<IMetadataTokenProvider>();
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
            }*/
        }

        public static readonly string apiSuffix = ".API";
        public static readonly string apiPrefix = "Purge.";

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

        static void HandleAbstractClass(/*PurgedType*/object fake, MethodDefinition[] overrides)
        {
            /*foreach (var overrid in overrides)
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
            }*/
        }
    }
}