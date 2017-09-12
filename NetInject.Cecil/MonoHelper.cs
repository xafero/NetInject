using System;
using System.IO;
using Mono.Cecil;

namespace NetInject.Cecil
{
    public static class MonoHelper
    {
        public static void Collect<T>(this ITypeCollector collector)
            => collector.Collect(typeof(T));

        public static void Collect(this ITypeCollector collector, Type type)
        {
            var ass = type.Assembly;
            var file = Path.GetFullPath(ass.Location ?? ass.CodeBase);
            collector.Collect(file);
        }

        public static void Collect(this ITypeCollector collector, string fileName)
        {
            var rparam = new ReaderParameters();
            var ass = AssemblyDefinition.ReadAssembly(fileName, rparam);
            collector.Collect(ass);
        }
    }
}