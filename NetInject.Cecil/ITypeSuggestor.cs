using Mono.Cecil;

namespace NetInject.Cecil
{
    public interface ITypeSuggestor
    {
        TypeReference this[TypeReference type, ITypeImporter import] { get; }
    }
}