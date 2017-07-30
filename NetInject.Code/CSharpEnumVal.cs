namespace NetInject.Code
{
    public class CSharpEnumVal
    {
        public string Name { get; }

        public CSharpEnumVal(string name)
        {
            Name = name;
        }

        public override string ToString() => $"{Name}";
    }
}