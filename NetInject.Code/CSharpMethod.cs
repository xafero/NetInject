using System;
using System.Collections.Generic;
using System.IO;

namespace NetInject.Code
{
    public class CSharpMethod
    {
        public string Name { get; set; }
        public IList<CSharpAttribute> Attributes { get; }
        public string ReturnType { get; set; }

        public CSharpMethod(string name)
        {
            Name = name;
            ReturnType = "void";
            Attributes = new List<CSharpAttribute>();
        }

        public string ToString(UnitKind kind)
        {
            const string indent = "\t\t";
            using (var writer = new StringWriter())
            {
                if (kind != UnitKind.Interface)
                    foreach (var attr in Attributes)
                        writer.WriteLine($"{indent}{attr}");
                var mods = "public static extern";
                if (kind == UnitKind.Interface)
                    mods = string.Empty;
                writer.WriteLine($"{indent}{mods} {ReturnType} {Name}();");
                return writer.ToString();
            }
        }

        public override string ToString() => ToString(UnitKind.Class);
    }
}