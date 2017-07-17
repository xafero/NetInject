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

        [Value(1, MetaName = "patches", HelpText = "type:method=value")]
        public IEnumerable<string> Patches { get; set; }
    }

    [Verb("purify", HelpText = "Clean platform-dependent assemblies.")]
    class PurifyOptions
    {
        [Value(0, MetaName = "work", HelpText = "Directory to work in")]
        public string WorkDir { get; set; }

        [Value(1, MetaName = "code", HelpText = "Code generation directory")]
        public string CodeDir { get; set; }
    }

    [Verb("sip", HelpText = "Search an instruction and print.")]
    class SipOptions
    {
        [Value(0, MetaName = "work", HelpText = "Directory to work in")]
        public string WorkDir { get; set; }

        [Value(1, MetaName = "wildcards", HelpText = "asmbl:namesp:type:opcode:term")]
        public IEnumerable<string> Terms { get; set; }
    }

    [Verb("isle", HelpText = "IL stream list and edit.")]
    class IsleOptions
    {
        [Value(0, MetaName = "work", HelpText = "Directory to work in")]
        public string WorkDir { get; set; }

        [Value(1, MetaName = "wildcard", HelpText = "asmbl:namesp:type:opcode:term")]
        public string Term { get; set; }

        [Value(2, MetaName = "patch", HelpText = "opcode:term")]
        public string Patch { get; set; }
    }

    [Verb("adopt", HelpText = "Give up an assembly for adoption.")]
    class AdoptOptions
    {
        [Value(0, MetaName = "work", HelpText = "Directory to work in")]
        public string WorkDir { get; set; }

        [Value(1, MetaName = "parent", HelpText = "The assembly name")]
        public string Parent { get; set; }

        [Value(2, MetaName = "foster", HelpText = "New assembly name")]
        public string Foster { get; set; }
    }

    [Verb("weave", HelpText = "Interweave new code into binary.")]
    class WeaveOptions
    {
        [Value(0, MetaName = "work", HelpText = "Directory to work in")]
        public string WorkDir { get; set; }

        [Value(1, MetaName = "vamp", HelpText = "New code's binary")]
        public string Binary { get; set; }
    }
}