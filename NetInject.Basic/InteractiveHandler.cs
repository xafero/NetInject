using NetInject.API;
using Microsoft.VisualBasic;

namespace NetInject.Basic
{
    public class InteractiveHandler : IInvocationHandler
    {
        public object Invoke(object real, string method, object[] args)
        {
            var txt = string.Join(", ", args);
            var raw = Interaction.InputBox(txt, method, "TEST");
            return raw;
        }
    }
}