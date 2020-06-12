using System;
using System.Runtime.InteropServices;
using libclang;

namespace ClangCaster
{
    class ClangString : IDisposable
    {
        CXString m_str;

        public static ClangString FromCursor(CXCursor cursor)
        {
            return new ClangString
            {
                m_str = index.clang_getCursorSpelling(cursor)
            };
        }

        public static ClangString FromFile(IntPtr file)
        {
            return new ClangString
            {
                m_str = index.clang_getFileName(file)
            };
        }

        public override string ToString()
        {
            var p = cxstring.clang_getCString(m_str);
            return Marshal.PtrToStringUTF8(p);
        }

        public void Dispose()
        {
            cxstring.clang_disposeString(m_str);
        }
    }
}
