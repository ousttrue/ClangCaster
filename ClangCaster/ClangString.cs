using System;
using System.Runtime.InteropServices;
using libclang;

namespace ClangCaster
{
    class ClangString : IDisposable
    {
        CXString m_str;

        public ClangString(CXCursor cursor)
        {
            m_str = index.clang_getCursorSpelling(cursor);
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
