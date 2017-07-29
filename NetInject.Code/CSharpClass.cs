using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NetInject.Code
{
    public class CSharpClass
    {
        readonly StringComparison cmp = StringComparison.InvariantCultureIgnoreCase;

        public UnitKind Kind { get; set; }
        public string Name { get; }
        public IList<string> Bases { get; }
        public IList<CSharpMethod> Methods { get; set; }
        public IList<CSharpProperty> Properties { get; set; }
        public IList<CSharpEvent> Events { get; set; }
        public IList<string> Modifiers { get; set; }

        public CSharpClass(string name)
        {
            Name = name;
            Bases = new List<string>();
            Methods = new List<CSharpMethod>();
            Properties = new List<CSharpProperty>();
            Events = new List<CSharpEvent>();
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
            {
                if (meth.Name.StartsWith(CSharpProperty.Get + '_', cmp))
                {
                    var name = meth.Name.Substring(CSharpProperty.Get.Length + 1);
                    var prop = Properties.FirstOrDefault(p => p.Name == name);
                    if (prop == null)
                    {
                        prop = new CSharpProperty(name);
                        Properties.Add(prop);
                    }
                    prop.Getter = meth;
                    continue;
                }
                if (meth.Name.StartsWith(CSharpProperty.Set + '_', cmp))
                {
                    var name = meth.Name.Substring(CSharpProperty.Set.Length + 1);
                    var prop = Properties.FirstOrDefault(p => p.Name == name);
                    if (prop == null)
                    {
                        prop = new CSharpProperty(name);
                        Properties.Add(prop);
                    }
                    prop.Setter = meth;
                    continue;
                }
                if (meth.Name.StartsWith(CSharpEvent.Add + '_', cmp))
                {
                    var name = meth.Name.Substring(CSharpEvent.Add.Length + 1);
                    var evt = Events.FirstOrDefault(e => e.Name == name);
                    if (evt == null)
                    {
                        evt = new CSharpEvent(name);
                        Events.Add(evt);
                    }
                    evt.Adder = meth;
                    continue;
                }
                if (meth.Name.StartsWith(CSharpEvent.Remove + '_', cmp))
                {
                    var name = meth.Name.Substring(CSharpEvent.Remove.Length + 1);
                    var evt = Events.FirstOrDefault(e => e.Name == name);
                    if (evt == null)
                    {
                        evt = new CSharpEvent(name);
                        Events.Add(evt);
                    }
                    evt.Remover = meth;
                    continue;
                }
                writer.WriteLine(meth.ToString(Kind));
            }
            foreach (var prop in Properties)
                writer.WriteLine(prop.ToString(Kind));
            foreach (var evt in Events)
                writer.WriteLine(evt.ToString(Kind));
        }
    }
}