using System.Collections.Generic;

namespace NetInject.Inspect
{
    public interface IMethod
    {
        ICollection<IParameter> Parameters { get; }
    }
}