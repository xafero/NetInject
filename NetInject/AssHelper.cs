using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using MethodAttr = Mono.Cecil.MethodAttributes;
using System.IO;
using log4net;

namespace NetInject
{
    internal static class AssHelper
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AssHelper));

        internal static void RemoveSigning(AssemblyDefinition ass, IEnumerable<string> keys)
        {
            if (!keys.Any(k => ass.FullName.EndsWith($"={k}", StringComparison.InvariantCulture)))
                return;
            ass.Name.HasPublicKey = false;
            ass.Name.PublicKey = new byte[0];
            foreach (var module in ass.Modules)
                module.Attributes &= ~ModuleAttributes.StrongNameSigned;
        }

        internal static void RemoveSignedRefs(IEnumerable<ModuleDefinition> modules, IEnumerable<string> keys)
        {
            foreach (var module in modules)
            foreach (var assRef in module.AssemblyReferences)
            {
                if (!keys.Any(k => assRef.FullName.EndsWith($"={k}", StringComparison.InvariantCulture)))
                    continue;
                assRef.HasPublicKey = false;
                assRef.PublicKey = new byte[0];
            }
        }

        internal static IEnumerable<T> GetAttribute<T>(this TypeDefinition type) where T : Attribute
            => type.CustomAttributes.Where(a => a.AttributeType.FullName == typeof(T).FullName)
                .Select(a => a.ConstructorArguments.Select(c => c.Value).ToArray())
                .Select(a => (T) typeof(T).GetConstructors().First().Invoke(a));

        internal static void RemovePInvoke(this MethodDefinition meth)
        {
            meth.Attributes &= ~MethodAttr.PInvokeImpl;
            meth.IsPreserveSig = false;
        }

        internal static void AddOrReplaceModuleSetup(AssemblyDefinition ass, Action<ILProcessor> il = null)
        {
            var mod = ass.MainModule;
            var voidRef = mod.ImportReference(typeof(void));
            var attrs = MethodAttr.Static | MethodAttr.SpecialName | MethodAttr.RTSpecialName;
            var cctor = new MethodDefinition(".cctor", attrs, voidRef);
            var modClass = mod.Types.First(t => t.Name == "<Module>");
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

        private static string[] CSharpKeyWords = {"object"};

        internal static string Escape(string name)
            => CSharpKeyWords.Contains(name) ? $"@{name}" : name;

        public static AssemblyDefinition ReadAssembly(
            Stream stream, ReaderParameters rparam, string file)
        {
            try
            {
                return stream == null
                    ? AssemblyDefinition.ReadAssembly(file, rparam)
                    : AssemblyDefinition.ReadAssembly(stream, rparam);
            }
            catch (BadImageFormatException)
            {
                log.Error($"Could not read image from '{file}'!");
                return null;
            }
        }
    }
}