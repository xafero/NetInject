using Mono.Cecil;
using NetInject.Cecil;
using System.Linq;

using AMA = System.Reflection.AssemblyMetadataAttribute;

namespace NetInject.Purge
{
    public class PurgeRewriter : IRewiring
    {
        private readonly IRewiring<AssemblyNameReference> assWire;
        private readonly IRewiring<ModuleReference> modWire;

        public PurgeRewriter()
        {
            assWire = new ManagedPurgeRewriter();
            modWire = new NativePurgeRewriter();
        }

        public void Rewrite(AssemblyDefinition ass, AssemblyDefinition[] inserts)
        {
            var ins = inserts.ToDictionary(k => k.GetAttribute<AMA>().First(
                a => a.Key == Defaults.Replaces).Value.ToLowerInvariant(), v => v);
            foreach (var myRef in ass.GetAllExternalRefs().ToArray())
            {
                AssemblyDefinition insAss;
                var assName = myRef as AssemblyNameReference;
                if (assName != null && ins.TryGetValue(assName.Name.ToLowerInvariant(), out insAss))
                    assWire.Rewrite(ass, assName, insAss);
                var modName = myRef as ModuleReference;
                if (modName != null && ins.TryGetValue(modName.Name.ToLowerInvariant(), out insAss))
                    modWire.Rewrite(ass, modName, insAss);
            }
        }
    }
}