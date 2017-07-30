using System;
using System.Collections.Generic;
using System.IO;

using static NetInject.Code.CodeConvert;

namespace NetInject.Code
{
    public class CSharpMethod : IHasParameters
    {
        public string Name { get; set; }
        public IList<CSharpAttribute> Attributes { get; }
        public IList<string> Modifiers { get; }
        public string ReturnType { get; set; }
        public string Body { get; set; }
        public IList<CSharpParameter> Parameters { get; }

        public CSharpMethod(string name)
        {
            Name = name;
            ReturnType = "void";
            Attributes = new List<CSharpAttribute>();
            Modifiers = new List<string> { "static", "extern" };
            Parameters = new List<CSharpParameter>();
        }

        public CSharpMethod(CSharpMethod orig)
        {
            Name = orig.Name;
            Attributes = orig.Attributes;
            Modifiers = orig.Modifiers;
            ReturnType = orig.ReturnType;
            Body = orig.Body;
            Parameters = orig.Parameters;
        }

        public string ToString(UnitKind kind)
        {
            const string indent = "\t\t";
            using (var writer = new StringWriter())
            {
                if (kind != UnitKind.Interface)
                    foreach (var attr in Attributes)
                        writer.WriteLine($"{indent}{attr}");
                var mods = string.Join(" ", Modifiers);
                if (kind == UnitKind.Interface)
                    mods = string.Empty;
                var parms = string.Join(", ", Parameters);
                writer.Write($"{indent}{mods} {Simplify(ReturnType)} {Name}({parms})");
                WriteJustBody(writer);
                return writer.ToString();
            }
        }

        public void WriteJustBody(StringWriter writer)
        {
            if (Body == null)
                writer.WriteLine(";");
            else
                WriteBody(writer, Body);
        }

        static void WriteBody(TextWriter writer, string body)
        {
            if (!body.EndsWith(";", StringComparison.InvariantCulture))
            {
                writer.WriteLine($" => {body};");
                return;
            }
            const string indent = "\t\t";
            writer.WriteLine();
            writer.WriteLine($"{indent}{{");
            writer.WriteLine($"{indent}\t{body.Trim()}");
            writer.WriteLine($"{indent}}}");
        }

        public override string ToString() => ToString(UnitKind.Class);
    }
}