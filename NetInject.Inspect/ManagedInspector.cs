using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using NetInject.Cecil;
using static NetInject.Cecil.CecilHelper;

namespace NetInject.Inspect
{
    public class ManagedInspector : IInspector
    {
        private static readonly StringComparer Comp
            = StringComparer.InvariantCultureIgnoreCase;

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

                // PurgedAssemblies purged;

                manageds++;
            }
            return manageds;
        }

        private static bool IsStandardLib(string key)
            => key == "mscorlib" || key == "System" ||
               key == "System.Core" || key == "Microsoft.CSharp";
    }
}

/*

static void InvertAssemblyRef(AssemblyNameReference invRef, PurgedAssemblies purged,
            TypeReference[] assTypes, MemberReference[] assMembs)
        {
            log.Info($" - '{invRef.FullName}'");
            PurgedAssembly purge;
            if (!purged.TryGetValue(invRef.FullName, out purge))
                purged[invRef.FullName] = purge = new PurgedAssembly(invRef.Name, invRef.Version);
            var myTypes = assTypes.Where(t => ContainsType(invRef, t)).ToArray();
            var myMembers = assMembs.Where(m => ContainsMember(invRef, m)).GroupBy(m => m.DeclaringType).ToArray();
            foreach (var myType in myTypes)
            {
                PurgedType ptype;
                if (!purge.Types.TryGetValue(myType.FullName, out ptype))
                    purge.Types[myType.FullName] = ptype = new PurgedType(myType.Namespace, myType.Name);
                var myTypeDef = myType.Resolve();
                if (myTypeDef.IsEnum)
                {
                    foreach (var enumFld in myTypeDef.Fields.Where(f => !f.Name.EndsWith("__", cmpa)).ToArray())
                        ptype.Values[enumFld.Name] = new PurgedEnumVal(enumFld.Name);
                }
                if (myTypeDef.IsClass && myTypeDef.BaseType.FullName == typeof(MulticastDelegate).FullName)
                {
                    var dlgtSig = myTypeDef.Methods.First(m => m.Name == "Invoke");
                    var dlgtMeth = new PurgedMethod(dlgtSig.Name)
                    {
                        ReturnType = dlgtSig.ReturnType.FullName
                    };
                    foreach (var dlgtParm in dlgtSig.Parameters)
                        dlgtMeth.Parameters.Add(new PurgedParam
                        {
                            Name = Escape(dlgtParm.Name),
                            ParamType = dlgtParm.ParameterType.FullName
                        });
                    ptype.Methods[dlgtMeth.Name] = dlgtMeth;
                }
                if (!myTypeDef.IsValueType && !myTypeDef.IsSpecialName && !myTypeDef.IsSealed
                    && !myTypeDef.IsRuntimeSpecialName && !myTypeDef.IsPrimitive
                    && !myTypeDef.IsInterface && !myTypeDef.IsArray && (myTypeDef.IsPublic
                                                                        || myTypeDef.IsNestedPublic) &&
                    myTypeDef.IsClass)
                {
                    var virtuals = myTypeDef.Methods.Where(m => m.IsVirtual || m.IsAbstract).ToArray();
                    if (virtuals.Any())
                    {
                        var derived = myType.Module.Assembly.GetDerivedTypes(myType).ToArray();
                        if (derived.Any())
                        {
                            var overrides = virtuals.Intersect(derived.SelectMany(d => d.Methods)
                                .Where(m => m.IsAbstract || m.IsVirtual), methCmp).ToArray();
                            if (overrides.Any())
                            {
                                var otypeName = myType.Name;
                                var otypeFqn = myType.FullName;
                                PurgedType otype;
                                if (!purge.Types.TryGetValue(otypeFqn, out otype))
                                    purge.Types[otypeFqn] = otype = new PurgedType(myType.Namespace, otypeName);
                                HandleAbstractClass(otype, overrides);
                            }
                        }
                    }
                }
            }
            foreach (var myPair in myMembers)
            {
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
            }
        }

        

        static bool ContainsType(AssemblyNameReference assRef, TypeReference typRef)
            => assRef.FullName == (typRef.Scope as AssemblyNameReference)?.FullName;

        static bool ContainsMember(AssemblyNameReference assRef, MemberReference mbmRef)
            => ContainsType(assRef, mbmRef.DeclaringType);

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
        }
        
        */