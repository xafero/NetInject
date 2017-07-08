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

        public override string ToString()
        {
            const string indent = "\t\t";
            using (var writer = new StringWriter())
            {
                foreach (var attr in Attributes)
                    writer.WriteLine($"{indent}{attr}");
                writer.WriteLine($"{indent}public static extern {ReturnType} {Name}();");
                return writer.ToString();
            }
        }
    }
}