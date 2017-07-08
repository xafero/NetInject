using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NetInject
{
    class AssHelper
    {
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
    }
}