using Mono.Cecil;

namespace NetInject.Cecil
{
    public interface ITypeImporter
    {
        TypeReference Import(TypeReference type);
    }
}