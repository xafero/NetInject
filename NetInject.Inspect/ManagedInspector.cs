using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using NetInject.Cecil;
using static NetInject.Cecil.CecilHelper;
using static NetInject.Cecil.WordHelper;
using static NetInject.Inspect.ApiExts;
using GenParmAttr = Mono.Cecil.GenericParameterAttributes;

namespace NetInject.Inspect
{
    public class ManagedInspector : IInspector
    {
        private static readonly StringComparer Comp = StringComparer.InvariantCultureIgnoreCase;
        private static readonly StringComparison Cmpa = StringComparison.InvariantCulture;
        private static readonly MethodDefComparer MethCmp = new MethodDefComparer();

        public IList<string> Filters { get; }

        public ManagedInspector(IEnumerable<string> filters)
        {
            Filters = filters.ToList();
        }

        public int Inspect(AssemblyDefinition ass, IDependencyReport report)
        {
            var manageds = 0;
            var assTypes = ass.GetAllTypeRefs().ToArray();
            var assMembs = ass.GetAllMemberRefs().ToArray();
            foreach (var assRef in ass.Modules.SelectMany(m => m.AssemblyReferences)
                .Where(r => Filters.Count < 1 || Filters.Contains(r.Name, Comp)))
            {
                var key = assRef.Name;
                if (IsStandardLib(key))
                    continue;
                ISet<string> list;
                if (!report.ManagedRefs.TryGetValue(key, out list))
                    report.ManagedRefs[key] = list = new SortedSet<string>();
                list.Add(ass.FullName);
                Process(report, assRef, assTypes, assMembs);
                manageds++;
            }
            return manageds;
        }

        private void Process(IDependencyReport report, AssemblyNameReference invRef,
            IEnumerable<TypeReference> assTypes, IEnumerable<MemberReference> assMembs)
        {
            var mbmFlter = new MemberReference[0];
            var myTypes = assTypes.Where(t => ContainsType(invRef, t)).ToArray();
            var myMembers = assMembs.Where(m => ContainsMember(invRef, m)).GroupBy(m => m.DeclaringType).ToArray();
            foreach (var myType in myTypes)
            {
                var myTypeDef = myType.Resolve();
                InspectType(report, myType, myTypeDef, mbmFlter);
            }
            foreach (var myPair in myMembers)
            {
                var myType = myPair.Key;
                var myMembs = myPair.ToArray();
                var myTypeDef = myType.Resolve();
                InspectType(report, myType, myTypeDef, myMembs);
            }
        }

        internal void InspectType(IDependencyReport report, TypeReference typeRef,
            TypeDefinition typeDef, MemberReference[] myMembers)
        {
            var purged = report.Units;
            var invRef = typeDef.Module.Assembly;
            IUnit purge;
            if (!purged.TryGetValue(invRef.Name.Name, out purge))
                purged[invRef.Name.Name] = purge = new AssemblyUnit(invRef.Name.Name, new Version(0, 0, 0, 0));
            var kind = typeDef.GetTypeKind();
            IType ptype;
            if (!purge.Types.TryGetValue(typeDef.FullName, out ptype))
                purge.Types[typeDef.FullName] = ptype = new AssemblyType(typeDef.FullName, kind);
            var members = myMembers ?? typeDef.GetAllMembers();
            switch (kind)
            {
                case TypeKind.Enum:
                    InspectEnum(ptype, typeDef);
                    break;
                case TypeKind.Delegate:
                    InspectDelegate(ptype, typeDef);
                    break;
                case TypeKind.Struct:
                case TypeKind.Interface:
                    InspectMembers(ptype, members);
                    ExtractGenerics(ptype, typeDef);
                    ExtractBases(ptype, typeDef);
                    break;
                case TypeKind.Class:
                    InspectClass(ptype, typeRef, typeDef, members);
                    break;
            }
        }

        private void InspectClass(IType type, TypeReference typeRef, TypeDefinition typeDef,
            IEnumerable<MemberReference> members)
        {
            var virtuals = new MethodDefinition[0];
            var derived = new TypeDefinition[0];
            var overrides = new MethodDefinition[0];
            var isBase = typeDef.IsBaseCandidate()
                         && (virtuals = typeDef.Methods.Where(m => m.IsVirtual || m.IsAbstract).ToArray()).Any()
                         && (derived = typeRef.Module.Assembly.GetDerivedTypes(typeRef).ToArray()).Any()
                         && (overrides = virtuals.Intersect(derived.SelectMany(d => d.Methods)
                             .Where(m => m.IsAbstract || m.IsVirtual), MethCmp).ToArray()).Any();
            if (isBase)
                members = members.Concat(overrides).Distinct();
            InspectMembers(type, members);
            ExtractGenerics(type, typeDef);
            ExtractBases(type, typeDef);
        }

