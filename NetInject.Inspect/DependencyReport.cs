using System.Collections.Generic;

namespace NetInject.Inspect
{
    public class DependencyReport : IDependencyReport
    {
        public ICollection<string> Files { get; }
        public IDictionary<string, ISet<string>> NativeRefs { get; set; }
        public IDictionary<string, ISet<string>> ManagedRefs { get; set; }

        public DependencyReport()
        {
            Files = new SortedSet<string>();
            NativeRefs = new SortedDictionary<string, ISet<string>>();
            ManagedRefs = new SortedDictionary<string, ISet<string>>();
        }
    }
}