using System;
using System.Runtime.InteropServices;
using CIndex;

namespace ClangAggregator
{
    public static class ClangVisitor
    {
        public delegate CXChildVisitResult CallbackFunc(in CXCursor cursor);

        static CXChildVisitResult Visitor(CXCursor cursor, CXCursor parent, IntPtr data)
        {
            var callback = Marshal.GetDelegateForFunctionPointer<CallbackFunc>(data);
            return callback(cursor);
        }
        static CXCursorVisitor s_func = new CXCursorVisitor(Visitor);

        public static void ProcessChildren(in CXCursor cursor, CallbackFunc callback)
        {
            var p = Marshal.GetFunctionPointerForDelegate(callback);
            libclang.clang_visitChildren(cursor, s_func, p);
        }
    }
}
