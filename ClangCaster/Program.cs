using System;
using System.Text;

namespace ClangCaster
{
    class Program
    {
        static void Main(string[] args)
        {
            var index = libclang.index.clang_createIndex(0, 1);
            try
            {
                var source = Encoding.UTF8.GetBytes(args[0]);
                var unsaved = new libclang.CXUnsavedFile
                {

                };
                IntPtr cmd = default;
                var tu = libclang.index.clang_parseTranslationUnit(index, ref source[0], ref cmd, 0, out unsaved, 0, 0);
                Console.WriteLine(tu);
            }
            finally
            {
                libclang.index.clang_disposeIndex(index);
            }
        }
    }
}
