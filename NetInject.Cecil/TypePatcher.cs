using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace NetInject.Cecil
{
    public class TypePatcher : ITypePatcher
    {
        private readonly IDictionary<TypeReference, TypeDefinition> _replaces;

        public TypePatcher(IDictionary<TypeReference, TypeDefinition> replaces)
        {
            _replaces = replaces;
        }

        public void Patch(AssemblyDefinition ass, Action<TypeReference> onReplace)
        {
            foreach (var mod in ass.Modules)
                Patch(mod, onReplace);
        }

        public void Patch(ModuleDefinition mod, Action<TypeReference> onReplace)
        {
            foreach (var type in mod.Types)
                Patch(type, onReplace);
        }

        public void Patch(TypeDefinition type, Action<TypeReference> onReplace)
        {
            TypeDefinition newType;
            if (type.BaseType != null && _replaces.TryGetValue(type.BaseType, out newType))
            {
                onReplace(type.BaseType);
                type.BaseType = Import(type, newType);
            }
            foreach (var intf in type.Interfaces)
                if (_replaces.TryGetValue(intf.InterfaceType, out newType))
                {
                    onReplace(intf.InterfaceType);
                    intf.InterfaceType = Import(type, newType);
                }
            foreach (var fiel in type.Fields)
                Patch(fiel, onReplace);
            foreach (var prop in type.Properties)
                Patch(prop, onReplace);
            foreach (var meth in type.Methods)
                Patch(meth, onReplace);
            foreach (var evt in type.Events)
                Patch(evt, onReplace);
            foreach (var nested in type.NestedTypes)
                Patch(nested, onReplace);
        }

        public void Patch(MethodDefinition meth, Action<TypeReference> onReplace)
        {
            if (meth == null)
                return;
            TypeDefinition newType;
            if (_replaces.TryGetValue(meth.ReturnType, out newType))
            {
                onReplace(meth.ReturnType);
                meth.ReturnType = Import(meth, newType);
            }
            foreach (var param in meth.Parameters)
                Patch(param, onReplace);
            if (!meth.HasBody)
                return;
            foreach (var vari in meth.Body.Variables)
                Patch(meth, vari, onReplace);
        }

        private void Patch(MethodDefinition meth, VariableDefinition vari, Action<TypeReference> onReplace)
        {
            TypeDefinition newType;
            if (!_replaces.TryGetValue(vari.VariableType, out newType))
                return;
            onReplace(vari.VariableType);
            vari.VariableType = Import(meth, newType);
        }

        public void Patch(ParameterDefinition param, Action<TypeReference> onReplace)
        {
            TypeDefinition newType;
            if (!_replaces.TryGetValue(param.ParameterType, out newType))
                return;
            onReplace(param.ParameterType);
            param.ParameterType = Import(param, newType);
        }

        public void Patch(PropertyDefinition prop, Action<TypeReference> onReplace)
        {
            TypeDefinition newType;
            if (_replaces.TryGetValue(prop.PropertyType, out newType))
            {
                onReplace(prop.PropertyType);
                prop.PropertyType = Import(prop, newType);
            }
            Patch(prop.GetMethod, onReplace);
            Patch(prop.SetMethod, onReplace);
        }

        public void Patch(FieldDefinition fiel, Action<TypeReference> onReplace)
        {
            TypeDefinition newType;
            if (!_replaces.TryGetValue(fiel.FieldType, out newType))
                return;
            onReplace(fiel.FieldType);
            fiel.FieldType = Import(fiel, newType);
        }

        public void Patch(EventDefinition evt, Action<TypeReference> onReplace)
        {
            TypeDefinition newType;
            if (_replaces.TryGetValue(evt.EventType, out newType))
            {
                onReplace(evt.EventType);
                evt.EventType = Import(evt, newType);
            }
            Patch(evt.AddMethod, onReplace);
            Patch(evt.InvokeMethod, onReplace);
            Patch(evt.RemoveMethod, onReplace);
        }

        private static TypeReference Import(ParameterDefinition param, TypeReference newType)
            => Import(param.Method as MethodDefinition, newType);

        private static TypeReference Import(IMemberDefinition member, TypeReference newType)
            => Import(member.DeclaringType, newType);

        private static TypeReference Import(TypeDefinition type, TypeReference newType)
            => type.Module.ImportReference(newType);
    }
}