using Mono.Cecil;
using Mono.Cecil.Cil;
using NetInject.Cecil;
using System.Collections.Generic;
using System.Linq;

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
                    var scopeMeth = ioc.ScopeMethod;
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
}