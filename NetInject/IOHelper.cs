using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NetInject
{
    static class IOHelper
    {
        static readonly IEqualityComparer<AssemblyNameReference> assNameComp
            = new AssemblyNameComparer();

        internal static IEnumerable<string> GetAssemblyFiles(string root)
        {
            var opt = SearchOption.AllDirectories;
            var dlls = Directory.EnumerateFiles(root, "*.dll", opt);
            var exes = Directory.EnumerateFiles(root, "*.exe", opt);
            return dlls.Concat(exes);
        }

        internal static IEnumerable<string> GetAllFiles(string root)
        {
            var opt = SearchOption.AllDirectories;
            return Directory.EnumerateFiles(root, "*.*", opt);
        }

        internal static void EnsureWritable(string file)
        {
            var info = new FileInfo(file);
            info.IsReadOnly = false;
        }

        internal static void AddAssemblyByType<T>(AssemblyDefinition ass)
        {
            var refAss = typeof(T).Assembly.GetName();
            var refRef = new AssemblyNameReference(refAss.Name, refAss.Version);
            foreach (var mod in ass.Modules)
                if (!mod.AssemblyReferences.Contains(refRef, assNameComp))
                    mod.AssemblyReferences.Add(refRef);
        }

        internal static Assembly CopyTypeRef<T>(string workDir)
        {
            var ass = typeof(T).Assembly;
            foreach (var refAss in ass.GetReferencedAssemblies().Select(Assembly.Load))
                if (!refAss.GlobalAssemblyCache && !string.IsNullOrWhiteSpace(refAss.Location))
                    CopyTypeRef(refAss, workDir);
            return CopyTypeRef(ass, workDir);
        }

        internal static Assembly CopyTypeRef(Assembly ass, string workDir)
        {
            var assName = Path.GetFileName(ass.Location);
            var assLib = Path.Combine(workDir, assName);
            if (File.Exists(assLib))
                File.Delete(assLib);
            File.Copy(ass.Location, assLib);
            return ass;
        }

        internal static T GetIndex<T>(this T[] array, int index)
        {
            try { return array[index]; } catch (IndexOutOfRangeException) { return default(T); }
        }

        internal static MemoryStream IntoMemory(string file) => new MemoryStream(File.ReadAllBytes(file));

        internal static string Capitalize(string text)
        {
            var bld = new StringBuilder();
            var first = true;
            foreach (var letter in text)
            {
                if (first)
                {
                    bld.Append(char.ToUpper(letter));
                    first = false;
                    continue;
                }
                bld.Append(letter);
                if (letter == '.')
                    first = true;
            }
            return bld.ToString();
        }
    }
}