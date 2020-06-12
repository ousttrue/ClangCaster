using System;

namespace ClangCaster
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var tu = ClangTU.Parse(args[0]))
            {
                Console.WriteLine(tu);
            }
        }
    }
}
