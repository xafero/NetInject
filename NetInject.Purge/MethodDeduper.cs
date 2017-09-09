using System;
using System.Linq;
using Noaster.Api;

using Noast = Noaster.Dist.Noaster;
using Microsoft.CSharp;
using System.CodeDom;

namespace NetInject.Purge
{
    public class MethodDeduper : ICodeValidator
    {
        private static readonly TypeAbbreviations abbreviations = new TypeAbbreviations();

        public void Validate(IInterface intf)
        {
            foreach (var pair in intf.Methods.GroupBy(m => ToString(m)).Where(g => g.Count() >= 2))
                foreach (var meth in pair)
                {
                    var retType = abbreviations[meth.ReturnType];
                    var newSuffix = abbreviations[retType];
                    var newName = $"{meth.Name}_{newSuffix}";
                    meth.Rename(newName);
                }
        }

        private string ToString(IMethod method)
            => $"{method.Name}({string.Join(", ", method.Parameters.Select(p => p.Type))})";
    }
}