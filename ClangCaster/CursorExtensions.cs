using System.Collections.Generic;
using libclang;

namespace ClangCaster
{
    public static class CursorExtensions
    {
        public static (uint, ClangLocation, string) CursorHashLocationSpelling(this in CXCursor cursor)
        {
            var hash = index.clang_hashCursor(cursor);
            var location = ClangLocation.Create(cursor);
            using (var spelling = ClangString.FromCursor(cursor))
            {
                return (hash, location, spelling.ToString());
            }
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
