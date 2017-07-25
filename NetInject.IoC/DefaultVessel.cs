using NetInject.API;
using NetInject.Autofac;
using NetInject.Moq;

namespace NetInject.IoC
{
    public class DefaultVessel : MultiVessel
    {
        public DefaultVessel() : base(new AutofacContainer()/*, new MoqContainer()*/)
        {
        }
    }
}