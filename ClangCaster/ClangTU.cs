using System;
using System.Collections.Generic;
using System.Text;

namespace ClangCaster
{
    public class ClangTU : IDisposable
    {
        IntPtr m_index;
        libclang.CXTranslationUnitImpl m_tu;

        ClangTU(IntPtr index, IntPtr tu)
        {
            m_index = index;
            m_tu.p = tu;
        }

        public void Dispose()
        {
            if (m_tu.p != IntPtr.Zero)
            {
                libclang.index.clang_disposeTranslationUnit(m_tu.p);
                m_tu.p = IntPtr.Zero;
            }
            if (m_index != IntPtr.Zero)
            {
                libclang.index.clang_disposeIndex(m_index);
                m_index = IntPtr.Zero;
            }
        }

        public static ClangTU Parse(
            IReadOnlyList<string> headers,
            IReadOnlyList<string> includes)
        {
            var index = libclang.index.clang_createIndex(0, 1);
            if (index == IntPtr.Zero)
            {
                return null;
            }

            var source = Encoding.UTF8.GetBytes(headers[0]);
            var unsaved = new libclang.CXUnsavedFile
            {
            };
            IntPtr cmd = default;
            var tu = libclang.index.clang_parseTranslationUnit(index, ref source[0], ref cmd, 0, out unsaved, 0, 0);
            if (tu == IntPtr.Zero)
            {
                return null;
            }

            return new ClangTU(index, tu);
        }
    }
}
