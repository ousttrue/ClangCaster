using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using libclang;

namespace ClangCaster
{
    public class ClangTU : IDisposable
    {
        IntPtr m_index;
        CXTranslationUnitImpl m_tu;

        ClangTU(IntPtr index, IntPtr tu)
        {
            m_index = index;
            m_tu.p = tu;
        }

        public void Dispose()
        {
            if (m_tu.p != IntPtr.Zero)
            {
                index.clang_disposeTranslationUnit(m_tu.p);
                m_tu.p = IntPtr.Zero;
            }
            if (m_index != IntPtr.Zero)
            {
                index.clang_disposeIndex(m_index);
                m_index = IntPtr.Zero;
            }
        }

        public static ClangTU Parse(
            IReadOnlyList<string> headers,
            IReadOnlyList<string> includes,
            IReadOnlyList<string> defines)
        {
            var args = new List<string>{
                "-x",
                "c++",
                "-target",
                "x86_64-windows-msvc",
                "-fms-compatibility-version=18",
                "-fdeclspec",
                "-fms-compatibility"
            };
            foreach (var include in includes)
            {
                args.Add($"-I{include}");
            }
            foreach (var define in defines)
            {
                args.Add($"-D{define}");
            }

            return Parse(headers, args);
        }

        class AllocBuffer : IDisposable
        {
            readonly IntPtr m_p;

            public IntPtr Ptr => m_p;

            AllocBuffer(IntPtr p)
            {
                m_p = p;
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal(m_p);
            }

            public static AllocBuffer FromString(string src)
            {
                var bytes = Encoding.UTF8.GetBytes(src);
                // add zero terminater size
                var p = Marshal.AllocHGlobal(bytes.Length + 1);
                Marshal.Copy(bytes, 0, p, bytes.Length);

                // zero terminate
                var term = new byte[] { 0 };
                Marshal.Copy(term, 0, p + bytes.Length, 1);

                return new AllocBuffer(p);
            }
        }

        static ClangTU Parse(
            IReadOnlyList<string> headers,
            IReadOnlyList<string> args)
        {
            var index = libclang.index.clang_createIndex(0, 1);
            if (index == IntPtr.Zero)
            {
                return null;
            }

            var buffers = args.Select(x => AllocBuffer.FromString(x)).ToArray();
            var ptrs = buffers.Select(x => x.Ptr).ToArray();

            var options = CXTranslationUnit_Flags._DetailedPreprocessingRecord
                // | CXTranslationUnit_SkipFunctionBodies
                ;

            IntPtr tu = default;
            if (headers.Count == 1)
            {
                var source = Encoding.UTF8.GetBytes(headers[0]);
                CXUnsavedFile unsaved = default;
                tu = libclang.index.clang_parseTranslationUnit(index,
                    ref source[0],
                    ref ptrs[0], ptrs.Length,
                    ref unsaved, 0,
                    options);
            }
            else
            {
                var sb = "";
                foreach (var header in headers)
                {
                    sb += $"#include \"{header}\"\n";
                }

                // use unsaved files
                throw new NotImplementedException();
                var unsaved = new CXUnsavedFile
                {
                };
                // files.push_back(CXUnsavedFile{ "__tmp__dclangen__.h", sb.c_str(), static_cast < unsigned long> (sb.size())});
                var source = Encoding.UTF8.GetBytes("__tmp__dclangen__.h");
                tu = libclang.index.clang_parseTranslationUnit(index,
                    ref source[0],
                    ref ptrs[0], ptrs.Length,
                    ref unsaved, 1, options);
            }

            foreach (var buffer in buffers)
            {
                buffer.Dispose();
            }

            if (tu == IntPtr.Zero)
            {
                return null;
            }
            return new ClangTU(index, tu);
        }
    }
}
