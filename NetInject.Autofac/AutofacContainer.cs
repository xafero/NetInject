using Autofac;
using Autofac.Core.Registration;
using NetInject.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using static NetInject.Autofac.DynamicLoad;

namespace NetInject.Autofac
{
    public class AutofacContainer : IVessel
    {
        private IContainer Container { get; }

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
            asses.AddRange(GetSameDirectoryAssemblies(GetType()));
            var cmp = StringComparison.InvariantCulture;
            var builder = new ContainerBuilder();
            foreach (var ass in asses.Where(a => a != null).Distinct())
                try
                {
                    ass.GetTypes();
                    builder.RegisterAssemblyTypes(ass)
                           .Where(t => t.Name.EndsWith("Service", cmp))
                           .AsImplementedInterfaces();
                }
                catch (ReflectionTypeLoadException)
                {
                    Console.Error.WriteLine($"Could not reflect types from '{ass.Location}!");
                }
            Container = builder.Build();
        }

        public T Resolve<T>() where T : class
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