using System.Collections.Generic;

namespace NetInject.Inspect
{
    public interface IMethod
    {
        ICollection<IParameter> Parameters { get; }

        ICollection<string> Aliases { get; }
    }
}