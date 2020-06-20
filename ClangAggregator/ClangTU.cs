using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using CIndex;

namespace ClangAggregator
{
    public class ClangTU : IDisposable
    {
        IntPtr m_index;
        IntPtr m_tu;

        ClangTU(IntPtr index, IntPtr tu)
        {
            m_index = index;
            m_tu = tu;
        }

        public void Dispose()
        {
            if (m_tu != IntPtr.Zero)
            {
                libclang.clang_disposeTranslationUnit(m_tu);
                m_tu = IntPtr.Zero;
            }
            if (m_index != IntPtr.Zero)
            {
                libclang.clang_disposeIndex(m_index);
                m_index = IntPtr.Zero;
            }
        }

        public static ClangTU Parse(
            string source)
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
            return Parse(args, "tmp.h", source);
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
            if (headers.Count == 1)
            {
                return Parse(args, headers[0], null);
            }
            else
            {
                // 複数の #include をまとめる include をメモリ上に作成 => CXUnsavedFile
                var sb = "";
                foreach (var header in headers)
                {
                    sb += $"#include \"{header}\"\n";
                }

                return Parse(args, "__tmp__ClangCaster__.h", sb.ToString());
            }
        }

        static ClangTU Parse(
            IReadOnlyList<string> args,
            string filename,
            string unsaved)
        {
            var index = libclang.clang_createIndex(0, 1);
            if (index == IntPtr.Zero)
            {
                return null;
            }

            // args
            var buffers = args.Select(x => AllocBuffer.FromString(x)).ToArray();
            var ptrs = buffers.Select(x => x.Ptr).ToArray();

            var options = (uint)CXTranslationUnit_Flags._DetailedPreprocessingRecord
                // | CXTranslationUnit_SkipFunctionBodies
                ;

            var filenameBytes = Encoding.UTF8.GetBytes(filename).Concat(new byte[] { 0 }).ToArray();

            List<Action> disposeList = new List<Action>();

            CXUnsavedFile cxUnsaved = default;
            int unsavedFiles = default;
            if (string.IsNullOrEmpty(unsaved))
            {
                unsavedFiles = 0;
            }
            else
            {
                // use unsaved files
                unsavedFiles = 1;
                var contentsBytes = Encoding.UTF8.GetBytes(unsaved);
                var contentsHandle = GCHandle.Alloc(contentsBytes, GCHandleType.Pinned);
                disposeList.Add(() => contentsHandle.Free());
                var filenameHandle = GCHandle.Alloc(filenameBytes, GCHandleType.Pinned);
                disposeList.Add(() => filenameHandle.Free());
                cxUnsaved = new CXUnsavedFile
                {
                    Filename = filenameHandle.AddrOfPinnedObject(),
                    Contents = contentsHandle.AddrOfPinnedObject(),
                    Length = (uint)contentsBytes.Length,
                };
            }

            // CXUnsavedFileをエントリポイントとしてパースする
            var tu = libclang.clang_parseTranslationUnit(index,
                ref filenameBytes[0],
                ref ptrs[0], ptrs.Length,
                ref cxUnsaved, 1,
                options);

            foreach (var x in disposeList)
            {
                x();
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

        public CXCursor GetCursor()
        {
            return libclang.clang_getTranslationUnitCursor(m_tu);
        }
    }
}
