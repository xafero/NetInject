using System.Text;

namespace NetInject.Cecil
{
    public static class WordHelper
    {
        public static string Capitalize(string text)
        {
            var bld = new StringBuilder();
            var first = true;
            foreach (var letter in text)
            {
                if (first)
                {
                    bld.Append(char.ToUpper(letter));
                    first = false;
                    continue;
                }
                bld.Append(letter);
                if (letter == '.')
                    first = true;
            }
            return bld.ToString();
        }

        private static string SingleDeobfuscate(string text)
        {
            var buff = new StringBuilder();
            foreach (var letter in text)
                if ((letter >= 'A' && letter <= 'Z') || (letter >= 'a' && letter <= 'z')
                    || (letter >= '0' && letter <= '9') || letter == '.'
                    || letter == '&' || letter == ' ' || letter == '/'
                    || letter == ',' || letter == '(' || letter == ')'
                    || letter == ':' || letter == '[' || letter == ']'
                    || letter == '_' || letter == '<' || letter == '>')
                    buff.Append(letter);
            if (buff.Length >= 1 && !char.IsLetter(buff[0]) && buff[0] != '.' && buff[0] != '_')
                buff.Insert(0, '_');
            return buff.ToString();
        }

        public static string DerivedClassDeobfuscate(string text)
            => Deobfuscate(text.Replace('/', '_'));

        public static string ToName(string text)
            => text.Replace('.', ' ').Replace(" ", "");

        public static string Deobfuscate(string text)
        {
            const char dot = '.';
            var builder = new StringBuilder();
            foreach (var part in text.Split(dot))
                builder.Append(SingleDeobfuscate(part)).Append(dot);
            return builder.ToString().TrimEnd(dot);
        }
    }
}