using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace NetInject.Cecil
{
    public class TypeSuggestor : ITypeSuggestor
    {
        private readonly IDictionary<TypeReference, TypeReference> _dict;

        public TypeSuggestor(IDictionary<TypeReference, TypeReference> dict)
        {
            _dict = dict;
        }

        public TypeReference this[TypeReference type, ITypeImporter import]
        {
            get
            {
                TypeReference tNew;
                if (_dict.TryGetValue(type, out tNew))
                    return import.Import(tNew);
                var altKey = _dict.Keys.FirstOrDefault(r => r.FullName == type.FullName);
                if (altKey != null && _dict.TryGetValue(altKey, out tNew))
                    return import.Import(tNew);
                ByReferenceType byRef;
                if ((byRef = type as ByReferenceType) != null)
                    return this[byRef.ElementType, import].MakeByReferenceType();
                ArrayType arType;
                if ((arType = type as ArrayType) != null)
                    return this[arType.ElementType, import].MakeArrayType(arType.Rank).ApplyDims(arType);
                GenericInstanceType gnType;
                if ((gnType = type as GenericInstanceType) != null)
                {
                    var args = gnType.GenericArguments.Select(a => this[a, import]).ToArray();
                    return this[gnType.ElementType, import].MakeGenericInstanceType(args);
                }
                if (type.IsInStandardLib())
                    return import.Import(type);
                return type;
            }
        }
    }
}