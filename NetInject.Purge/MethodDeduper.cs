using System.Linq;
using Noaster.Api;
using System.Collections.Generic;

namespace NetInject.Purge
{
    public class MethodDeduper : ICodeValidator
    {
        private static readonly TypeAbbreviations abbreviations = new TypeAbbreviations();

        public void Validate(IInterface type) => Validate(type, type.Methods);

        public void Validate(IStruct type)
        {
            Validate(type, type.Methods);
            Validate(type, type.Fields);
            Validate(type, type.Operators);
        }

        private void Validate(IHasFields holder, IList<IField> fields)
        {
            foreach (var fiel in fields.ToArray())
                if (fiel.Name.Contains("__BackingField"))
                    holder.Fields.Remove(fiel);
        }

        private void Validate(IHasMethods holder, IList<IMethod> methods)
        {
            foreach (var pair in methods.GroupBy(m => ToString(m)).Where(g => g.Count() >= 2))
                foreach (var meth in pair)
                {
                    var retType = abbreviations[meth.ReturnType];
                    var newSuffix = abbreviations[retType];
                    var newName = $"{meth.Name}_{newSuffix}";
                    meth.Rename(newName);
                }
            foreach (var meth in methods.ToArray())
                if (meth.Name.Contains("#"))
                    meth.Rename(meth.Name.Replace("#", "Hash"));
                else if (meth.Name.Equals("Equals") || meth.Name.Equals("GetHashCode"))
                    holder.Methods.Remove(meth);
        }

        private void Validate(IHasOperators holder, IList<IOperator> operators)
        {
            holder.Operators.Clear();
        }

        private string ToString(IMethod method)
            => $"{method.Name}({string.Join(", ", method.Parameters.Select(p => p.Type))})";
    }
}