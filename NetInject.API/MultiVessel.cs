using System;
using System.Collections.Generic;

namespace NetInject.API
{
    public class MultiVessel : IVessel
    {
        IVessel[] Vessels { get; set; }

        public MultiVessel(params IVessel[] vessels)
        {
            Vessels = vessels;
        }

        public T Resolve<T>()
        {
            var resolved = default(T);
            foreach (var vessel in Vessels)
            {
                resolved = vessel.Resolve<T>();
                if (!EqualityComparer<T>.Default.Equals(resolved, default(T)))
                    break;
            }
            return resolved;
        }

        public void Dispose()
        {
            Array.ForEach(Vessels, v => v.Dispose());
            Vessels = null;
        }
    }
}