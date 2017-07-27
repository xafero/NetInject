using Autofac;
using Autofac.Core.Registration;
using NetInject.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NetInject.Autofac
{
    public class AutofacContainer : IVessel
    {
        IContainer Container { get; }

        public AutofacContainer()
        {
            var asses = new List<Assembly>();
            asses.AddRange(new[]
            {
                Assembly.GetExecutingAssembly(),
                Assembly.GetCallingAssembly(),
                Assembly.GetEntryAssembly()
            });
            asses.AddRange(AppDomain.CurrentDomain.GetAssemblies());
            var cmp = StringComparison.InvariantCulture;
            var builder = new ContainerBuilder();
            foreach (var ass in asses.Where(a => a != null).Distinct())
                builder.RegisterAssemblyTypes(ass)
                       .Where(t => t.Name.EndsWith("Service", cmp))
                       .AsImplementedInterfaces();
            Container = builder.Build();
        }

        public T Resolve<T>()
        {
            try
            {
                return Container.Resolve<T>();
            }
            catch (ComponentNotRegisteredException)
            {
                return default(T);
            }
        }

        public void Dispose()
        {
            Container.Dispose();
        }
    }
}