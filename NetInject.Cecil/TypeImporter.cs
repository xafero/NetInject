using Mono.Cecil;

namespace NetInject.Cecil
{
    public struct TypeImporter : ITypeImporter
    {
        private readonly ModuleDefinition _module;

        public TypeImporter(IMemberDefinition member)
        {
            _module = member.DeclaringType.Module;
        }

        public TypeImporter(IGenericParameterProvider type)
        {
            _module = type.Module;
        }

        public TypeReference Import(TypeReference type)
            => _module.ImportReference(type);
    }
}