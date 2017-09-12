using System;
using System.IO;
using Mono.Cecil;
using System.Reflection;

namespace NetInject.Cecil
{
    public static class MonoHelper
    {
        public static void Collect<T>(this ITypeCollector collector)
            => collector.Collect(typeof(T));

        public static void Collect(this ITypeCollector collector, Type type)
        {
            var ass = type.Assembly;
            collector.Collect(ass, t => t.FullName == type.FullName);
        }

        public static void Collect(this ITypeCollector collector, Assembly ass, Func<TypeDefinition, bool> filter = null)
        {
            var file = Path.GetFullPath(ass.Location ?? ass.CodeBase);
            collector.Collect(file, filter);
        }

        public static void Collect(this ITypeCollector collector, string fileName, Func<TypeDefinition, bool> filter = null)
        {
            var dir = Path.GetDirectoryName(fileName);
            using (var resolver = new DefaultAssemblyResolver())
            {
                resolver.AddSearchDirectory(dir);
                var rparam = new ReaderParameters { AssemblyResolver = resolver };
                using (var ass = AssemblyDefinition.ReadAssembly(fileName, rparam))
                {
                    if (filter == null)
                    {
                        collector.Collect(ass);
                        return;
                    }
                    foreach (var type in ass.GetAllTypes())
                        if (filter(type))
                            collector.Collect(type);
                }
            }
        }
    }
}