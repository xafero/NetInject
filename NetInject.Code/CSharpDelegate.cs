using System.Collections.Generic;
using System.IO;

using static NetInject.Code.CodeConvert;

namespace NetInject.Code
{
    public class CSharpDelegate
    {
        public string Name { get; }
        public IList<string> Modifiers { get; set; }
        public string ReturnType { get; set; }
        public IList<CSharpParameter> Parameters { get; }

        public CSharpDelegate(string name)
        {
            Name = name;
            Modifiers = new List<string> { "public" };
            ReturnType = "void";
            Parameters = new List<CSharpParameter>();
        }

        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                const string indent = "\t";
                var mods = string.Join(" ", Modifiers);
                writer.Write($"{indent}{mods} delegate {Simplify(ReturnType)} {Name}(");
                var parms = string.Join(", ", Parameters);
                writer.Write(parms);
                writer.WriteLine(");");
                return writer.ToString();
            }
        }
    }
}