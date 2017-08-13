using System.Collections.Generic;
using NetInject.Cecil;

namespace NetInject.Inspect
{
    internal class AssemblyType : IType
    {
        public TypeKind Kind { get; }
        public string Name { get; }
        public IDictionary<string, IField> Fields { get; }
        public IDictionary<string, IMethod> Methods { get; }
        public IDictionary<string, IValue> Values { get; }

        public AssemblyType(string name, TypeKind kind)
        {
            Kind = kind;
            Name = name;
            Fields = new SortedDictionary<string, IField>();
            Methods = new SortedDictionary<string, IMethod>();
            Values = new SortedDictionary<string, IValue>();
        }
    }
}