using System;
using System.Collections.Generic;

namespace NetInject.Inspect
{
    internal class AssemblyUnit : IUnit
    {
        public string Name { get; }
        public string Version { get; }
        public IDictionary<string, IType> Types { get; }

        public AssemblyUnit(string name, Version version)
        {
            Name = name;
            Version = version.ToString();
            Types = new SortedDictionary<string, IType>();
        }
    }
}