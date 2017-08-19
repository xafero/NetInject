using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NetInject.API;
using NetInject.IoC;
using MethodAttr = Mono.Cecil.MethodAttributes;
using TypeAttr = Mono.Cecil.TypeAttributes;
using FieldAttr = Mono.Cecil.FieldAttributes;

namespace NetInject
{
    internal static class IocProcessor
    {
        private const string IocName = "IoC";
        private const string CctorName = ".cctor";

        private static void AddOrReplaceIoc(ILProcessor il)
        {
            var mod = il.Body.Method.DeclaringType.Module;
            var myNamespace = mod.Types.Select(t => t.Namespace)
                .Where(n => !string.IsNullOrWhiteSpace(n)).OrderBy(n => n.Length).First();
            const TypeAttributes attr = TypeAttr.Class | TypeAttr.Public | TypeAttr.Sealed
                                        | TypeAttr.Abstract | TypeAttr.BeforeFieldInit;
            var objBase = mod.ImportReference(typeof(object));
            var type = new TypeDefinition(myNamespace, IocName, attr, objBase);
            var oldType = mod.Types.FirstOrDefault(t => t.FullName == type.FullName);
            if (oldType != null)
                mod.Types.Remove(oldType);
            mod.Types.Add(type);
            var vesselRef = mod.ImportReference(typeof(IVessel));
            const FieldAttributes fieldAttr = FieldAttr.Static | FieldAttr.Private;
            var contField = new FieldDefinition("scope", fieldAttr, vesselRef);
            type.Fields.Add(contField);
            const MethodAttributes getAttrs = MethodAttr.Static | MethodAttr.Public
                                              | MethodAttr.SpecialName | MethodAttr.HideBySig;
            var getMethod = new MethodDefinition("GetScope", getAttrs, vesselRef);
            type.Methods.Add(getMethod);
            var gmil = getMethod.Body.GetILProcessor();
            gmil.Append(gmil.Create(OpCodes.Ldsfld, contField));
            gmil.Append(gmil.Create(OpCodes.Ret));
            var voidRef = mod.ImportReference(typeof(void));
            const MethodAttributes constrAttrs = MethodAttr.Static | MethodAttr.SpecialName | MethodAttr.Private
                                                 | MethodAttr.RTSpecialName | MethodAttr.HideBySig;
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