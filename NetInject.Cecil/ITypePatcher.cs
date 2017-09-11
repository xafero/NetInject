using System;
using Mono.Cecil;

namespace NetInject.Cecil
{
    public interface ITypePatcher
    {
        void Patch(AssemblyDefinition ass, Action<TypeReference> onReplace);

        void Patch(ModuleDefinition mod, Action<TypeReference> onReplace);

        void Patch(TypeDefinition type, Action<TypeReference> onReplace);

        void Patch(MethodDefinition meth, Action<TypeReference> onReplace);

        void Patch(ParameterDefinition param, Action<TypeReference> onReplace);

        void Patch(PropertyDefinition prop, Action<TypeReference> onReplace);

        void Patch(FieldDefinition fiel, Action<TypeReference> onReplace);

        void Patch(EventDefinition evt, Action<TypeReference> onReplace);
    }
}