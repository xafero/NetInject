using Mono.Cecil;

namespace NetInject.Cecil
{
    public interface ITypeCollector
    {
        void Collect(AssemblyDefinition ass);

        void Collect(ModuleDefinition mod);

        void Collect(TypeDefinition type);
    }
}