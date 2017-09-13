using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace NetInject.Inspect
{
    internal class CodePatcher : ICodePatcher
    {
        private readonly IDictionary<string, string> _mappings;

        public CodePatcher(IDictionary<TypeDefinition, string> mappings)
        {
            _mappings = mappings.ToDictionary(k => k.Key.FullName, v => v.Value);
        }

        public void Patch(IType type)
        {
            foreach (var myBase in type.Bases.ToArray())
            {
                string newBase;
                if (!TryGetType(myBase, out newBase)) continue;
                type.Bases.Remove(myBase);
                type.Bases.Add(newBase);
            }
            foreach (var myPair in type.Fields.ToArray())
            {
                var myField = myPair.Value;
                string newField;
                if (!TryGetType(myField.Type, out newField)) continue;
                type.Fields[myPair.Key] = new AssemblyField(myField.Name, newField);
            }
            foreach (var myPair in type.Methods.ToArray())
            {
                var myMethod = myPair.Value;
                var dirty = false;
                string newRt;
                if (TryGetType(myMethod.ReturnType ?? typeof(void).FullName, out newRt))
                    dirty = true;
                string tmp;
                if (myMethod.Parameters.Any(p => TryGetType(p.Type, out tmp)))
                    dirty = true;
                if (!dirty)
                    continue;
                var meth = new AssemblyMethod(myMethod.Name, newRt ?? myMethod.ReturnType);
                foreach (var parm in myMethod.Parameters)
                {
                    var ptype = parm.Type;
                    string newPtype;
                    if (TryGetType(ptype, out newPtype))
                        ptype = newPtype;
                    meth.Parameters.Add(new MethodParameter(parm.Name, ptype));
                }
                type.Methods[myPair.Key] = meth;
            }
        }

        private const char RefSuffix = '&';

        private bool TryGetType(string type, out string newType)
        {
            var term = type.TrimEnd(RefSuffix);
            if (_mappings.TryGetValue(term, out newType))
                return true;


            if (type.StartsWith("System."))
                return false;


            throw new InvalidOperationException();
        }
    }
}