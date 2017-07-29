using System.IO;
using System.Linq;

namespace NetInject.Code
{
    public class CSharpProperty
    {
        public const string Get = "get";
        public const string Set = "set";

        public string Name { get; set; }
        public CSharpMethod Setter { get; set; }
        public CSharpMethod Getter { get; set; }

        public CSharpProperty(string name)
        {
            Name = name;
        }

        public string PropType => Getter?.ReturnType ?? Setter.Parameters.First().PType;

        public string ToString(UnitKind kind)
        {
            const string indent = "\t\t";
            using (var writer = new StringWriter())
            {
                writer.WriteLine($"{indent}{PropType} {Name} {{");
                if (Getter != null)
                {
                    writer.Write($"{indent}\t{Get}");
                    Getter.WriteJustBody(writer);
                }
                if (Setter != null)
                {
                    writer.Write($"{indent}\t{Set}");
                    Setter.WriteJustBody(writer);
                }
                writer.WriteLine($"{indent}}}");
                return writer.ToString();
            }
        }

        public override string ToString() => ToString(UnitKind.Class);
    }
}