using System.Collections.Generic;

namespace NetInject.Inspect
{
    public interface IUnit
    {
        IDictionary<string, IType> Types { get; }
    }
}