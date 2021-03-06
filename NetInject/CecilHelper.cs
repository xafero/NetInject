﻿using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace NetInject
{
    internal static class CecilHelper
    {
        public static IEnumerable<TypeDefinition> GetAllTypes(this AssemblyDefinition ass)
            => ass.Modules.SelectMany(m => m.GetAllTypes());

        public static IEnumerable<TypeDefinition> GetAllTypes(this ModuleDefinition mod)
            => mod.Types.SelectMany(t => t.GetAllTypes());

        public static IEnumerable<TypeDefinition> GetAllTypes(this TypeDefinition type)
            => (new[] { type }).Concat(type.NestedTypes.SelectMany(t => t.GetAllTypes()));

        public static CharSet? ToCharset(this PInvokeInfo pinv)
           => pinv.IsCharSetAnsi ? CharSet.Ansi : pinv.IsCharSetAuto ? CharSet.Auto :
            pinv.IsCharSetUnicode ? CharSet.Unicode : default(CharSet?);

        public static AssemblyNameReference ToRef(this Assembly ass)
            => new AssemblyNameReference(ass.GetName().Name, ass.GetName().Version);

        public static IEnumerable<string> GetAllNatives(this AssemblyDefinition ass)
            => ass.Modules.SelectMany(m => m.GetAllNatives());

        public static IEnumerable<string> GetAllNatives(this ModuleDefinition mod)
            => mod.ModuleReferences.Select(m => m.Name);

        public static IEnumerable<TypeDefinition> GetDerivedTypes(this AssemblyDefinition ass,
            TypeReference baseType)
        {
            return ass.GetAllTypes().Where(type => type.BaseType == baseType);
        }
    }
}