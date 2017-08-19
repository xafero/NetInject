using log4net;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;

namespace NetInject
{
    public static class Compiler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Compiler));

        public static Assembly CreateAssembly(string dir, string name, string[] files, string[] refs = null)
        {
            var provider = CodeDomProvider.CreateProvider("CSharp");
            var options = new CompilerParameters
            {
                GenerateExecutable = false,
                IncludeDebugInformation = true,
                ReferencedAssemblies = {"System.dll", "System.Drawing.dll"},
                GenerateInMemory = false,
                TreatWarningsAsErrors = true,
                OutputAssembly = Path.Combine(dir, name + ".dll")
            };
            if (refs?.Length >= 1)
                options.ReferencedAssemblies.AddRange(refs);
            var res = provider.CompileAssemblyFromFile(options, files);
            if (res.Errors.HasErrors)
                foreach (var err in res.Errors)
                {
                    var errTxt = err.ToString().Trim();
                    errTxt = errTxt.Substring(errTxt.LastIndexOf(Path.DirectorySeparatorChar))
                        .TrimStart(Path.DirectorySeparatorChar);
                    Log.ErrorFormat(" {0}", errTxt);
                }
            return res.CompiledAssembly;
        }
    }
}