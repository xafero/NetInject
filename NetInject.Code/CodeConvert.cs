
namespace NetInject.Code
{
    public static class CodeConvert
    {
        public static string ToStr(object obj)
        {
            var raw = obj;
            if (raw is bool)
                return ((bool)raw) ? "true" : "false";
            if (raw?.GetType().IsEnum ?? false)
                return $"{raw.GetType().Name}.{raw}";
            return $"{raw}";
        }

        public static string Simplify(string type)
        {
            var t = type.TrimEnd('&');
            switch (type)
            {
                case "Void": t = "void"; break;
                case "Boolean": t = "bool"; break;
                case "Int16": t = "short"; break;
                case "Int32": t = "int"; break;
                case "Int64": t = "long"; break;
                case "UInt32": t = "uint"; break;
                case "UInt64": t = "ulong"; break;
                case "Object": t = "object"; break;
                case "String": t = "string"; break;
            }
            return t;
        }
    }
}