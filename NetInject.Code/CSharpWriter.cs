using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetInject.Code
{
    public class CSharpWriter : IDisposable
    {
        readonly Stream stream;
        readonly TextWriter writer;

        public ISet<string> Usings { get; }
        public string Namespace { get; set; }
        public string Name { get; set; }
        public IList<CSharpMethod> Methods { get; set; }

        public CSharpWriter(Stream stream)
        {
            this.stream = stream;
            writer = new StreamWriter(stream, Encoding.UTF8);
            Usings = new HashSet<string>();
            Methods = new List<CSharpMethod>();
        }

        public void WriteUsings()
        {
            foreach (var usin in Usings)
                writer.WriteLine($"using {usin};");
            writer.WriteLine();
        }

        public void WriteNamespace()
        {
            writer.WriteLine($"namespace {Namespace} {{");
            WriteClass();
            writer.WriteLine("}");
            writer.Flush();
        }

        public void WriteClass(string indent = "\t")
        {
            writer.WriteLine($"{indent}class {Name} {{");
            WriteMethods();
            writer.WriteLine($"{indent}}}");
        }

        public void WriteMethods()
        {
            foreach (var meth in Methods)
                writer.WriteLine(meth);
        }

        public void Dispose()
        {
            writer.Dispose();
            stream.Dispose();
        }
    }
}