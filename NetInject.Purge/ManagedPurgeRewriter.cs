using Mono.Cecil;
using NetInject.Cecil;

namespace NetInject.Purge
{
    internal class ManagedPurgeRewriter : IRewiring<AssemblyNameReference>
    {
        public void Rewrite(AssemblyDefinition ass, AssemblyNameReference assRef,
            AssemblyDefinition insAss, IIocProcessor ioc)
        {
            // TODO: Inject manageds?
            ass.Remove(assRef);
        }
    }
}