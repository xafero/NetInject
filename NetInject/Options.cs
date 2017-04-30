using CommandLine;
using System.Collections.Generic;

namespace NetInject
{
    [Verb("unsign", HelpText = "Remove signing of assemblies.")]
    class UnsignOptions
    {
        [Value(0, MetaName = "input", HelpText = "Directory to copy")]
        public string InputDir { get; set; }

        [Value(1, MetaName = "output", HelpText = "Destination for write")]
        public string OutputDir { get; set; }

        [Value(2, MetaName = "keys", HelpText = "Keys to remove")]
        public IEnumerable<string> UnsignKeys { get; set; }
    }

    [Verb("patch", HelpText = "Patch members to new values.")]
    class PatchOptions
    {
        [Value(0, MetaName = "work", HelpText = "Directory to work in")]
        public string WorkDir { get; set; }

        [Value(2, MetaName = "patches", HelpText = "type:method=value")]
        public IEnumerable<string> Patches { get; set; }
    }
}