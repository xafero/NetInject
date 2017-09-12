using Mono.Cecil;
using System.Collections.Generic;

namespace NetInject.Cecil
{
    public interface ITypeCollector
    {
        void Collect(AssemblyDefinition ass);

        void Collect(ModuleDefinition mod);

        void Collect(TypeReference type);

        void Collect(TypeDefinition type);

        void Collect(MethodDefinition meth);

        void Collect(PropertyDefinition prop);

        void Collect(EventDefinition evt);

        void Collect(FieldDefinition fiel);

        ICollection<AssemblyDefinition> Asses { get; }
        ICollection<ModuleDefinition> Modules { get; }
        ICollection<TypeDefinition> Types { get; }
        ICollection<MethodDefinition> Methods { get; }
        ICollection<PropertyDefinition> Properties { get; }
        ICollection<EventDefinition> Events { get; }
        ICollection<FieldDefinition> Fields { get; }
    }
}