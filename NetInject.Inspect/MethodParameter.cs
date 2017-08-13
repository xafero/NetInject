namespace NetInject.Inspect
{
    public class MethodParameter : IParameter
    {
        public string Type { get; }
        public string Name { get; }

        public MethodParameter(string name, string type)
        {
            Type = type;
            Name = name;
        }
    }
}