        private static void ExtractGenerics(IType type, TypeDefinition typeDef)
        {
            if (!typeDef.HasGenericParameters)
                return;
            foreach (var parm in typeDef.GenericParameters)
            {
                var name = parm.FullName;
                var clauses = ToClauses(parm);
                var myParm = new AssemblyConstraint(name, clauses);
                type.Constraints.Add(myParm);
            }
        }

        private static void ExtractBases(IType type, TypeDefinition typeDef)
        {
            var myBase = typeDef.BaseType?.FullName;
            var myIntfs = typeDef.Interfaces.Select(i => i.InterfaceType.FullName);
            if (type.Kind == TypeKind.Class && !string.IsNullOrWhiteSpace(myBase))
                type.Bases.Add(myBase);
            foreach (var myIntf in myIntfs)
                if (!string.IsNullOrWhiteSpace(myIntf))
                    type.Bases.Add(myIntf);
        }

        private static void InspectMembers(IType type, IEnumerable<MemberReference> members)
        {
            foreach (var member in members)
            {
                var methRef = member as MethodReference;
                var meth = methRef?.Resolve() ?? member as MethodDefinition;
                if (meth != null)
                {
                    InspectMethod(type, meth);
                    continue;
                }
                var fldRef = member as FieldReference;
                var fld = fldRef?.Resolve() ?? member as FieldDefinition;
                if (fld != null)
                {
                    InspectField(type, fld);
                    continue;
                }
                var prpRef = member as PropertyReference;
                var prp = prpRef?.Resolve() ?? member as PropertyDefinition;
                if (prp != null)
                {
                    if (prp.GetMethod != null) InspectMethod(type, prp.GetMethod);
                    if (prp.SetMethod != null) InspectMethod(type, prp.SetMethod);
                    continue;
                }
                var evtRef = member as EventReference;
                var evt = evtRef?.Resolve() ?? member as EventDefinition;
                if (evt != null)
                {
                    if (evt.AddMethod != null) InspectMethod(type, evt.AddMethod);
                    if (evt.RemoveMethod != null) InspectMethod(type, evt.RemoveMethod);
                    continue;
                }
                throw new InvalidOperationException(member.GetType().FullName + " / " + member);
            }
        }

        private static void InspectField(IType type, FieldDefinition fld)
        {
            var fldName = Deobfuscate(fld.Name);
            var fldType = Deobfuscate(fld.FieldType.FullName);
            var key = fldName;
            IField pfield;
            if (!type.Fields.TryGetValue(key, out pfield))
                type.Fields[key] = pfield = new AssemblyField(fldName, fldType);
        }

        private static void InspectMethod(IType type, MethodReference meth)
        {
            var methName = Deobfuscate(meth.Name);
            var retType = Deobfuscate(meth.ReturnType.FullName);
            var parms = Deobfuscate(GetParamStr(meth));
            var key = $"{methName} {retType} {parms}";
            IMethod pmethod;
            if (!type.Methods.TryGetValue(key, out pmethod))
                type.Methods[key] = pmethod = new AssemblyMethod(methName, retType);
            pmethod.Parameters.Clear();
            foreach (var parm in meth.Parameters)
            {
                var parmName = Deobfuscate(parm.Name);
                var pparm = new MethodParameter(parmName, Deobfuscate(parm.ParameterType.FullName));
                pmethod.Parameters.Add(pparm);
            }
        }

        private static void InspectDelegate(IType type, TypeDefinition typeDef)
        {
            var dlgtSig = typeDef.Methods.First(m => m.Name == "Invoke");
            var dlgtMeth = new AssemblyMethod(dlgtSig.Name, dlgtSig.ReturnType.FullName);
            foreach (var dlgtParm in dlgtSig.Parameters)
                dlgtMeth.Parameters.Add(new MethodParameter(dlgtParm.Name, dlgtParm.ParameterType.FullName));
            type.Methods[dlgtMeth.Name] = dlgtMeth;
        }

        private static void InspectEnum(IType type, TypeDefinition typeDef)
        {
            var underType = typeDef.GetEnumUnderlyingType();
            if (underType != null && underType.FullName != typeof(int).FullName)
                type.Bases.Add(underType.FullName);
            foreach (var enumFld in typeDef.Fields.Where(f => !f.Name.EndsWith("__", Cmpa)).ToArray())
                type.Values[enumFld.Name] = new EnumValue(enumFld.Name);
        }
    }
}