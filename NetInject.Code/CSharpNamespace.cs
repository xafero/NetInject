using System.Collections.Generic;
using System.IO;

namespace NetInject.Code
{
    public class CSharpNamespace
    {
        public string Name { get; }
        public IList<CSharpClass> Classes { get; }

        public CSharpNamespace(string name)
        {
            Name = name;
            Classes = new List<CSharpClass>();
        }

        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                writer.WriteLine($"namespace {Name} {{");
                WriteClasses(writer);
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
    }
}