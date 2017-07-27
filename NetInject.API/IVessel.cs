using System;

namespace NetInject.API
{
    public interface IVessel : IDisposable
    {
        T Resolve<T>() where T : class;
    }
}