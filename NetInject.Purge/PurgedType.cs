using System.Collections.Generic;

namespace NetInject.Purge
{
    public class PurgedType
    {
        public string Namespace { get; }
        public string Name { get; }
        public IDictionary<string, PurgedMethod> Methods { get; }
        public IDictionary<string, PurgedEnumVal> Values { get; set; }

        public PurgedType(string @namespace, string name)
        {
            Namespace = @namespace;
            Name = name;
            Methods = new SortedDictionary<string, PurgedMethod>();
            Values = new SortedDictionary<string, PurgedEnumVal>();
        }
    }
}