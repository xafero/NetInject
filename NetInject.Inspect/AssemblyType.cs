using System.Collections.Generic;
using NetInject.Cecil;

namespace NetInject.Inspect
{
    internal class AssemblyType : IType
    {
        public TypeKind Kind { get; }
        public IDictionary<string, IField> Fields { get; }
        public IDictionary<string, IMethod> Methods { get; }
        public IDictionary<string, IValue> Values { get; }

        private readonly string _name;

        public AssemblyType(string name, TypeKind kind)
        {
            _name = name;
            Kind = kind;
            Fields = new SortedDictionary<string, IField>();
            Methods = new SortedDictionary<string, IMethod>();
            Values = new SortedDictionary<string, IValue>();
        }

        public string Namespace => _name.Substring(0, _name.LastIndexOf('.').BeNonNegative(_name.Length));

        public string Name => _name.Substring(_name.LastIndexOf('.') + 1);
    }
}