using System;
using System.Collections.Generic;

namespace NetInject.Purge
{
    public class PurgedAssembly
    {
        public string Name { get; }
        public string Version { get; }
        public IDictionary<string, PurgedType> Types { get; }

        public PurgedAssembly(string name, Version version)
        {
            Name = name;
            Version = version.ToString(3);
            Types = new SortedDictionary<string, PurgedType>();
        }
    }
}