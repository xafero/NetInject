using System.Collections.Generic;

namespace NetInject.Inspect
{
    public interface IMethod
    {
        string Name { get; }

        ICollection<IParameter> Parameters { get; }

        string ReturnType { get; }
        
        ICollection<string> Aliases { get; }
    }
}