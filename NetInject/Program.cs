using System;
using log4net;
using log4net.Config;
using System.Configuration;
using CommandLine;

namespace NetInject
{
    class Program
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Program).Namespace);

        static int Main(string[] args)
        {
            BasicConfigurator.Configure();
            var cfg = ConfigurationManager.AppSettings;
            const StringComparison cmp = StringComparison.InvariantCultureIgnoreCase;
            int res;
            using (var parser = Parser.Default)
            {
                res = parser.ParseArguments<UnsignOptions, PatchOptions>(args).MapResult(
                      (UnsignOptions opts) => Signer.Unsign(opts),
                      (PatchOptions opts) => Patcher.Modify(opts),
                      errs => 1);
            }
            return res;
        }
    }
}