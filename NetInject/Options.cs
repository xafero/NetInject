using CommandLine;
using System.Collections.Generic;

namespace NetInject
{
    [Verb("unsign", HelpText = "Remove signing of assemblies.")]
    internal class UnsignOptions
    {
        [Value(0, MetaName = "input", HelpText = "Directory to copy")]
        public string InputDir { get; set; }

        [Value(1, MetaName = "output", HelpText = "Destination for write")]
        public string OutputDir { get; set; }

        [Value(2, MetaName = "keys", HelpText = "Keys to remove")]
        public IEnumerable<string> UnsignKeys { get; set; }
    }

    [Verb("patch", HelpText = "Patch members to new values.")]
    internal class PatchOptions
    {
        [Value(0, MetaName = "work", HelpText = "Directory to work in")]
        public string WorkDir { get; set; }

        [Value(1, MetaName = "patches", HelpText = "type:method=value")]
        public IEnumerable<string> Patches { get; set; }
    }

    [Verb("purify", HelpText = "Clean platform-dependent assemblies.")]
    internal class PurifyOptions
    {
        [Value(0, MetaName = "work", HelpText = "Directory to work in")]
        public string WorkDir { get; set; }

        [Value(1, MetaName = "code", HelpText = "Code generation directory")]
        public string CodeDir { get; set; }
    }

    [Verb("sip", HelpText = "Search an instruction and print.")]
    internal class SipOptions
    {
        [Value(0, MetaName = "work", HelpText = "Directory to work in")]
        public string WorkDir { get; set; }

        [Value(1, MetaName = "wildcards", HelpText = "asmbl:namesp:type:opcode:term")]
        public IEnumerable<string> Terms { get; set; }
    }

    [Verb("isle", HelpText = "IL stream list and edit.")]
    internal class IsleOptions
    {
        [Value(0, MetaName = "work", HelpText = "Directory to work in")]
        public string WorkDir { get; set; }

        [Value(1, MetaName = "wildcard", HelpText = "asmbl:namesp:type:opcode:term")]
        public string Term { get; set; }

        [Value(2, MetaName = "patch", HelpText = "opcode:term")]
        public string Patch { get; set; }
    }

    [Verb("adopt", HelpText = "Give up an assembly for adoption.")]
    internal class AdoptOptions
    {
        [Value(0, MetaName = "work", HelpText = "Directory to work in")]
        public string WorkDir { get; set; }

        [Value(1, MetaName = "parent", HelpText = "The assembly name")]
        public string Parent { get; set; }

        [Value(2, MetaName = "foster", HelpText = "New assembly name")]
        public string Foster { get; set; }
    }

    [Verb("weave", HelpText = "Interweave new code into binary.")]
    internal class WeaveOptions
    {
        [Value(0, MetaName = "work", HelpText = "Directory to work in")]
        public string WorkDir { get; set; }

        [Value(1, MetaName = "vamp", HelpText = "New code's binary")]
        public string Binary { get; set; }
    }

    internal interface IUsageOpts
    {
        string WorkDir { get; }

        IEnumerable<string> Assemblies { get; }
    }

    [Verb("invert", HelpText = "Apply inversion of control.")]
    internal class InvertOptions : IUsageOpts
    {
        [Value(0, MetaName = "work", HelpText = "Directory to work in")]
        public string WorkDir { get; set; }

        [Value(1, MetaName = "temp", HelpText = "Directory for temp stuff")]
        public string TempDir { get; set; }

        [Value(2, MetaName = "output", HelpText = "Directory for results")]
        public string OutputDir { get; set; }

        [Value(3, MetaName = "legacy", HelpText = "Old dependencies to purge")]
        public IEnumerable<string> Assemblies { get; set; }
    }

    [Verb("usages", HelpText = "Poll assemblies for detailed usages.")]
    internal class UsagesOptions : IUsageOpts
    {
        [Value(0, MetaName = "work", HelpText = "Directory to work in")]
        public string WorkDir { get; set; }

        [Value(1, MetaName = "legacy", HelpText = "Dependency name filter")]
        public IEnumerable<string> Assemblies { get; set; }
    }
}