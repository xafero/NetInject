using System;
using System.Linq;
using Noaster.Api;

using Noast = Noaster.Dist.Noaster;
using Microsoft.CSharp;
using System.CodeDom;
using System.Collections.Generic;

namespace NetInject.Purge
{
    public class MethodDeduper : ICodeValidator
    {
        private static readonly TypeAbbreviations abbreviations = new TypeAbbreviations();

        public void Validate(IInterface type) => Validate(type.Methods);

        public void Validate(IStruct type)
        {
            Validate(type.Methods);
            Validate(type, type.Fields);
        }

        private void Validate(IHasFields holder, IList<IField> fields)
        {
            foreach (var fiel in fields.ToArray())
                if (fiel.Name.Contains("__BackingField"))
                    holder.Fields.Remove(fiel);
        }

        private void Validate(IList<IMethod> methods)
        {
            foreach (var meth in methods)
                if (meth.Name.Contains("#"))
                    meth.Rename(meth.Name.Replace("#", "Hash"));
            foreach (var pair in methods.GroupBy(m => ToString(m)).Where(g => g.Count() >= 2))
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