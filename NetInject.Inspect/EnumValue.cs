namespace NetInject.Inspect
{
    internal class EnumValue : IValue
    {
        public string Name { get; }

        public EnumValue(string name)
        {
            Name = name;
        }
    }
}