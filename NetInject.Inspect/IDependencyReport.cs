using System.Collections.Generic;

namespace NetInject.Inspect
{
    public interface IDependencyReport
    {
        ICollection<string> Files { get; }

        IDictionary<string, ISet<string>> NativeRefs { get; }

        IDictionary<string, ISet<string>> ManagedRefs { get; }
    }
}