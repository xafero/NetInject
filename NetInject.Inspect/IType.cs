using System.Collections.Generic;

namespace NetInject.Inspect
{
    public interface IType
    {
        IDictionary<string, IMethod> Methods { get; }
    }
}