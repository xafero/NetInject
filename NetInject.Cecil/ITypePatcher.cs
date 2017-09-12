using System;
using System.IO;
using Mono.Cecil;

namespace NetInject.Cecil
{
    public interface ITypePatcher
    {
        void Patch(AssemblyDefinition ass, Action<TypeReference> onReplace);

        void Patch(ModuleDefinition mod, Action<TypeReference> onReplace);

        void Patch(TypeDefinition type, Action<TypeReference> onReplace);

        void Patch(MethodDefinition meth, Action<TypeReference> onReplace);

        void Patch(ParameterDefinition param, Action<TypeReference> onReplace);

        void Patch(PropertyDefinition prop, Action<TypeReference> onReplace);

        void Patch(FieldDefinition fiel, Action<TypeReference> onReplace);

        void Patch(EventDefinition evt, Action<TypeReference> onReplace);
    }

    public interface ITypeCollector
    {
        void Collect(AssemblyDefinition ass);
    }

    public class TypeCollector : ITypeCollector
    {
        public void Collect(AssemblyDefinition ass)
        {
        }
    }

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