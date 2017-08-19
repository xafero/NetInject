using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace NetInject.Autofac
{
    public static class DynamicLoad
    {
        public static IEnumerable<Assembly> GetSameDirectoryAssemblies(Type type)
        {
            var selfLoc = Path.GetFullPath(type.Assembly.Location);
            var selfDir = Path.GetDirectoryName(selfLoc);
            foreach (var otherLoc in Directory.GetFiles(selfDir, "*.dll"))
            {
                var otherAss = LoadAssembly(otherLoc);
                yield return otherAss;
            }
        }

        private static Assembly LoadAssembly(string file)
        {
            try
            {
                return Assembly.LoadFrom(file);
            }
            catch (BadImageFormatException)
            {
                Console.Error.WriteLine($"Could not read image from '{file}'!");
                return null;
            }
        }
    }
}