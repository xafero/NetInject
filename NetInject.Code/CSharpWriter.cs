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
        public IList<CSharpNamespace> Namespaces { get; set; }

        public CSharpWriter(Stream stream)
        {
            this.stream = stream;
            writer = new StreamWriter(stream, Encoding.UTF8);
            Usings = new HashSet<string>();
            Namespaces = new List<CSharpNamespace>();
        }

        public void WriteUsings()
        {
            foreach (var usin in Usings)
                writer.WriteLine($"using {usin};");
            writer.WriteLine();
        }

        public void WriteNamespaces()
        {
            foreach (var name in Namespaces)
                writer.WriteLine(name);
        }

        public void Dispose()
        {
            writer.Dispose();
            stream.Dispose();
        }
    }
}