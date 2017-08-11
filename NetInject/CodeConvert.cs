namespace NetInject
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
                case "System.Void": case "Void": t = "void"; break;
                case "System.IntPtr": t = "IntPtr"; break;
                case "System.Boolean": case "Boolean": t = "bool"; break;
                case "System.Int16": case "Int16": t = "short"; break;
                case "System.Int32": case "Int32": t = "int"; break;
                case "System.Int64": case "Int64": t = "long"; break;
                case "System.UInt32": case "UInt32": t = "uint"; break;
                case "System.UInt64": case "UInt64": t = "ulong"; break;
                case "System.Object": case "Object": t = "object"; break;
                case "System.String": case "String": t = "string"; break;
                case "System.Drawing.Icon": t = "Icon"; break;
            }
            return t;
        }
    }
}