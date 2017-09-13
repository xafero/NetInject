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
            Validate(type, type.Constructors);
        }

        public void Validate(IClass type)
        {
            Validate(type, type.Methods);
            Validate(type, type.Fields);
            Validate(type, type.Operators);
            Validate(type, type.Constructors);
        }

        private void Validate(IHasConstructors holder, IList<IConstructor> constrs)
        {
            foreach (var constr in constrs)
                Validate(constr, constr.Parameters);
        }

        private void Validate(IHasParameters holder, IList<IParameter> parms)
        {
            var doubled = parms.GroupBy(p => p.Name).Where(g => g.Count() >= 2);
            foreach (var pair in doubled)
            {
                var index = 0;
                foreach (var parm in pair)
                {
                    var newName = $"{pair.Key}{++index}";
                    parm.Rename(newName);
                }
            }
        }

        private void Validate(IHasFields holder, IList<IField> fields)
        {
            foreach (var fiel in fields.ToArray())
                if (fiel.Name.Contains("__BackingField") || fiel.Name.Contains("<>")
                    || fiel.Type.Contains("Dictionary2"))
                    holder.Fields.Remove(fiel);
        }

        private void Validate(IHasMethods holder, IList<IMethod> methods)
        {
            foreach (var pair in methods.GroupBy(m => ToString(m)).Where(g => g.Count() >= 2))
                foreach (var meth in pair)
                {
                    var retType = abbreviations[meth.ReturnType];
                    if (retType == null)
                        continue;
                    var newSuffix = abbreviations[retType];
                    var newName = $"{meth.Name}_{newSuffix}";
                    meth.Rename(newName);
                }
            foreach (var meth in methods.ToArray())
                if (meth.Name.Contains("#"))
                    meth.Rename(meth.Name.Replace("#", "Hash"));
                else if (meth.Name.Equals("Equals") || meth.Name.Equals("GetHashCode")
                         || meth.Name.Equals("Finalize") || meth.Name.Equals("ToString") 
                         || meth.Name.StartsWith("_<") 
                         || meth.ReturnType.Contains("Dictionary2")
                         || meth.ReturnType.Contains("Func2"))
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