using System.Collections.Generic;
using NetInject.Cecil;

namespace NetInject.Inspect
{
    public interface IType
    {
        TypeKind Kind { get; }
        
        IDictionary<string, IMethod> Methods { get; }
        
        IDictionary<string, IValue> Values { get; }
    }
}