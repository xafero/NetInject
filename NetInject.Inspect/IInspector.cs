using Mono.Cecil;

namespace NetInject.Inspect
{
    public interface IInspector
    {
        int Inspect(AssemblyDefinition ass, IDependencyReport report);
    }
}