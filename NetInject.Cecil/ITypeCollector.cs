using Mono.Cecil;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace NetInject.Cecil
{
    public interface ITypeCollector
    {
        void Collect(AssemblyDefinition ass);

        void Collect(ModuleDefinition mod);

        void Collect(TypeReference type);

        void Collect(TypeDefinition type);

        void Collect(MethodReference meth);

        void Collect(MethodDefinition meth);

        void Collect(VariableReference vari);

        void Collect(VariableDefinition vari);

        void Collect(ParameterReference param);

        void Collect(ParameterDefinition param);

        void Collect(PropertyReference prop);

        void Collect(PropertyDefinition prop);

        void Collect(EventReference evet);

        void Collect(EventDefinition evet);

        void Collect(FieldReference fiel);

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