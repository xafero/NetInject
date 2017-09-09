using Mono.Cecil;
using Mono.Cecil.Cil;
using NetInject.Cecil;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

using static NetInject.Cecil.CecilHelper;
using System;

namespace NetInject.Purge
{
    internal class NativePurgeRewriter : IRewiring<ModuleReference>
    {
        public void Rewrite(AssemblyDefinition ass, ModuleReference modRef,
            AssemblyDefinition insAss, IIocProcessor ioc)
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
                    PatchCall(meth.Body, ioc, instr, methRef, insAss);
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

        private void PatchCall(MethodBody body, IIocProcessor ioc, Instruction instr,
            MethodReference meth, AssemblyDefinition insAss)
        {
            var scopeMeth = ioc.ScopeMethod;
            var scopeType = ioc.IocType;
            var oldMeth = meth.Resolve();
            var oldAttr = oldMeth.GetAttribute<DescriptionAttribute>().SingleOrDefault()?.Description;
            var newMeth = FindMethodByStr(insAss, oldAttr);
            if (newMeth == null)
                newMeth = FindMethodByOld(insAss, oldMeth);
            if (newMeth == null)
            {
                instr.OpCode = OpCodes.Nop;
                instr.Operand = null;
                // TODO: Handle error?!
                return;
            }
            var resolveMethod = ioc.GetResolveMethod(typeof(String));





            instr.Operand = Import(body, newMeth);
        }
    }
}