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
            switch (t)
            {
                case "System.Void": case "Void": t = "void"; break;
                case "System.IntPtr": t = "IntPtr"; break;
                case "System.Boolean": case "Boolean": t = "bool"; break;
                case "System.SByte": case "SByte": t = "sbyte"; break;
                case "System.Byte": case "Byte": t = "byte"; break;
                case "System.Int16": case "Int16": t = "short"; break;
                case "System.Int32": case "Int32": t = "int"; break;
                case "System.Int64": case "Int64": t = "long"; break;
                case "System.UInt16": case "UInt16": t = "ushort"; break;
                case "System.UInt32": case "UInt32": t = "uint"; break;
                case "System.UInt64": case "UInt64": t = "ulong"; break;
                case "System.Single": case "Single": t = "float"; break;
                case "System.Double": case "Double": t = "double"; break;
                case "System.Decimal": case "Decimal": t = "decimal"; break;
                case "System.Object": case "Object": t = "object"; break;
                case "System.String": case "String": t = "string"; break;
                case "System.Drawing.Icon": t = "Icon"; break;
                case "System.Text.StringBuilder": t = "StringBuilder"; break;
                case "System.TimeSpan": t = "TimeSpan"; break;
                case "System.DateTime": t = "DateTime"; break;
                case "System.Guid": t = "Guid"; break;
            }
            return t;
        }
    }
}