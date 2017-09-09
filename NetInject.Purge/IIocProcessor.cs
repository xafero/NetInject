using Mono.Cecil;
using System;

namespace NetInject.Purge
{
    public interface IIocProcessor
    {
        MethodDefinition ScopeMethod { get; }

        TypeDefinition IocType { get; }

        GenericInstanceMethod GetResolveMethod(TypeReference forType);
    }
}