using System.Collections.Generic;

namespace NetInject.Inspect
{
    public interface IConstraint
    {
        string Name { get; }
        
        ICollection<string> Clauses { get; }
    }
}