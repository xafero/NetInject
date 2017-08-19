using NetInject.API;
using Microsoft.VisualBasic;
using System;

namespace NetInject.Basic
{
    public class InteractiveHandler : IInvocationHandler
    {
        public T Invoke<T>(object real, string method, object[] args)
        {
            var txt = string.Join(", ", args);
            var raw = Interaction.InputBox(txt, method, "TEST");
            return ConvertTo<T>(raw);
        }

        private T ConvertTo<T>(string raw)
        {
            if (typeof(T) == typeof(long?))
                return (T)(object)long.Parse(raw);
            return (T)Convert.ChangeType(raw, typeof(T));
        }
    }
}