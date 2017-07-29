using System.Linq;
using System.Collections.Generic;

namespace NetInject.Purge
{
    public class PurgedAssemblies : SortedDictionary<string, PurgedAssembly>
    {
        public IEnumerable<KeyValuePair<string, string>> GetNativeMappings(string prefix)
            => this.SelectMany(p => p.Value.Types.SelectMany(
                t => t.Value.Methods.SelectMany(
                    m => m.Value.Refs.ToDictionary(k => k, v => $"{prefix}{t.Value.Namespace}.I{t.Value.Name}.{m.Value.Name}"))));
    }
}