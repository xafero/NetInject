using log4net;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net;
using Mono.Cecil;
using NetInject.Code;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mono.Cecil.Cil;

using static NetInject.IOHelper;
using static NetInject.Code.CodeConvert;
using System.Reflection;

namespace NetInject
{
    static class Purger
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Purger));

        internal static int Invert(InvertOptions opts)
        {
            using (var resolv = new DefaultAssemblyResolver())
            {
                resolv.AddSearchDirectory(opts.WorkDir);
                var rparam = new ReaderParameters { AssemblyResolver = resolv };
                var wparam = new WriterParameters();
                var files = GetAssemblyFiles(opts.WorkDir).ToArray();
                log.Info($"Found {files.Length} file(s)!");
                foreach (var file in files)
                    using (var stream = new MemoryStream(File.ReadAllBytes(file)))
                    {
                        var ass = AssemblyDefinition.ReadAssembly(stream, rparam);
                        log.Info($"'{ass.FullName}'");
                        Invert(ass, opts, wparam, file);
                    }
            }
            return 0;
        }

        static void Invert(AssemblyDefinition ass, InvertOptions opts, WriterParameters wparam, string file)
        {
            var assRefs = ass.Modules.SelectMany(m => m.AssemblyReferences).ToArray();
            var assTypes = ass.Modules.SelectMany(m => m.GetTypeReferences()).ToArray();
            var assMembs = ass.Modules.SelectMany(m => m.GetMemberReferences()).ToArray();
            var isDirty = false;
            foreach (var invRef in assRefs.Where(r => opts.Assemblies.Contains(r.Name)))
            {
                log.Info($" - '{invRef.FullName}'");
                var myTypes = assTypes.Where(t => ContainsType(invRef, t)).ToArray();
                var myMembers = assMembs.Where(m => ContainsMember(invRef, m)).GroupBy(m => m.DeclaringType).ToArray();

                foreach (var myType in myTypes)
                    Console.WriteLine(myType);

                isDirty = false;
            }
            if (!isDirty)
                return;
            ass.Write(file, wparam);
            log.InfoFormat($"Replaced something in '{ass}'!");
        }

        static bool ContainsType(AssemblyNameReference assRef, TypeReference typRef)
          => assRef.FullName == (typRef.Scope as AssemblyNameReference)?.FullName;

        static bool ContainsMember(AssemblyNameReference assRef, MemberReference mbmRef)
          => ContainsType(assRef, mbmRef.DeclaringType);
    }
}