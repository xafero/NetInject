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
    }
}