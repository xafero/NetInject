using log4net;
using Mono.Cecil;
using System;
using System.Linq;
using System.IO;

using static NetInject.IOHelper;
using NetInject.API;
using NetInject.Autofac;
using NetInject.Moq;

namespace NetInject
{
    static class Purger
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Purger));

        internal static int Invert(InvertOptions opts)
        {
            var isDirty = false;
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
                        Invert(ass, opts, wparam, file, ref isDirty);
                    }
            }
            if (isDirty)
            {
                log.InfoFormat("Added '{0}'!", CopyTypeRef<IVessel>(opts.WorkDir));
                log.InfoFormat("Added '{0}'!", CopyTypeRef<AutofacContainer>(opts.WorkDir));
                log.InfoFormat("Added '{0}'!", CopyTypeRef<MoqContainer>(opts.WorkDir));
            }
            return 0;
        }

        static void Invert(AssemblyDefinition ass, InvertOptions opts,
            WriterParameters wparam, string file, ref bool isOneDirty)
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



                // Add basic references
                AddAssemblyByType<IVessel>(ass);
                AddAssemblyByType<AutofacContainer>(ass);
                AddAssemblyByType<MoqContainer>(ass);
                // Set dirty flag
                isDirty = true;
                isOneDirty = true;
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