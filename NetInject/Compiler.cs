using log4net;
using System.CodeDom.Compiler;
using System.Reflection;

namespace NetInject
{
    public static class Compiler
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Compiler));

        public static Assembly CreateAssembly(string name, string[] files, string[] refs = null)
        {
            var provider = CodeDomProvider.CreateProvider("CSharp");
            var options = new CompilerParameters
            {
                GenerateExecutable = false,
                IncludeDebugInformation = true,
                ReferencedAssemblies = { "System.dll" },
                GenerateInMemory = false,
                TreatWarningsAsErrors = true,
                OutputAssembly = name + ".dll"
            };
            if (refs?.Length >= 1)
                options.ReferencedAssemblies.AddRange(refs);
            var res = provider.CompileAssemblyFromFile(options, files);
            if (res.Errors.HasErrors)
                foreach (var err in res.Errors)
                    log.ErrorFormat("{0}", err);
            return res.CompiledAssembly;
        }
    }
}