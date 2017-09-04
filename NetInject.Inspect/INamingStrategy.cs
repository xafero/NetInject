using Mono.Cecil;

namespace NetInject.Inspect
{
    public interface INamingStrategy
    {
        string GetName(AssemblyDefinition ass);

        string GetName(TypeDefinition type);

        string GetName(TypeReference type);
    }
}