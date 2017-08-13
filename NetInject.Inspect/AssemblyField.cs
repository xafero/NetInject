namespace NetInject.Inspect
{
    internal class AssemblyField : IField
    {
        public string Type { get; }
        public string Name { get; }

        public AssemblyField(string name, string fldType)
        {
            Name = name;
            Type = fldType;
        }
    }
}