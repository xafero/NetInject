using System;
using System.IO;
using System.Linq;

namespace NetInject.Code
{
    public class CSharpEvent
    {
        public const string Add = "add";
        public const string Remove = "remove";

        public string Name { get; set; }
        public CSharpMethod Remover { get; set; }
        public CSharpMethod Adder { get; set; }

        public CSharpEvent(string name)
        {
            Name = name;
        }

        public string EventType => Adder?.Parameters.FirstOrDefault()?.PType
            ?? Remover?.Parameters.FirstOrDefault()?.PType
            ?? typeof(EventHandler).Name;

        public string ToString(UnitKind kind)
        {
            const string indent = "\t\t";
            using (var writer = new StringWriter())
            {
                writer.WriteLine($"{indent}event {EventType} {Name};");
                return writer.ToString();
            }
        }

        public override string ToString() => ToString(UnitKind.Class);
    }
}