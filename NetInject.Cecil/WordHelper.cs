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
        
        public static string Deobfuscate(string text)
        {
            var buff = new StringBuilder();
            foreach (var letter in text)
                if ((letter >= 'A' && letter <= 'Z') || (letter >= 'a' && letter <= 'z')
                    || (letter >= '0' && letter <= '9') || letter == '.' || letter == '&'
                    || letter == ' ' || letter == '/' || letter == ','
                    || letter == '(' || letter == ')' || letter == ':'
                    || letter == '[' || letter == ']' || letter == '_')
                    buff.Append(letter);
            return buff.ToString();
        }
    }
}