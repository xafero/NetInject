using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace NetInject
{
    class Captivator : IParser
    {
        public IList<IMetadataTokenProvider> MembersToDelete { get; } = new List<IMetadataTokenProvider>();

        public IResolved Parse(Instruction il, string methStr,
            IDictionary<string, string> myMappings, IDictionary<string, AssemblyDefinition> gens)
        {
            string nativeFqName;
            if (!myMappings.TryGetValue(methStr, out nativeFqName))
                return null;
            var nativeTypeFn = nativeFqName.Substring(0, nativeFqName.LastIndexOf('.'));
            var nativeAss = nativeTypeFn.Replace(Purger.apiPrefix, "").Substring(0, nativeTypeFn.IndexOf('.') + 1);
            var nativeMethName = nativeFqName.Replace(nativeTypeFn, string.Empty).TrimStart('.');
            var genAss = gens.FirstOrDefault(g => g.Key == $"{nativeAss}{Purger.apiSuffix}").Value;
            if (genAss == null)
                return null;
            var nativeType = genAss.GetAllTypes().FirstOrDefault(g => g.FullName == nativeTypeFn);
            if (nativeType == null)
                return null;
            var nativeMeth = nativeType.Methods.FirstOrDefault(m => m.Name == nativeMethName);
            if (nativeMeth == null)
                return null;
            if (il.OpCode == OpCodes.Call)
            {
                MembersToDelete.Add((IMetadataTokenProvider)il.Operand);
                return new Resolved { NewMethod = nativeMeth, NewType = nativeType };
            }
            return null;
        }

        class Resolved : IResolved
        {
            public MethodDefinition NewMethod { get; set; }
            public TypeDefinition NewType { get; set; }
        }
    }

    interface IParser
    {
        IList<IMetadataTokenProvider> MembersToDelete { get; }

        IResolved Parse(Instruction il, string methStr,
            IDictionary<string, string> myMappings, IDictionary<string, AssemblyDefinition> gens);
    }

    interface IResolved
    {
        MethodDefinition NewMethod { get; set; }
        TypeDefinition NewType { get; set; }
    }
}