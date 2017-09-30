using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

using MethodAttr = Mono.Cecil.MethodAttributes;

namespace NetInject.Cecil
{
    public static class AssHelper
    {
        public static bool IsDelegate(this Type type) => type.BaseType == typeof(MulticastDelegate);

        public static IEnumerable<T> GetAttribute<T>(this ICustomAttributeProvider prov) where T : Attribute
            => GetAttribute<T>(prov.CustomAttributes);

        private static IEnumerable<T> GetAttribute<T>(IEnumerable<CustomAttribute> attributes) where T : Attribute
            => attributes.Where(a => a.AttributeType.FullName == typeof(T).FullName)
                .Select(a => a.ConstructorArguments.Select(c => c.Value).ToArray())
                .Select(a => (T)typeof(T).GetConstructors().First().Invoke(a));

        public static void RemovePInvoke(this MethodDefinition meth)
        {
            meth.Attributes &= ~MethodAttr.PInvokeImpl;
            meth.IsPreserveSig = false;
        }

        public static void AddOrReplaceModuleSetup(this AssemblyDefinition ass, Action<ILProcessor> il = null)
        {
            var mod = ass.MainModule;
            var voidRef = mod.ImportReference(typeof(void));
            var attrs = MethodAttr.Static | MethodAttr.SpecialName | MethodAttr.RTSpecialName;
            var cctor = new MethodDefinition(Defaults.StaticConstrName, attrs, voidRef);
            var modClass = mod.Types.First(t => t.Name == Defaults.ModuleName);
            var oldMeth = modClass.Methods.FirstOrDefault(m => m.Name == cctor.Name);
            if (oldMeth != null)
                modClass.Methods.Remove(oldMeth);
            modClass.Methods.Add(cctor);
            var body = cctor.Body.GetILProcessor();
            if (il == null)
            {
                body.Append(body.Create(OpCodes.Nop));
                body.Append(body.Create(OpCodes.Ret));
            }
            il?.Invoke(body);
        }
    }
}