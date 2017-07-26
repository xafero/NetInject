using NetInject.API;
using AutoMoq;
using Moq;

namespace NetInject.Moq
{
    public class MoqContainer : IVessel
    {
#pragma warning disable CS0169
        static readonly Microsoft.Practices.Unity.GenericParameter dummy;
#pragma warning restore CS0169

        AutoMoqer Container { get; set; }

        public MoqContainer()
        {
            Container = new AutoMoqer(new Config
            {
                MockBehavior = MockBehavior.Strict
            });
        }

        public T Resolve<T>() => Container.Resolve<T>();

        public void Dispose()
        {
            Container = null;
        }
    }
}