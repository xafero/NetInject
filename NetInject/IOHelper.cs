using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NetInject
{
    static class IOHelper
    {
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

        internal static Assembly CopyTypeRef<T>(string workDir)
        {
            var ass = typeof(T).Assembly;
            var assName = Path.GetFileName(ass.Location);
            var assLib = Path.Combine(workDir, assName);
            if (File.Exists(assLib))
                File.Delete(assLib);
            File.Copy(ass.Location, assLib);
            return ass;
        }
    }
}