using Mono.Cecil;

namespace NetInject.Cecil
{
    public struct TypeImporter : ITypeImporter
    {
        private readonly ModuleDefinition _module;

        public TypeImporter(ModuleDefinition module)
        {
            _module = module;
        }

        public TypeImporter(IMemberDefinition member) : this(
            (member as TypeReference)?.Module ?? member.DeclaringType.Module)
        {
        }

        public TypeImporter(IGenericParameterProvider type) : this(type.Module)
        {
        }

        public TypeImporter(ParameterDefinition param) : this((IMemberDefinition) (param.Method as MethodDefinition))
        {
        }

        public TypeReference Import(TypeReference type)
            => string.IsNullOrWhiteSpace(type.Namespace) ? type : _module.ImportReference(type);
    }
}