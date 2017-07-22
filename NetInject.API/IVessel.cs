using System;

namespace NetInject.API
{
    public interface IVessel : IDisposable
    {
        T Resolve<T>();
    }
}