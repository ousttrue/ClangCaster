using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CIndex;

namespace ClangAggregator
{
    class ClangToken : IDisposable, IEnumerable<CXToken>
    {
        IntPtr m_tu;
        public IntPtr TU => m_tu;

        IntPtr m_tokens;
        uint m_num;
        public uint Length => m_num;

        public ClangToken(in CXCursor cursor)
        {
            m_tu = libclang.clang_Cursor_getTranslationUnit(cursor);
            var range = libclang.clang_getCursorExtent(cursor);

            libclang.clang_tokenize(m_tu, range, ref m_tokens, ref m_num);
        }

        public void Dispose()
        {
            libclang.clang_disposeTokens(m_tu, m_tokens, m_num);
        }

        public IEnumerator<CXToken> GetEnumerator()
        {
            var p = m_tokens;
            for (uint i = 0; i < m_num; ++i, p = IntPtr.Add(p, Marshal.SizeOf(typeof(CXToken))))
            {
                yield return (CXToken)Marshal.PtrToStructure(p, typeof(CXToken));
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
