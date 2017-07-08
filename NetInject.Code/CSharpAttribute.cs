using System.Collections.Generic;
using System.Linq;

using static NetInject.Code.CodeConvert;

namespace NetInject.Code
{
    public class CSharpAttribute
    {
        public string Name { get; }
        public string Value { get; set; }
        public IDictionary<string, object> Properties { get; }

        public CSharpAttribute(string name)
        {
            Name = name.Replace("Attribute", "");
            Properties = new Dictionary<string, object>();
        }

        public override string ToString()
        {
            var items = (new[] { Value }).Concat(Properties.Where(p => p.Value != null).Select(p => $"{p.Key} = {ToStr(p.Value)}"));
            return $"[{Name}({string.Join(", ", items.Where(s => !string.IsNullOrWhiteSpace(s)))})]";
        }
    }
}