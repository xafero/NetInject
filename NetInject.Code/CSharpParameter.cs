
namespace NetInject.Code
{
    public class CSharpParameter
    {
        public string PType { get; }
        public string Name { get; }
        public bool IsRef { get; set; }

        public CSharpParameter(string ptype, string name)
        {
            PType = ptype;
            Name = name;
        }

        public override string ToString()
        {
            var mod = IsRef ? "ref " : string.Empty;
            var text = $"{mod}{PType} {Name}";
            return text;
        }
    }
}