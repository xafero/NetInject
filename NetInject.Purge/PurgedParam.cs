namespace NetInject.Purge
{
    public class PurgedParam
    {
        public string Name { get; set; } = "unknown";

        public string ParamType { get; set; } = typeof(object).FullName;
    }
}