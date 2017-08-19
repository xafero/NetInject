using System.Collections.Generic;

namespace NetInject.Inspect
{
    public interface IHasConstraints
    {
        ICollection<IConstraint> Constraints { get; }
    }
}