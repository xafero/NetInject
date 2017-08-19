using System;
using System.Collections.Generic;

namespace NetInject.Inspect
{
    public struct AssemblyConstraint : IConstraint, IComparable, IComparable<IConstraint>
    {
        public string Name { get; }

        public ICollection<string> Clauses { get; }

        public AssemblyConstraint(string name, params string[] clauses)
        {
            Name = name;
            Clauses = new SortedSet<string>(clauses);
        }

        public override string ToString()
            => $"{Name} : {string.Join(", ", Clauses)}";

        public int CompareTo(object obj) => CompareTo(obj as IConstraint);

        public int CompareTo(IConstraint other)
            => StringComparer.InvariantCultureIgnoreCase.Compare(ToString(), other.ToString());
    }
}