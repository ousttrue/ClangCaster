using System;
using CIndex;
using ClangAggregator;

namespace ClangTree
{
    class Printer
    {
        NormalizedFilePath m_path;
        string m_filter;

        bool m_enable;

        public Printer(string path, string filter)
        {
            m_path = new NormalizedFilePath(path);
            m_filter = filter;

            if (string.IsNullOrEmpty(m_filter))
            {
                // no filter
                m_enable = true;
            }
        }

        public void PrintRecursive(in CXCursor cursor, string indent = "")
        {
            ClangVisitor.ProcessChildren(cursor, (in CXCursor c) =>
            {
                var (hash, location) = c.CursorHashLocation();
                var name = c.Spelling();
                if (location.Path == m_path)
                {
                    if (!string.IsNullOrEmpty(m_filter) && name == m_filter)
                    {
                        // enter
                        Console.WriteLine($"{location.Path}:{location.Line}");
                        m_enable = true;
                    }

                    if (m_enable)
                    {                        
                        var isAnonymousDecl = libclang.clang_Cursor_isAnonymousRecordDecl(c);
                        var isAnonymous = libclang.clang_Cursor_isAnonymous(c);
                        Console.WriteLine($"{indent}[{c.kind}]{name} {isAnonymousDecl}{isAnonymous}");
                    }
                }

                PrintRecursive(c, m_enable ? indent + "  " : indent);

                if (location.Path == m_path)
                {
                    if (!string.IsNullOrEmpty(m_filter) && name == m_filter)
                    {
                        // exit
                        m_enable = false;
                    }
                }

                return CXChildVisitResult._Continue;
            });
        }

    }

    class Program
    {

        static void Main(string[] args)
        {
            var header = args[0];
            var filter = args[1];

            using (var tu = ClangTU.Parse(header))
            {
                if (tu is null)
                {
                    Console.WriteLine("fail to parse");
                    return;
                }

                var printer = new Printer(header, filter);
                printer.PrintRecursive(tu.GetCursor());
            }
        }
    }
}
