using System.Collections.Generic;
using CIndex;

namespace ClangAggregator
{
    public static class CursorExtensions
    {
        public static string Spelling(this in CXCursor cursor)
        {
            using (var spelling = ClangString.FromCursor(cursor))
            {
                return spelling.ToString();
            }
        }

        public static (uint, ClangLocation) CursorHashLocation(this in CXCursor cursor)
        {
            var hash = libclang.clang_hashCursor(cursor);
            var location = ClangLocation.Create(cursor);
            return (hash, location);
        }

        public static List<CXCursor> Children(this in CXCursor cursor)
        {
            var list = new List<CXCursor>();
            ClangVisitor.ProcessChildren(cursor, (in CXCursor child) =>
            {
                list.Add(child);
                return CXChildVisitResult._Continue;
            });
            return list;
        }
    }
}
