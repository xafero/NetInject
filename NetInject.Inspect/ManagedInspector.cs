using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Mono.Cecil;
using NetInject.Cecil;
using static NetInject.Cecil.CecilHelper;
using static NetInject.Cecil.WordHelper;

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
            var myTypes = assTypes.Where(t => ContainsType(invRef, t)).ToArray();
            var myMembers = assMembs.Where(m => ContainsMember(invRef, m)).GroupBy(m => m.DeclaringType).ToArray();
            foreach (var myType in myTypes)
            {
                var myTypeDef = myType.Resolve();
                InspectType(report, myType, myTypeDef);
            }
            foreach (var myPair in myMembers)
            {
                var myType = myPair.Key;
                var myTypeDef = myType.Resolve();
                InspectType(report, myType, myTypeDef);
            }
        }

        internal void InspectType(IDependencyReport report, TypeReference typeRef, TypeDefinition typeDef)
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
            switch (kind)
            {
                case TypeKind.Enum:
                    InspectEnum(ptype, typeRef, typeDef);
                    break;
                case TypeKind.Delegate:
                    InspectDelegate(ptype, typeRef, typeDef);
                    break;
                case TypeKind.Struct:
                    InspectStruct(ptype, typeRef, typeDef);
                    break;
                case TypeKind.Interface:
                    InspectInterface(ptype, typeRef, typeDef);
                    break;
                case TypeKind.Class:
                    InspectClass(ptype, typeRef, typeDef);
                    break;
            }
        }

        private void InspectClass(IType type, TypeReference typeRef, TypeDefinition typeDef)
        {
            var virtuals = new MethodDefinition[0];
            var derived = new TypeDefinition[0];
            var overrides = new MethodDefinition[0];
            var isBase = typeDef.IsBaseCandidate()
                         && (virtuals = typeDef.Methods.Where(m => m.IsVirtual || m.IsAbstract).ToArray()).Any()
                         && (derived = typeRef.Module.Assembly.GetDerivedTypes(typeRef).ToArray()).Any()
                         && (overrides = virtuals.Intersect(derived.SelectMany(d => d.Methods)
                             .Where(m => m.IsAbstract || m.IsVirtual), MethCmp).ToArray()).Any();

            Console.WriteLine(typeDef + " is class");
        }

        private void InspectInterface(IType type, TypeReference typeRef, TypeDefinition typeDef)
        {
            Console.WriteLine(typeDef + " is interface");
        }

        private void InspectDelegate(IType type, TypeReference typeRef, TypeDefinition typeDef)
        {
            var dlgtSig = typeDef.Methods.First(m => m.Name == "Invoke");
            var dlgtMeth = new AssemblyMethod(dlgtSig.Name, dlgtSig.ReturnType.FullName);
            foreach (var dlgtParm in dlgtSig.Parameters)
                dlgtMeth.Parameters.Add(new MethodParameter(dlgtParm.Name, dlgtParm.ParameterType.FullName));
            type.Methods[dlgtMeth.Name] = dlgtMeth;
        }

        private void InspectStruct(IType type, TypeReference typeRef, TypeDefinition typeDef)
        {
            Console.WriteLine(typeDef + " is struct");
        }

        private void InspectEnum(IType type, TypeReference typeRef, TypeDefinition typeDef)
        {
            foreach (var enumFld in typeDef.Fields.Where(f => !f.Name.EndsWith("__", Cmpa)).ToArray())
                type.Values[enumFld.Name] = new EnumValue(enumFld.Name);
        }
    }
}

/*
                                var otypeName = myType.Name;
                                var otypeFqn = myType.FullName;
                                PurgedType otype;
                                if (!purge.Types.TryGetValue(otypeFqn, out otype))
                                    purge.Types[otypeFqn] = otype = new PurgedType(myType.Namespace, otypeName);
                                HandleAbstractClass(otype, overrides);
                       
                var myType = myPair.Key;
                PurgedType ptype;
                if (!purge.Types.TryGetValue(myType.FullName, out ptype))
                    purge.Types[myType.FullName] = ptype = new PurgedType(myType.Namespace, myType.Name);
                foreach (var myMember in myPair)
                {
                    PurgedMethod pmethod;
                    if (!ptype.Methods.TryGetValue(myMember.FullName, out pmethod))
                        ptype.Methods[myMember.FullName] = pmethod = new PurgedMethod(myMember.Name);
                    var pmd = (MethodDefinition) myMember.Resolve();
                    if (pmethod.Parameters.Count == 0)
                        foreach (var parm in pmd.Parameters)
                        {
                            var pparm = new PurgedParam
                            {
                                Name = Escape(parm.Name),
                                ParamType = parm.ParameterType.FullName
                            };
                            pmethod.Parameters.Add(pparm);
                        }
                    if (pmd.ReturnType.FullName != typeof(void).FullName)
                        pmethod.ReturnType = pmd.ReturnType.FullName;
                }

        static void HandleAbstractClass(PurgedType fake, MethodDefinition[] overrides)
        {
            foreach (var overrid in overrides)
            {
                PurgedMethod pmethod;
                if (!fake.Methods.TryGetValue(overrid.FullName, out pmethod))
                    fake.Methods[overrid.FullName] = pmethod = new PurgedMethod(overrid.Name);
                if (pmethod.Parameters.Count == 0)
                    foreach (var parm in overrid.Parameters)
                    {
                        var pparm = new PurgedParam
                        {
                            Name = Escape(parm.Name),
                            ParamType = parm.ParameterType.FullName
                        };
                        pmethod.Parameters.Add(pparm);
                    }
                if (overrid.ReturnType.FullName != typeof(void).FullName)
                    pmethod.ReturnType = overrid.ReturnType.FullName;
            }
        } */