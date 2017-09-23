using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace NetInject.Cecil
{
    public class TypePatcher : ITypePatcher
    {
        private readonly IDictionary<TypeReference, TypeReference> _replaces;
        private readonly TypeSuggestor _suggestor;

        public TypePatcher(IDictionary<TypeReference, TypeReference> replaces)
        {
            _replaces = replaces;
            _suggestor = new TypeSuggestor(_replaces);
        }

        private bool TryGetValue(IMemberDefinition member, TypeReference tOld, out TypeReference tNew)
        {
            if (member == null || tOld == null)
            {
                tNew = null;
                return false;
            }
            var import = new TypeImporter(member);
            tNew = _suggestor[tOld, import];
            if (tNew == null || tNew == tOld || tOld.FullName.Equals(tNew.FullName))
            {
                tNew = null;
                return false;
            }
            _replaces[tOld] = tNew;
            return true;
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
            TypeReference newType;
            if (type.BaseType != null && TryGetValue(type, type.BaseType, out newType))
            {
                onReplace(type.BaseType);
                type.BaseType = newType;
            }
            foreach (var intf in type.Interfaces)
                if (TryGetValue(type, intf.InterfaceType, out newType))
                {
                    onReplace(intf.InterfaceType);
                    intf.InterfaceType = newType;
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
            TypeReference newType;
            if (TryGetValue(meth, meth.ReturnType, out newType))
            {
                onReplace(meth.ReturnType);
                meth.ReturnType = newType;
            }
            foreach (var param in meth.Parameters)
                Patch(param, onReplace);
            if (meth.HasBody)
                Patch(meth.Body, onReplace);
        }

        private void Patch(MethodBody body, Action<TypeReference> onReplace)
        {
            foreach (var vari in body.Variables)
                Patch(body.Method, vari, onReplace);
            foreach (var instr in body.Instructions)
            {
                if (instr.HasNoUsefulOperand())
                    continue;
                var meth = instr.Operand as MethodReference;
                if (meth != null)
                    PatchMethodRef(body, onReplace, meth, instr);
                var type = instr.Operand as TypeReference;
                if (type != null)
                    PatchTypeRef(body, onReplace, type, instr);
                var fiel = instr.Operand as FieldReference;
                if (fiel != null)
                    PatchFieldRef(body, onReplace, fiel, instr);
            }
        }

        private void PatchFieldRef(MethodBody body, Action<TypeReference> onReplace,
            FieldReference fiel, Instruction instr)
        {
            TypeReference declaringType;
            TypeReference fieldType;
            var fielDef = fiel as FieldDefinition ?? fiel.TryResolve();
            if (TryGetValue(body.Method, fielDef.DeclaringType, out declaringType))
                onReplace(fielDef.DeclaringType);
            if (TryGetValue(body.Method, fielDef.FieldType, out fieldType))
                onReplace(fielDef.FieldType);
            if (fieldType == null && declaringType == null)
                return;
            instr.Operand = new FieldReference(fiel.Name,
                fieldType ?? fiel.FieldType,
                declaringType ?? fiel.DeclaringType);
        }

        private void PatchTypeRef(MethodBody body, Action<TypeReference> onReplace,
            TypeReference type, Instruction instr)
        {
            TypeReference newType;
            var typeDef = type as TypeDefinition ?? type.TryResolve();
            if (!TryGetValue(body.Method, typeDef, out newType))
                return;
            onReplace(typeDef);
            instr.Operand = newType;
        }

        private void PatchMethodRef(MethodBody body, Action<TypeReference> onReplace,
            MethodReference meth, Instruction instr)
        {
            TypeReference declaringType;
            TypeReference returnType;
            var methDef = meth as MethodDefinition ?? meth.TryResolve();
            if (TryGetValue(body.Method, methDef.DeclaringType, out declaringType))
                onReplace(methDef.DeclaringType);
            if (TryGetValue(body.Method, methDef.ReturnType, out returnType))
                onReplace(methDef.ReturnType);
            if (returnType == null && declaringType == null)
                return;
            var newMeth = new MethodReference(meth.Name,
                returnType ?? meth.ReturnType,
                declaringType ?? meth.DeclaringType)
            {
                CallingConvention = meth.CallingConvention,
                ExplicitThis = meth.ExplicitThis,
                HasThis = meth.HasThis
            };
            foreach (var parm in meth.Parameters)
            {
                TypeReference ptype;
                if (TryGetValue(body.Method, parm.ParameterType, out ptype))
                    onReplace(parm.ParameterType);
                var mparm = new ParameterDefinition(parm.Name, parm.Attributes,
                    ptype ?? parm.ParameterType);
                newMeth.Parameters.Add(mparm);
            }
            instr.Operand = newMeth;
        }

        private void Patch(IMemberDefinition meth, VariableDefinition vari, Action<TypeReference> onReplace)
        {
            TypeReference newType;
            if (!TryGetValue(meth, vari.VariableType, out newType))
                return;
            onReplace(vari.VariableType);
            vari.VariableType = newType;
        }

        public void Patch(ParameterDefinition param, Action<TypeReference> onReplace)
        {
            TypeReference newType;
            if (!TryGetValue((IMemberDefinition) param.Method, param.ParameterType, out newType))
                return;
            onReplace(param.ParameterType);
            param.ParameterType = newType;
        }

        public void Patch(PropertyDefinition prop, Action<TypeReference> onReplace)
        {
            TypeReference newType;
            if (TryGetValue(prop, prop.PropertyType, out newType))
            {
                onReplace(prop.PropertyType);
                prop.PropertyType = newType;
            }
            Patch(prop.GetMethod, onReplace);
            Patch(prop.SetMethod, onReplace);
        }

        public void Patch(FieldDefinition fiel, Action<TypeReference> onReplace)
        {
            TypeReference newType;
            if (!TryGetValue(fiel, fiel.FieldType, out newType))
                return;
            onReplace(fiel.FieldType);
            fiel.FieldType = newType;
        }

        public void Patch(EventDefinition evt, Action<TypeReference> onReplace)
        {
            TypeReference newType;
            if (TryGetValue(evt, evt.EventType, out newType))
            {
                onReplace(evt.EventType);
                evt.EventType = newType;
            }
            Patch(evt.AddMethod, onReplace);
            Patch(evt.InvokeMethod, onReplace);
            Patch(evt.RemoveMethod, onReplace);
        }
    }
}