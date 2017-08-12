using log4net;
using log4net.Config;
using System.Configuration;
using CommandLine;

namespace NetInject
{
    static class Program
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Program).Namespace);

        static int Main(string[] args)
        {
            BasicConfigurator.Configure();
            var cfg = ConfigurationManager.AppSettings;
            int res;
            using (var parser = Parser.Default)
            {
                res = parser.ParseArguments<UnsignOptions, PatchOptions, PurifyOptions, SipOptions, IsleOptions, AdoptOptions, WeaveOptions, InvertOptions, UsagesOptions>(args).MapResult(
                      (UnsignOptions opts) => Signer.Unsign(opts),
                      (PatchOptions opts) => Patcher.Modify(opts),
                      (PurifyOptions opts) => Purifier.Clean(opts),
                      (SipOptions opts) => Searcher.Find(opts),
                      (IsleOptions opts) => Island.Replace(opts),
                      (AdoptOptions opts) => Adopter.Force(opts),
                      (WeaveOptions opts) => Weaver.Web(opts),
                      (InvertOptions opts) => Purger.Invert(opts),
                      (UsagesOptions opts) => Usager.Poll(opts),
                      errs => 1);
            }
            return res;
        }
    }
}