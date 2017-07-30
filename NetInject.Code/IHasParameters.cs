using System.Collections.Generic;

namespace NetInject.Code
{
    public interface IHasParameters
    {
        IList<CSharpParameter> Parameters { get; }
    }
}