using System;
using libclang;

namespace ClangCaster
{
    struct ClangLocation
    {
        IntPtr file;
        uint line;
        uint column;
        uint offset;

        uint begin;
        uint end;

        public static ClangLocation Create(in CXCursor cursor)
        {
            var location = index.clang_getCursorLocation(cursor);
            ClangLocation l = default;
            if (index.clang_equalLocations(location, index.clang_getNullLocation()) == 0)
            {
                index.clang_getInstantiationLocation(location, out l.file, out l.line, out l.column, out l.offset);
                var extent = index.clang_getCursorExtent(cursor);
                var begin = index.clang_getRangeStart(extent);
                index.clang_getInstantiationLocation(begin, out l.file, out l.line, out l.column, out l.begin);
                var end = index.clang_getRangeEnd(extent);
                IntPtr _p = default;
                uint line;
                uint column;
                index.clang_getInstantiationLocation(end, out _p, out line, out column, out l.end);
            }
            return l;
        }

        public string Path
        {
            get
            {
                using (var fileStr = ClangString.FromFile(file))
                {
                    return fileStr.ToString();
                }
            }
        }
    }
}
