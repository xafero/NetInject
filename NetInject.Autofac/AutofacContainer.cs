using Autofac;
using Autofac.Core.Registration;
using NetInject.API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using static NetInject.Autofac.DynamicLoad;
using IContainer = Autofac.IContainer;

namespace NetInject.Autofac
{
    public class AutofacContainer : IVessel
    {
        private const string ServiceSuffix = "Service";
        private const StringComparison cmp = StringComparison.InvariantCulture;

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
            var builder = new ContainerBuilder();
            foreach (var ass in asses.Where(a => a != null).Distinct())
                try
                {
                    ass.GetTypes();
                    builder.RegisterAssemblyTypes(ass).Where(IsConventional)
                        .AsImplementedInterfaces().SingleInstance();
                    builder.RegisterAssemblyTypes(ass).Where(HasName)
                        .Named<Func<Type, string>>(GetName).SingleInstance();
                    // .ExternallyOwned()
                    // .InstancePerLifetimeScope()
                    // .AsSelf()
                }
                catch (ReflectionTypeLoadException)
                {
                    if (!ass.FullName.StartsWith("Microsoft."))
                        Console.Error.WriteLine($"Could not reflect types from '{ass.Location}!");
                }
            Container = builder.Build();
        }

        private static string GetName(Type t)
            => t.GetCustomAttribute<DescriptionAttribute>().Description;

        private static bool HasName(Type t)
            => t.Name.EndsWith(ServiceSuffix, cmp) && t.GetCustomAttribute<DescriptionAttribute>(false) != null;

        private static bool IsConventional(Type t)
            => t.Name.EndsWith(ServiceSuffix, cmp) && t.GetCustomAttributes(false).Length == 0;

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