using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NetInject.Code
{
    public class CSharpEnum
    {
        public string Name { get; }
        public IList<string> Modifiers { get; set; }
        public ISet<CSharpEnumVal> Values { get; }

        public CSharpEnum(string name)
        {
            Name = name;
            Modifiers = new List<string> { "public" };
            Values = new HashSet<CSharpEnumVal>();
        }

        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                const string indent = "\t";
                var mods = string.Join(" ", Modifiers);
                writer.WriteLine($"{indent}{mods} enum {Name} {{");
                WriteValues(writer);
                writer.WriteLine($"{indent}}}");
                return writer.ToString();
            }
        }

        void WriteValues(StringWriter writer)
        {
            const string indent = "\t\t";
            writer.WriteLine(string.Join(',' + Environment.NewLine,
                Values.Select(v => $"{indent}{v}")));
        }
    }
}