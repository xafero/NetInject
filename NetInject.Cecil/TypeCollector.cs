using Mono.Cecil;

namespace NetInject.Cecil
{
    public class TypeCollector : ITypeCollector
    {
        public void Collect(AssemblyDefinition ass)
        {
            foreach (var mod in ass.Modules)
                Collect(mod);
        }

        public void Collect(ModuleDefinition mod)
        {
            foreach (var type in mod.Types)
                Collect(type);
        }

        public void Collect(TypeReference type)
        {
            Collect(type.Resolve());
        }

        public void Collect(TypeDefinition type)
        {
            if (type.BaseType != null)
                Collect(type.BaseType);
            foreach (var intf in type.Interfaces)
                Collect(intf.InterfaceType);
            foreach (var fiel in type.Fields)
                Collect(fiel);
            foreach (var prop in type.Properties)
                Collect(prop);
            foreach (var meth in type.Methods)
                Collect(meth);
            foreach (var evt in type.Events)
                Collect(evt);
            foreach (var nested in type.NestedTypes)
                Collect(nested);
        }

        public void Collect(MethodDefinition meth)
        {
            Collect(meth.ReturnType);
            foreach (var param in meth.Parameters)
                Collect(param);
        }

        public void Collect(ParameterDefinition param)
        {
            Collect(param.ParameterType);
        }

        public void Collect(PropertyDefinition prop)
        {
            Collect(prop.PropertyType);
            Collect(prop.GetMethod);
            Collect(prop.SetMethod);
        }

        public void Collect(FieldDefinition fiel)
        {
            Collect(fiel.FieldType);
        }

        public void Collect(EventDefinition evt)
        {
            Collect(evt.EventType);
            Collect(evt.AddMethod);
            Collect(evt.InvokeMethod);
            Collect(evt.RemoveMethod);
        }
    }
}