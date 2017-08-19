using System.Collections.Generic;
using NetInject.Cecil;

namespace NetInject.Inspect
{
    internal class AssemblyType : IType
    {
        private readonly string _name;

        public ICollection<IConstraint> Constraints { get; }
        public TypeKind Kind { get; }
        public ICollection<string> Bases { get; }
        public IDictionary<string, IField> Fields { get; }
        public IDictionary<string, IMethod> Methods { get; }
        public IDictionary<string, IValue> Values { get; }

        public AssemblyType(string name, TypeKind kind)
        {
            _name = name;
            Constraints = new SortedSet<IConstraint>();
            Kind = kind;
            Bases = new SortedSet<string>();
            Fields = new SortedDictionary<string, IField>();
            Methods = new SortedDictionary<string, IMethod>();
            Values = new SortedDictionary<string, IValue>();
        }

        public string Namespace => _name.Substring(0, _name.LastIndexOf('.').BeNonNegative(_name.Length));

        public string Name => _name.Substring(_name.LastIndexOf('.') + 1);
    }
}