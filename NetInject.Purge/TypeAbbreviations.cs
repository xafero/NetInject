using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace NetInject.Purge
{
    internal class TypeAbbreviations
    {
        private readonly IDictionary<string, Type> _mappings = new Dictionary<string, Type>
            {
                { "Z", typeof(bool) },
                { "B", typeof(byte) },
                { "S", typeof(short) },
                { "I", typeof(int) },
                { "P", typeof(IntPtr) },
                { "J", typeof(long) },
                { "U", typeof(ulong) },
                { "F", typeof(float) },
                { "D", typeof(double) },
                { "C", typeof(char) },
                { "L", typeof(object) },
                { "N", null }
            };

        private readonly IDictionary<string, Type> _shortNames = new Dictionary<string, Type>();
        private readonly IDictionary<string, Type> _longNames = new Dictionary<string, Type>();

        public TypeAbbreviations()
        {
            using (var provider = new CSharpCodeProvider())
                foreach (var map in _mappings.Where(m => m.Value != null))
                {
                    var typeRef = new CodeTypeReference(map.Value);
                    var name = provider.GetTypeOutput(typeRef);
                    _shortNames[name.Replace("System.", "")] = map.Value;
                    _longNames[map.Value.FullName] = map.Value;
                }
        }

        public Type this[string alias]
        {
            get
            {
                Type type;
                if (_mappings.TryGetValue(alias, out type))
                    return type;
                if (_shortNames.TryGetValue(alias, out type))
                    return type;
                if (_longNames.TryGetValue(alias, out type))
                    return type;
                return null;
            }
        }

        public string this[Type type]
        {
            get
            {
                string alias;
                if ((alias = _mappings.FirstOrDefault(m => m.Value == type).Key) != null)
                    return alias;
                throw new InvalidOperationException(type?.FullName);
            }
        }
    }
}