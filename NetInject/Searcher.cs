using System;
using System.IO;
using System.Linq;

using log4net;
using Mono.Cecil;
using WildcardMatch;

using static NetInject.IOHelper;

namespace NetInject
{
    class Searcher
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Searcher));

        internal static int Find(SipOptions opts)
        {
            var terms = opts.Terms.Select(t => new SearchMask(t)).ToArray();
            log.Info($"{terms.Length} search masks given!");
            var files = GetAssemblyFiles(opts.WorkDir).ToArray();
            log.Info($"Found {files.Length} files!");
            var resolv = new DefaultAssemblyResolver();
            resolv.AddSearchDirectory(opts.WorkDir);
            var rparam = new ReaderParameters { AssemblyResolver = resolv };
            var wparam = new WriterParameters();
            foreach (var file in files)
                using (var stream = new MemoryStream(File.ReadAllBytes(file)))
                {
                    var ass = AssemblyDefinition.ReadAssembly(stream, rparam);
                    var assMask = new SearchMask { Assembly = ass.Name.Name };
                    var assMatched = terms.Where(t => t.Matches(assMask)).ToArray();
                    if (assMatched.Length < 1)
                        continue;
                    var types = ass.GetAllTypes();
                    foreach (var type in types)
                    {
                        var subMask = new SearchMask
                        {
                            Assembly = assMask.Assembly, Namespace = type.Namespace
                        };
                        if (subMask.Namespace.Length < 1)
                            subMask.Namespace = "-";
                        var subMatched = assMatched.Where(t => t.Matches(subMask)).ToArray();
                        if (subMatched.Length < 1)
                            continue;
                        subMask.Type = type.Name;
                        subMatched = subMatched.Where(t => t.Matches(subMask)).ToArray();
                        if (subMatched.Length < 1)
                            continue;

                        
                        


                        Console.WriteLine(subMask + " " + string.Join(", ", subMatched));
                    }
                }
            return 0;
        }

        public struct SearchMask
        {
            public SearchMask(string term)
            {
                var parts = term.Split(':');
                Assembly = parts.GetIndex(0);
                Namespace = parts.GetIndex(1);
                Type = parts.GetIndex(2);
                Opcode = parts.GetIndex(3);
                Argument = parts.GetIndex(4);
            }

            public string Assembly { get; set; }
            public string Namespace { get; set; }
            public string Type { get; set; }
            public string Opcode { get; set; }
            public string Argument { get; set; }

            public override string ToString() =>
                $"{Assembly ?? "*"}:{Namespace ?? "*"}:{Type ?? "*"}:{Opcode ?? "*"}:{Argument ?? "*"}";

            public bool Matches(SearchMask mask)
            {
                if (!string.IsNullOrWhiteSpace(mask.Assembly) && !string.IsNullOrWhiteSpace(Assembly)
                    && !Assembly.WildcardMatch(mask.Assembly, ignoreCase: true))
                    return false;
                if (!string.IsNullOrWhiteSpace(mask.Namespace) && !string.IsNullOrWhiteSpace(Namespace)
                    && !Namespace.WildcardMatch(mask.Namespace, ignoreCase: true))
                    return false;
                if (!string.IsNullOrWhiteSpace(mask.Type) && !string.IsNullOrWhiteSpace(Type)
                    && !Type.WildcardMatch(mask.Type, ignoreCase: true))
                    return false;
                if (!string.IsNullOrWhiteSpace(mask.Opcode) && !string.IsNullOrWhiteSpace(Opcode)
                    && !Opcode.WildcardMatch(mask.Opcode, ignoreCase: true))
                    return false;
                if (!string.IsNullOrWhiteSpace(mask.Argument) && !string.IsNullOrWhiteSpace(Argument)
                    && !Argument.WildcardMatch(mask.Argument, ignoreCase: true))
                    return false;
                return true;
            }
        }
    }
}