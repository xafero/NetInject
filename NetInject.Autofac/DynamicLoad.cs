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
                var otherAss = Assembly.LoadFrom(otherLoc);
                yield return otherAss;
            }
        }
    }
}