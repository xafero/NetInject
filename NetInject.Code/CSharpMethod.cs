using System;
using System.Collections.Generic;
using System.IO;

using static NetInject.Code.CodeConvert;

namespace NetInject.Code
{
    public class CSharpMethod
    {
        public string Name { get; set; }
        public IList<CSharpAttribute> Attributes { get; }
        public IList<string> Modifiers { get; }
        public string ReturnType { get; set; }
        public string Body { get; set; }

        public CSharpMethod(string name)
        {
            Name = name;
            ReturnType = "void";
            Attributes = new List<CSharpAttribute>();
            Modifiers = new List<string> { "static", "extern" };
        }

        public CSharpMethod(CSharpMethod orig)
        {
            Name = orig.Name;
            Attributes = orig.Attributes;
            Modifiers = orig.Modifiers;
            ReturnType = orig.ReturnType;
            Body = orig.Body;
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
                writer.Write($"{indent}{mods} {Simplify(ReturnType)} {Name}()");
                if (Body == null)
                    writer.WriteLine(";");
                else
                    WriteBody(writer, Body);
                return writer.ToString();
            }
        }

        static void WriteBody(TextWriter writer, string body)
        {
            if (!body.Contains(Environment.NewLine))
            {
                writer.WriteLine($" => {body};");
                return;
            }
            writer.WriteLine();
            writer.WriteLine("{");
            writer.WriteLine(body.Trim());
            writer.WriteLine("}");
        }

        public override string ToString() => ToString(UnitKind.Class);
    }
}