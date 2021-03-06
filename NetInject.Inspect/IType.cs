﻿using System.Collections.Generic;
using NetInject.Cecil;

namespace NetInject.Inspect
{
    public interface IType : IHasConstraints
    {
        string Namespace { get; }

        string Name { get; }

        TypeKind Kind { get; }

        ICollection<string> Bases { get; }

        IDictionary<string, IField> Fields { get; }

        IDictionary<string, IMethod> Methods { get; }

        IDictionary<string, IValue> Values { get; }
    }
}