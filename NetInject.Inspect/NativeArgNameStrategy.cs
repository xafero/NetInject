using Mono.Cecil;
using NetInject.Cecil;

namespace NetInject.Inspect
{
    internal class NativeArgNameStrategy : INamingStrategy
    {
        private readonly IType _type;

        public NativeArgNameStrategy(IType type)
        {
            _type = type;
        }

        public string GetName(AssemblyDefinition ass) => _type.Namespace;

        public string GetName(TypeDefinition type) =>
            type.IsInStandardLib() ? null : $"{_type.Namespace}.{type.Name}";

        public string GetName(TypeReference type) =>
            type.IsInStandardLib() ? null : $"{_type.Namespace}.{type.Name}";
    }
}