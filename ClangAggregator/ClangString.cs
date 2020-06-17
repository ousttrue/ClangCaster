using System;
using System.Runtime.InteropServices;
using CIndex;

namespace ClangAggregator
{
    class ClangString : IDisposable
    {
        CXString m_str;

        public static ClangString FromCursor(CXCursor cursor)
        {
            return new ClangString
            {
                m_str = libclang.clang_getCursorSpelling(cursor)
            };
        }

        public static ClangString FromFile(IntPtr file)
        {
            return new ClangString
            {
                m_str = libclang.clang_getFileName(file)
            };
        }

        public override string ToString()
        {
            var p = libclang.clang_getCString(m_str);
            return Marshal.PtrToStringUTF8(p);
        }

        public void Dispose()
        {
            libclang.clang_disposeString(m_str);
        }
    }
}
