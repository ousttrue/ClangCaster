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
    }
}
