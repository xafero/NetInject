using System.Collections.Generic;

namespace NetInject.Inspect
{
    public interface IUnit
    {
        string Name { get; }

        string Version { get; }

        IDictionary<string, IType> Types { get; }
    }
}