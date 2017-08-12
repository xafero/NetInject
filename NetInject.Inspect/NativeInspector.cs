using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace NetInject.Inspect
{
    public class NativeInspector : IInspector
    {
        public int Inspect(AssemblyDefinition ass, IDependencyReport report)
        {
            var natives = 0;
            foreach (var nativeRef in ass.Modules.SelectMany(m => m.ModuleReferences))
            {
                var key = NormalizeNativeRef(nativeRef);
                ISet<string> list;
                if (!report.NativeRefs.TryGetValue(key, out list))
                    report.NativeRefs[key] = list = new SortedSet<string>();
                list.Add(ass.FullName);
                natives++;
            }
            return natives;
        }

        private static string NormalizeNativeRef(IMetadataScope nativeRef)
        {
            var name = nativeRef.Name;
            name = name.ToLowerInvariant();
            const string suffix = ".dll";
            if (!name.EndsWith(suffix))
                name = $"{name}{suffix}";
            return name;
        }
    }
}