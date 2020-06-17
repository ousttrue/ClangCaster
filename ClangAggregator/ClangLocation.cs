using System;
using CIndex;

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
            var location = libclang.clang_getCursorLocation(cursor);
            ClangLocation l = default;
            if (libclang.clang_equalLocations(location, libclang.clang_getNullLocation()) == 0)
            {
                libclang.clang_getInstantiationLocation(location, ref l.file, ref l.line, ref l.column, ref l.offset);
                var extent = libclang.clang_getCursorExtent(cursor);
                var begin = libclang.clang_getRangeStart(extent);
                libclang.clang_getInstantiationLocation(begin, ref l.file, ref l.line, ref l.column, ref l.begin);
                var end = libclang.clang_getRangeEnd(extent);
                IntPtr _p = default;
                uint line = default;
                uint column = default;
                libclang.clang_getInstantiationLocation(end, ref _p, ref line, ref column, ref l.end);
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
