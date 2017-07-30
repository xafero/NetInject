using System.Collections.Generic;
using System.IO;

namespace NetInject.Code
{
    public class CSharpNamespace
    {
        public string Name { get; }
        public IList<CSharpClass> Classes { get; }
        public IList<CSharpEnum> Enums { get; }

        public CSharpNamespace(string name)
        {
            Name = name;
            Classes = new List<CSharpClass>();
            Enums = new List<CSharpEnum>();
        }

        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                writer.WriteLine($"namespace {Name} {{");
                WriteClasses(writer);
                WriteEnums(writer);
                writer.WriteLine("}");
                writer.Flush();
                return writer.ToString();
            }
        }

        public void WriteClasses(TextWriter writer)
        {
            foreach (var cla in Classes)
                writer.WriteLine(cla);
        }

        public void WriteEnums(TextWriter writer)
        {
            foreach (var enu in Enums)
                writer.WriteLine(enu);
        }
    }
}