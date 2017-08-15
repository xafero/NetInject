using System.Collections.Generic;
using System.Linq;

namespace NetInject.Inspect
{
    public class Units<T> : SortedDictionary<string, T> where T : IUnit
    {
        public IEnumerable<KeyValuePair<string, string>> GetNativeMappings(string prefix)
            => this.SelectMany(p => p.Value.Types.SelectMany(
                t => t.Value.Methods.SelectMany(
                    m => m.Value.Aliases.ToDictionary(k => k, v => $"{prefix}{t.Value.Namespace}.I{t.Value.Name}.{m.Value.Name}"))));
    }
}