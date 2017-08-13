using System.Collections.Generic;

namespace NetInject.Inspect
{
    internal class AssemblyType : IType
    {
        public string Name { get; }
        public IDictionary<string, IMethod> Methods { get; }

        public AssemblyType(string name)
        {
            Name = name;
            Methods = new SortedDictionary<string, IMethod>();
        }
    }
}