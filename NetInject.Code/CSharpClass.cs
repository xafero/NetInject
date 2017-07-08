using System.Collections.Generic;
using System.IO;

namespace NetInject.Code
{
    public class CSharpClass
    {
        public UnitKind Kind { get; set; }
        public string Name { get; }
        public IList<string> Bases { get; }
        public IList<CSharpMethod> Methods { get; set; }
        public IList<string> Modifiers { get; set; }

        public CSharpClass(string name)
        {
            Name = name;
            Bases = new List<string>();
            Methods = new List<CSharpMethod>();
            Modifiers = new List<string> { "public" };
        }

        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                const string indent = "\t";
                var bases = Bases.Count == 0 ? string.Empty : $": {string.Join(", ", Bases)} ";
                var mods = string.Join(" ", Modifiers);
                writer.WriteLine($"{indent}{mods} {Kind.ToString().ToLowerInvariant()} {Name} {bases}{{");
                WriteMethods(writer);
                writer.WriteLine($"{indent}}}");
                return writer.ToString();
            }
        }

        public void WriteMethods(TextWriter writer)
        {
            foreach (var meth in Methods)
                writer.WriteLine(meth.ToString(Kind));
        }
    }
}