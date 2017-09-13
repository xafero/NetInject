using Mono.Cecil;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace NetInject.Cecil
{
    public class TypeCollector : ITypeCollector
    {
        public ICollection<AssemblyDefinition> Asses { get; } = new HashSet<AssemblyDefinition>();
        public ICollection<ModuleDefinition> Modules { get; } = new HashSet<ModuleDefinition>();
        public ICollection<TypeDefinition> Types { get; } = new HashSet<TypeDefinition>();
        public ICollection<MethodDefinition> Methods { get; } = new HashSet<MethodDefinition>();
        public ICollection<PropertyDefinition> Properties { get; } = new HashSet<PropertyDefinition>();
        public ICollection<EventDefinition> Events { get; } = new HashSet<EventDefinition>();
        public ICollection<FieldDefinition> Fields { get; } = new HashSet<FieldDefinition>();

        public void Collect(AssemblyDefinition ass)
        {
            if (Asses.Contains(ass)) return;
            Asses.Add(ass);
            foreach (var mod in ass.Modules)
                Collect(mod);
        }

        public void Collect(ModuleDefinition mod)
        {
            if (Modules.Contains(mod)) return;
            Modules.Add(mod);
            foreach (var type in mod.Types)
                Collect(type);
        }

        public void Collect(TypeReference type)
        {
            TypeDefinition def;
            if ((def = type as TypeDefinition ?? type.Resolve()) != null)
                Collect(def);
        }

        public void Collect(MethodReference meth)
        {
            MethodDefinition def;
            if ((def = meth as MethodDefinition ?? meth.Resolve()) != null)
                Collect(def);
        }

        public void Collect(FieldReference fiel)
        {
            FieldDefinition def;
            if ((def = fiel as FieldDefinition ?? fiel.Resolve()) != null)
                Collect(def);
        }

        public void Collect(PropertyReference prop)
        {
            PropertyDefinition def;
            if ((def = prop as PropertyDefinition ?? prop.Resolve()) != null)
                Collect(def);
        }

        public void Collect(EventReference evet)
        {
            EventDefinition def;
            if ((def = evet as EventDefinition ?? evet.Resolve()) != null)
                Collect(def);
        }

        public void Collect(ParameterReference parm)
        {
            ParameterDefinition def;
            if ((def = parm as ParameterDefinition ?? parm.Resolve()) != null)
                Collect(def);
        }

        public void Collect(VariableReference vari)
        {
            VariableDefinition def;
            if ((def = vari as VariableDefinition ?? vari.Resolve()) != null)
                Collect(def);
        }

        public void Collect(TypeDefinition type)
        {
            if (Types.Contains(type) || type.IsInStandardLib()) return;
            Types.Add(type);
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
            if (Methods.Contains(meth)) return;
            Methods.Add(meth);
            Collect(meth.ReturnType);
            foreach (var param in meth.Parameters)
                Collect(param);
            if (meth.HasBody)
                Collect(meth.Body);
        }

        private void Collect(MethodBody body)
        {
            foreach (var vari in body.Variables)
                Collect(vari);
            foreach (var instr in body.Instructions)
            {
                if (instr.HasNoUsefulOperand())
                    continue;
                var meth = instr.Operand as MethodReference;
                if (meth != null)
                    Collect(meth);
                var type = instr.Operand as TypeReference;
                if (type != null)
                    Collect(type);
                var fiel = instr.Operand as FieldReference;
                if (fiel != null)
                    Collect(fiel);
                var prop = instr.Operand as PropertyReference;
                if (prop != null)
                    Collect(prop);
                var evet = instr.Operand as EventReference;
                if (evet != null)
                    Collect(evet);
                var parm = instr.Operand as ParameterReference;
                if (parm != null)
                    Collect(parm);
                var vari = instr.Operand as VariableReference;
                if (vari != null)
                    Collect(vari);
            }
        }

        public void Collect(VariableDefinition vari)
        {
            Collect(vari.VariableType);
        }

        public void Collect(ParameterDefinition param)
        {
            Collect(param.ParameterType);
        }

        public void Collect(PropertyDefinition prop)
        {
            if (Properties.Contains(prop)) return;
            Properties.Add(prop);
            Collect(prop.PropertyType);
            if (prop.GetMethod != null)
                Collect(prop.GetMethod);
            if (prop.SetMethod != null)
                Collect(prop.SetMethod);
        }

        public void Collect(FieldDefinition fiel)
        {
            if (Fields.Contains(fiel)) return;
            Fields.Add(fiel);
            Collect(fiel.FieldType);
        }

        public void Collect(EventDefinition evt)
        {
            if (Events.Contains(evt)) return;
            Events.Add(evt);
            Collect(evt.EventType);
            if (evt.AddMethod != null)
                Collect(evt.AddMethod);
            if (evt.InvokeMethod != null)
                Collect(evt.InvokeMethod);
            if (evt.RemoveMethod != null)
                Collect(evt.RemoveMethod);
        }
    }
}