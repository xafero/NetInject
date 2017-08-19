using NetInject.API;
using AutoMoq;
using Moq;

namespace NetInject.Moq
{
    public class MoqContainer : IVessel
    {
#pragma warning disable CS0169
        private static readonly Microsoft.Practices.Unity.GenericParameter dummy;
#pragma warning restore CS0169

        private AutoMoqer Container { get; set; }

        public MoqContainer()
        {
            Container = new AutoMoqer(new Config
            {
                MockBehavior = MockBehavior.Strict
            });
        }

        public T Resolve<T>() where T : class => Container.GetMock<T>().Object;

        public void Dispose()
        {
            Container = null;
        }
    }
}