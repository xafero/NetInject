using Mono.Cecil;

namespace NetInject.Inspect
{
    public class DefaultNamingStrategy : INamingStrategy
    {
        public string GetName(AssemblyNameDefinition assName)
            => assName.Name;

        public string GetName(AssemblyDefinition ass)
            => GetName(ass.Name);

        public string GetName(TypeReference type)
            => type.FullName;

        public string GetName(TypeDefinition type)
            => GetName((TypeReference) type);
    }
}