using Mono.Cecil;

namespace NetInject.Purge
{
    public interface IIocProcessor
    {
        MethodDefinition ScopeMethod { get; }
    }
}