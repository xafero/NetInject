using System.Collections.Generic;

namespace NetInject.Purge
{
    public class PurgedMethod
    {
        public string Name { get; }
        public ISet<string> Refs { get; }
        public string ReturnType { get; set; }

        public PurgedMethod(string name)
        {
            Name = name;
            Refs = new HashSet<string>();
        }
    }
}