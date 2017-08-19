using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using GenParmAttr = Mono.Cecil.GenericParameterAttributes;

namespace NetInject.Inspect
{
    public static class ApiExts
    {
        public static bool IsPInvoke(this IMethod meth)
            => meth.Aliases.Any();

        public static int BeNonNegative(this int value, int defaultVal)
            => value >= 0 ? value : defaultVal;

        public static string[] ToClauses(GenericParameter parm)
        {
            var isOut = parm.Attributes.HasFlag(GenParmAttr.Covariant);
            var isIn = parm.Attributes.HasFlag(GenParmAttr.Contravariant);
            var whereNew = parm.Attributes.HasFlag(GenParmAttr.DefaultConstructorConstraint);
            var whereClass = parm.Attributes.HasFlag(GenParmAttr.ReferenceTypeConstraint);
            var whereStruct = parm.Attributes.HasFlag(GenParmAttr.NotNullableValueTypeConstraint);
            var whereBases = parm.Constraints.Select(c => c.FullName).ToArray();
            var clauses = new List<string>();
            if (isOut) clauses.Add("#out");
            if (isIn) clauses.Add("#in");
            if (whereNew) clauses.Add("#new");
            if (whereClass) clauses.Add("#class");
            if (whereStruct) clauses.Add("#struct");
            if (whereBases.Length >= 1) clauses.AddRange(whereBases);
            return clauses.ToArray();
        }
    }
}