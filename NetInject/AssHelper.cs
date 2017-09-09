using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using log4net;

using MethodAttr = Mono.Cecil.MethodAttributes;

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