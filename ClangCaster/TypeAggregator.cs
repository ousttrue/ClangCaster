using System;
using System.Runtime.InteropServices;
using libclang;

namespace ClangCaster
{
    public class TypeAggregator
    {
        struct Context
        {
        }

        void TraverseChildren(in CXCursor cursor, in Context _context)
        {
            var context = _context;
            ClangVisitor.ProcessChildren(cursor, (in CXCursor c) => Traverse(c, context));
        }

        public void Process(in CXCursor cursor)
        {
            TraverseChildren(cursor, default);
        }

        CXChildVisitResult Traverse(in CXCursor cursor, in Context context)
        {
            Console.WriteLine(cursor.kind);
            return CXChildVisitResult._Continue;
        }
    }
}
