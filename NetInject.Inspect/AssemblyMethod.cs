using System.Collections.Generic;

namespace NetInject.Inspect
{
    internal class AssemblyMethod : IMethod
    {
        public string ReturnType { get; }
        public string Name { get; }
        public ICollection<IParameter> Parameters { get; }
        public ICollection<string> Aliases { get; }

        public AssemblyMethod(string name, string retType)
        {
            Name = name;
            ReturnType = retType;
            Parameters = new List<IParameter>();
            Aliases = new SortedSet<string>();
        }
    }
}