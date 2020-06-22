using System.Linq;

namespace CSType
{
    public static class CSSymbole
    {
        /// <summary>
        /// symbols for escape
        /// </summary>
        /// <value></value>
        static string[] Symbols = new string[]
       {
            "base",
            "string",
            "event",
            "ref",
            "in",
            "out",
       };

        public static string Escape(string src)
        {
            if (!Symbols.Contains(src))
            {
                return src;
            }
            return $"_{src}";
        }
    }
}
