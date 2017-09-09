using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NetInject.API;
using NetInject.IoC;

using MAttr = Mono.Cecil.MethodAttributes;
using TAttr = Mono.Cecil.TypeAttributes;
using FAttr = Mono.Cecil.FieldAttributes;

namespace NetInject.Purge
{
    internal static class IocProcessor
    {
        private const string IocName = "IoC";
        private const string IocField = "scope";
        private const string IocMethod = "GetScope";
        private const string CctorName = ".cctor";

        internal static void AddOrReplaceIoc(ILProcessor il)
        {
            var mod = il.Body.Method.DeclaringType.Module;
            var myNamespace = mod.Types.Select(t => t.Namespace)
                .Where(n => !string.IsNullOrWhiteSpace(n)).OrderBy(n => n.Length).First();
            const TAttr attr = TAttr.Class | TAttr.Public | TAttr.Sealed
                                        | TAttr.Abstract | TAttr.BeforeFieldInit;
            var objBase = mod.ImportReference(typeof(object));
            var type = new TypeDefinition(myNamespace, IocName, attr, objBase);
            var oldType = mod.Types.FirstOrDefault(t => t.FullName == type.FullName);
            if (oldType != null)
                mod.Types.Remove(oldType);
            mod.Types.Add(type);
            var vesselRef = mod.ImportReference(typeof(IVessel));
            const FAttr fieldAttr = FAttr.Static | FAttr.Private;
            var contField = new FieldDefinition(IocField, fieldAttr, vesselRef);
            type.Fields.Add(contField);
            const MAttr getAttrs = MAttr.Static | MAttr.Public
                                              | MAttr.SpecialName | MAttr.HideBySig;
            var getMethod = new MethodDefinition(IocMethod, getAttrs, vesselRef);
            type.Methods.Add(getMethod);
            var gmil = getMethod.Body.GetILProcessor();
            gmil.Append(gmil.Create(OpCodes.Ldsfld, contField));
            gmil.Append(gmil.Create(OpCodes.Ret));
            var voidRef = mod.ImportReference(typeof(void));
            const MAttr constrAttrs = MAttr.Static | MAttr.SpecialName | MAttr.Private
                                                 | MAttr.RTSpecialName | MAttr.HideBySig;
            var constr = new MethodDefinition(CctorName, constrAttrs, voidRef);
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
    }
}