using Mono.Cecil;

namespace NetInject.Purge
{
    public interface IRewiring
    {
        void Rewrite(AssemblyDefinition ass, AssemblyDefinition[] inserts);
    }

    public interface IRewiring<T> where T : IMetadataScope
    {
        void Rewrite(AssemblyDefinition ass, T myRef, AssemblyDefinition insAss, IIocProcessor ioc);
    }
}