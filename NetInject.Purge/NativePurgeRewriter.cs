using Mono.Cecil;
using Mono.Cecil.Cil;
using NetInject.Cecil;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

using static NetInject.Cecil.CecilHelper;

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
            var shittyTypes = pinvokes.SelectMany(p => p.CollectDistinctTypes()).Distinct();
            var typRefs = shittyTypes.ToDictionary(k => k,
                v => insAss.GetAllTypes().FirstOrDefault(t => t.Match(v)));
            var patcher = new TypePatcher(typRefs);
            patcher.Patch(ass, t => oldTypes.Add(t));
            foreach (var pinvoke in pinvokes)
            {
                pinvoke.RemovePInvoke();
                var type = pinvoke.DeclaringType;
                type.Methods.Remove(pinvoke);
            }
            foreach (var meth in ass.GetAllTypes().SelectMany(t => t.Methods).Where(
                m => !m.HasPInvokeInfo && m.HasBody))
                foreach (var instr in meth.Body.Instructions.ToArray())
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
            var oldMeth = meth.Resolve();
            var oldAttr = oldMeth.GetAttribute<DescriptionAttribute>().SingleOrDefault()?.Description;
            var newMeth = FindMethodByStr(insAss, oldAttr);
            if (newMeth == null)
                newMeth = FindMethodByOld(insAss, oldMeth, false);
            if (newMeth == null)
            {
                instr.OpCode = OpCodes.Nop;
                instr.Operand = null;
                // TODO: Handle error?!
                return;
            }
            PatchCalls(body, ioc, instr, newMeth);
        }

        private void PatchCalls(MethodBody body, IIocProcessor ioc, Instruction instr,
            MethodDefinition newMeth)
        {
            var scopeMeth = ioc.ScopeMethod;
            var resolveMeth = ioc.GetResolveMethod(newMeth.DeclaringType);
            var stepsBack = newMeth.Parameters.Count;
            var ilStart = instr.GoBack(stepsBack);
            var il = body.GetILProcessor();
            il.InsertBefore(ilStart, il.Create(OpCodes.Call, scopeMeth));
            il.InsertBefore(ilStart, il.Create(OpCodes.Callvirt, resolveMeth));
            instr.OpCode = OpCodes.Callvirt;
            instr.Operand = Import(body, newMeth);
        }
    }
}