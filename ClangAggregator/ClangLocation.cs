using System;
using libclang;

namespace ClangAggregator
{
    public struct ClangLocation
    {
        public IntPtr file;
        public uint line;
        public uint column;
        public uint offset;

        public uint begin;
        public uint end;

        public static ClangLocation Create(in CXCursor cursor)
        {
            var location = index.clang_getCursorLocation(cursor);
            ClangLocation l = default;
            if (index.clang_equalLocations(location, index.clang_getNullLocation()) == 0)
            {
                index.clang_getInstantiationLocation(location, ref l.file, ref l.line, ref l.column, ref l.offset);
                var extent = index.clang_getCursorExtent(cursor);
                var begin = index.clang_getRangeStart(extent);
                index.clang_getInstantiationLocation(begin, ref l.file, ref l.line, ref l.column, ref l.begin);
                var end = index.clang_getRangeEnd(extent);
                IntPtr _p = default;
                uint line = default;
                uint column = default;
                index.clang_getInstantiationLocation(end, ref _p, ref line, ref column, ref l.end);
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
