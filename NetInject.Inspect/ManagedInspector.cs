using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace NetInject.Inspect
{
    public class ManagedInspector : IInspector
    {
        public int Inspect(AssemblyDefinition ass, IDependencyReport report)
        {
            var manageds = 0;
            foreach (var assRef in ass.Modules.SelectMany(m => m.AssemblyReferences))
            {
                var key = assRef.Name;
                if (key == "mscorlib" || key == "System" || key == "System.Core" || key == "Microsoft.CSharp")
                    continue;
                ISet<string> list;
                if (!report.ManagedRefs.TryGetValue(key, out list))
                    report.ManagedRefs[key] = list = new SortedSet<string>();
                list.Add(ass.FullName);
                manageds++;
            }
            return manageds;
        }
    }
}